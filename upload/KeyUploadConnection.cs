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


        public void RequestUniqueKey(KeyValuePair<string, string>[] userData, KeyID keyID)
        {
            ConnectionLogger.Instance.KeyServerRequestSent();
            WWWForm form = new WWWForm();
            form.AddField(UserPropertyKeys.RequestTime, System.DateTime.UtcNow.ToString("u"));

            foreach (KeyValuePair<string, string> pair in userData)
                form.AddField(pair.Key, pair.Value);

            Send(new UploadRequest(new WWW(URL, form), null, keyID));
        }

        
    }
}