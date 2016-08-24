using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;

using UnityEngine;

namespace TelemetryTools.Upload
{
    public class BufferUploadRequest : UploadRequest
    {
        private readonly KeyAssociatedData keyedData;
        public KeyAssociatedData KeyedData { get { return keyedData; } }

        public byte[] Data { get { return keyedData.Data; } }
        public SessionID SessionID { get { return keyedData.SessionID; } }
        public SequenceID SequenceID { get { return keyedData.SequenceID; } }

        public BufferUploadRequest(WWW www, KeyAssociatedData keyedData) : base(www, keyedData.Key, keyedData.KeyID)
        {
            byte[] dataCopy = new byte[keyedData.Data.Length];
            System.Buffer.BlockCopy(keyedData.Data, 0, dataCopy, 0, keyedData.Data.Length);
            this.keyedData = keyedData;
        }

        public override Bytes RequestSizeInBytes()
        {
            return (uint) Data.Length;
        }
    }
}