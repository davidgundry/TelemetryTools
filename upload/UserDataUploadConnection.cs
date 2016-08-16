using UnityEngine;
using System;
using System.Collections.Generic;

using URL = System.String;
using KeyID = System.Nullable<System.UInt32>;
using UserDataKey = System.String;
using UniqueKey = System.String;

namespace TelemetryTools.Upload
{
    public class UserDataUploadConnection : UploadConnection
    {
        public UserDataUploadConnection(URL url) : base(url) { }

        public void SendUserData(Dictionary<UserDataKey, string> userData, UniqueKey uniqueKey, KeyID keyID)
        {
            if (!String.IsNullOrEmpty(uniqueKey))
            {
                if (userData.Count > 0)
                {
                    Send(new UploadRequest(new WWW(URL, CreateWWWForm(userData, uniqueKey)),uniqueKey,keyID));
                }
                else
                    Debug.LogWarning("Cannot send empty user data to server");
            }
            else
                Debug.LogWarning("Cannot send user data to server without a key");
        }

        private WWWForm CreateWWWForm(Dictionary<UserDataKey, string> userData, UniqueKey uniqueKey)
        {
            WWWForm form = new WWWForm();
            form.AddField("key", uniqueKey);
            foreach (string key in userData.Keys)
                form.AddField(key, userData[key]);
            return form;
        }
    }
}
