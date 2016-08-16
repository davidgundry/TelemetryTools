using UnityEngine;
using System;

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
    public class BufferUploadConnection : UploadConnection
    {
        public BufferUploadConnection(URL url) : base(url) { }

        public void UploadData(byte[] data, SessionID sessionID, SequenceID sequenceID, FilePath fileExtension, UniqueKey key, KeyID keyID)
        {
            if (!String.IsNullOrEmpty(key))
            {
                Send(new BufferUploadRequest(new WWW(URL, CreateWWWForm(key, data, sessionID, sequenceID, fileExtension)), key, keyID, data, sessionID,sequenceID));
            }
            else
            {
                Debug.LogWarning("Cannot send data without a key to the server");
            }
        }

        private WWWForm CreateWWWForm(UniqueKey key, byte[] data, SessionID sessionID, SequenceID sequenceID, FilePath fileExtension)
        {
            WWWForm form = new WWWForm();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(sessionID);
            sb.Append(".");
            sb.Append(sequenceID);
            sb.Append(".");
            sb.Append(fileExtension);
            form.AddField("key", key);
            form.AddField("session", sessionID.ToString());
            form.AddBinaryData(fileExtension, data, sb.ToString());

            return form;
        }
    }
}