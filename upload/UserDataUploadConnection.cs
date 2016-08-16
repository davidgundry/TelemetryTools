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

        private WWWForm CreateWWWForm(Dictionary<UserDataKey, string> userData, UniqueKey uniqueKey)
        {
            WWWForm form = new WWWForm();
            form.AddField("key", uniqueKey);
            foreach (string key in userData.Keys)
                form.AddField(key, userData[key]);
            return form;
        }


        public void SendByHTTPPost(Dictionary<UserDataKey, string> userData,
                                            UniqueKey uniqueKey,
                                            KeyID keyID)
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

    }
}
