using UnityEngine;
using System;
using System.Collections.Generic;

using UserDataKey = System.String;

namespace TelemetryTools.Upload
{
    public class UserDataUploadConnection : UploadConnection
    {
        public UserDataUploadConnection(URL url) : base(url) { }

        public void SendUserData(Dictionary<UserDataKey, string> userData, UniqueKey uniqueKey, KeyID keyID)
        {
            if (uniqueKey.IsSet)
            {
                if (userData.Count > 0)
                {
                    Send(new UploadRequest(new WWW(url.AsString, CreateWWWForm(userData, uniqueKey)),uniqueKey,keyID));
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
            form.AddField("key", uniqueKey.AsString);
            foreach (string key in userData.Keys)
                form.AddField(key, userData[key]);
            return form;
        }
    }
}
