using Bytes = System.UInt32;
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
            www.Dispose();
        }

        public virtual Bytes RequestSizeInBytes()
        {
            return 0;
        }
    }
}