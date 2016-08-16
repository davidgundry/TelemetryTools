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

        public BufferUploadRequest(WWW www, UniqueKey key, KeyID keyID, byte[] data, SessionID sessionID, SequenceID sequenceID) : base(www, key, keyID)
        {
            byte[] dataCopy = new byte[data.Length];
            System.Buffer.BlockCopy(data, 0, dataCopy, 0, data.Length);

            this.data = dataCopy;
            this.sessionID = sessionID;
            this.sequenceID = sequenceID;
        }

    }
}