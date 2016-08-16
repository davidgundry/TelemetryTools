using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using URL = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using KeyID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;
using UniqueKey = System.String;

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TelemetryTools.Upload
{
    public class KeyUploadConnection : UploadConnection
    {
        public KeyUploadConnection(URL url) : base(url)
        {
        }

        public void HandleKeyWWWResponse()
        {
            bool? success = null;
            if (Busy)
            {
                if (WWW != null)
                {
                    UniqueKey newKey = null;
                    success = GetReturnedKey(ref newKey);
                    if (success != null)
                    {
                        if (success == true)
                        {
                            ConnectionLogger.Instance.KeyServerSuccess();
                            Array.Resize(ref keys, NumberOfKeys + 1);
                            keys[NumberOfKeys - 1] = newKey;
                            PlayerPrefs.SetString("key" + (NumberOfKeys - 1), newKey);
                            PlayerPrefs.SetString("numkeys", NumberOfKeys.ToString());
                            PlayerPrefs.Save();
                            ConnectionLogger.Instance.UploadUserDataDelay = 0;
                            ConnectionLogger.Instance.UploadCacheFilesDelay = 0;
                        }
                        else
                        {
                            ConnectionLogger.Instance.KeyServerError();
                            ResetRequestDelay();
                        }
                        Busy = false;
                    }
                }
            }
        }

        public void RequestUniqueKey(KeyValuePair<string, string>[] userData)
        {
            ConnectionLogger.Instance.KeyServerRequestSent();
            WWWForm form = new WWWForm();
            form.AddField(UserPropertyKeys.RequestTime, System.DateTime.UtcNow.ToString("u"));

            foreach (KeyValuePair<string, string> pair in userData)
                form.AddField(pair.Key, pair.Value);

            Busy = true;
            WWW = new WWW(URL, form);
        }

        private bool? GetReturnedKey(ref string uniqueKey)
        {
            if (WWW != null)
                if (WWW.isDone)
                {
                    if (string.IsNullOrEmpty(WWW.error))
                    {
                        if (WWW.text.StartsWith("key:"))
                        {
                            uniqueKey = WWW.text.Substring(4);
                            Debug.Log("Key retrieved: " + uniqueKey);
                            return true;
                        }
                        else
                        {
                            Debug.LogWarning("Invalid key retrieved: " + WWW.text);
                            return false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Error connecting to key server");
                        return false;
                    }
                }
            return null;
        }
    }
}