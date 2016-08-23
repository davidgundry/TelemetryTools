using UnityEngine;
using System;

using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;

namespace TelemetryTools.Upload
{
    public class BufferUploadConnection : UploadConnection
    {
        public Bytes LostData { get; set; }
        private readonly FilePath fileExtension;

        public BufferUploadConnection(URL url, FilePath fileExtension) : base(url)
        {
            this.fileExtension = fileExtension;
        }

        public void UploadData(KeyAssociatedData keyedData)
        {
            if (keyedData.Key.IsSet)
            {
                Send(new BufferUploadRequest(new WWW(url.AsString, CreateWWWForm(keyedData)), keyedData));
            }
            else
            {
                Debug.LogWarning("Cannot send data without a key to the server");
            }
        }

        private WWWForm CreateWWWForm(KeyAssociatedData keyedData)
        {
            WWWForm form = new WWWForm();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(keyedData.SessionID);
            sb.Append(".");
            sb.Append(keyedData.SequenceID);
            sb.Append(".");
            sb.Append(fileExtension);
            form.AddField("key", keyedData.Key.AsString);
            form.AddField("session", keyedData.SessionID.ToString());
            form.AddBinaryData(fileExtension, keyedData.Data, sb.ToString());

            return form;
        }

    }
}