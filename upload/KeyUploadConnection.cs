using System;
using System.Collections.Generic;
using UnityEngine;

using TelemetryTools.Strings;

using URL = System.String;
using KeyID = System.Nullable<System.UInt32>;
using UniqueKey = System.String;

namespace TelemetryTools.Upload
{
    public class KeyUploadConnection : UploadConnection
    {
        public delegate void KeyReturnedHandler(UniqueKey keyReturned);
        public event KeyReturnedHandler OnKeyReturned = delegate (UniqueKey keyReturned) {};

        public KeyUploadConnection(URL url) : base(url)
        {
            OnSuccess += new SuccessHandler(HandleServerResponse);
        }

        public void RequestUniqueKey(KeyValuePair<string, string>[] userData, KeyID keyID)
        {
            Send(new UploadRequest(new WWW(url, CreateWWWForm(userData)), null, keyID));
        }

        private WWWForm CreateWWWForm(KeyValuePair<string, string>[] userData)
        {
            WWWForm form = new WWWForm();
            form.AddField(UserPropertyKeys.RequestTime, System.DateTime.UtcNow.ToString("u"));

            foreach (KeyValuePair<string, string> pair in userData)
                form.AddField(pair.Key, pair.Value);
            return form;
        }

        private void HandleServerResponse(UploadRequest uploadRequest, string message)
        {
            UniqueKey newKey = GetReturnedKey(message);
            if (newKey != null)
            {
                OnKeyReturned.Invoke(newKey);
            }
            else
            {
                InvalidResponse++;
                ResetRequestDelay();
            }
        }

        private UniqueKey GetReturnedKey(string message)
        {
            if (message.StartsWith("key:"))
            {
                UniqueKey uniqueKey = message.Substring(4);
                Debug.Log("Key retrieved: " + uniqueKey);
                return uniqueKey;
            }
            else
            {
                Debug.LogWarning("Invalid key retrieved: " + message);
                return null;
            }
        }
    }
}