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
        private readonly byte[] data;
        public byte[] Data { get { return data; } }

        private readonly SessionID sessionID;
        public SessionID SessionID { get { return sessionID; } }

        private readonly SequenceID sequenceID;
        public SequenceID SequenceID { get { return sequenceID; } }

        private readonly KeyAssociatedData keyedData;
        public KeyAssociatedData KeyedData { get { return keyedData; } }

        public BufferUploadRequest(WWW www, KeyAssociatedData keyedData) : base(www, keyedData.Key, keyedData.KeyID)
        {
            byte[] dataCopy = new byte[keyedData.Data.Length];
            System.Buffer.BlockCopy(keyedData.Data, 0, dataCopy, 0, keyedData.Data.Length);

            this.data = dataCopy;
            this.sessionID = keyedData.SessionID;
            this.sequenceID = keyedData.SequenceID;
        }

        public override Bytes RequestSizeInBytes()
        {
            return (uint) Data.Length;
        }

        public KeyAssociatedData GetKeyAssociatedData()
        {
            return KeyedData;
        }
    }
}