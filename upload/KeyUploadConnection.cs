using System;
using System.Collections.Generic;
using UnityEngine;

using URL = System.String;
using KeyID = System.Nullable<System.UInt32>;

namespace TelemetryTools.Upload
{
    public class KeyUploadConnection : UploadConnection
    {
        public KeyUploadConnection(URL url) : base(url) { }

        public void RequestUniqueKey(KeyValuePair<string, string>[] userData, KeyID keyID)
        {
            Send(new UploadRequest(new WWW(URL, CreateWWWForm(userData)), null, keyID));
        }

        private WWWForm CreateWWWForm(KeyValuePair<string, string>[] userData)
        {
            WWWForm form = new WWWForm();
            form.AddField(UserPropertyKeys.RequestTime, System.DateTime.UtcNow.ToString("u"));

            foreach (KeyValuePair<string, string> pair in userData)
                form.AddField(pair.Key, pair.Value);
            return form;
        }
    }
}