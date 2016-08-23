using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using URL = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;


namespace TelemetryTools
{
    public class KeyAssociatedData
    {
        private readonly byte[] data;
        public byte[] Data { get { return data; } }

        private readonly SessionID sessionID;
        public SessionID SessionID { get { return sessionID; } }

        private readonly SequenceID sequenceID;
        public SequenceID SequenceID { get { return sequenceID; } }

        private readonly KeyID keyID;
        public KeyID KeyID { get { return keyID; } }

        public KeyAssociatedData(byte[] data, SessionID sessionID, SequenceID sequenceID, KeyID keyID)
        {
            this.data = data;
            this.sessionID = sessionID;
            this.sequenceID = sequenceID;
            this.keyID = keyID;
        }

    }
}