#define POSTENABLED
#if POSTENABLED

using UnityEngine;
using System;
using System.Collections.Generic;

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
    public class UploadConnection
    {
        public WWW WWW { get; protected set; }
        public bool Busy { get; protected set; }
        public UniqueKey Key { get; protected set; }
        public KeyID KeyID { get; protected set; }
        public URL URL { get; protected set; }

        public UploadConnection(URL url)
        {
            URL = url;
        }

        public void SetURL(URL url)
        {
            URL = url;
        }

        public bool Initialised()
        {
            return WWW != null;
        }

        public virtual void Dispose()
        {
            Busy = false;
            Key = null;
            KeyID = null;
        }
    }
}

#endif