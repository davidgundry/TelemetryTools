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
    public class UploadRequest
    {
        private readonly WWW www;
        public WWW WWW { get { return www; } }

        private readonly UniqueKey key;
        public UniqueKey Key { get { return key; } }

        private readonly KeyID keyID;
        public KeyID KeyID { get { return keyID; } }

        public UploadRequest(WWW www, UniqueKey key, KeyID keyID)
        {
            this.www = www;
            this.key = key;
            this.keyID = keyID;
        }

        public void Dispose()
        {

        }
    }
}