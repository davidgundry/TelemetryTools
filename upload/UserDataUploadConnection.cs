using UnityEngine;
using System;
using System.Collections.Generic;

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

namespace TelemetryTools.Upload
{
    public class UserDataUploadConnection : UploadConnection
    {

        public UserDataUploadConnection(URL url)
            : base(url)
        {

        }



        public void SendByHTTPPost(Dictionary<UserDataKey, string> userData,
                                            UniqueKey uniqueKey,
                                            KeyID keyID)
        {
            if (!String.IsNullOrEmpty(uniqueKey))
            {
                if (userData.Count > 0)
                {
                    WWWForm form = new WWWForm();
                    form.AddField("key", uniqueKey);
                    foreach (string key in userData.Keys)
                        form.AddField(key, userData[key]);

                    WWW = new WWW(URL, form);
                    Busy = true;
                    KeyID = keyID;
                    ConnectionLogger.Instance.HTTPRequestSent();
                }
                else
                    Debug.LogWarning("Cannot send empty user data to server");
            }
            else
                Debug.LogWarning("Cannot send user data to server without a key");
        }

#if LOCALSAVEENABLED
        public bool HandleUserDataWWWResponse( ref Dictionary<UserDataKey, string> userData,
                                                KeyID currentKeyID,
                                                List<string> userDataFilesList,
                                                FileAccessor fileAccessor)
#else
        public bool HandleaWWWResponse(ref Dictionary<UserDataKey, string> userData,
                                                KeyID currentKeyID,
                                                List<string> userDataFilesList)
#endif

        {
            if (WWW != null)
            {
                if (Busy)
                {
                    if ((WWW.isDone) && (!string.IsNullOrEmpty(WWW.error)))
                    {
                        Debug.LogWarning("Send User Data Error: " + WWW.error);
                        Busy = false;
                        ConnectionLogger.Instance.HTTPError();
                    }
                    else if (WWW.isDone)
                    {
                        if (!string.IsNullOrEmpty(WWW.text.Trim()))
                        {
                            Debug.LogWarning("Response from server: " + WWW.text);
                        }
                        if (KeyID == currentKeyID)
                            userData.Clear();
                        else
                        {
#if LOCALSAVEENABLED
                            File.Delete(GetFileInfo(userDataDirectory, wwwKeyID.ToString() + "." + userDataFileExtension).FullName);
                            userDataFilesList.Remove(wwwKeyID.ToString() + "." + userDataFileExtension);
                            //fileAccessor.WriteStringsToFile(userDataFilesList.ToArray(), GetFileInfo(userDataDirectory, userDataListFilename));
                            userDataFilesListDirty = true;
#endif
                        }

                        Busy = false;
                        KeyID = null;
                        ConnectionLogger.Instance.HTTPSuccess();
                    }
                }
            }

            return true;
        }
    }
}
