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
        public byte[] Data { get; private set; }
        public SequenceID SequenceID { get; private set; }
        public SessionID SessionID { get; private set; }

        public BufferUploadConnection(URL url)
            : base(url)
        {

        }


        public override void Dispose()
        {
            Busy = false;
            Data = new byte[0];
            SessionID = null;
            SequenceID = null;
            Key = null;
            KeyID = null;
        }

        public void SendByHTTPPost(byte[] data,
                                    SessionID sessionID,
                                    SequenceID sequenceID,
                                    FilePath fileExtension,
                                    UniqueKey key,
                                    KeyID keyID)
        {

            if (!String.IsNullOrEmpty(key))
            {
                Data = new byte[data.Length];
                System.Buffer.BlockCopy(data, 0, Data, 0, data.Length);
                SequenceID = sequenceID;
                SessionID = sessionID;
                Key = key;
                KeyID = keyID;

                WWW = new WWW(URL, CreateWWWForm());
                Busy = true;
                Requests++;
            }
            else
            {
                Debug.LogWarning("Cannot send data without a key to the server");
                Dispose();
            }

        }

        private WWWForm CreateWWWForm()
        {
            string fileExtension = "telemetry";

            WWWForm form = new WWWForm();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(SessionID);
            sb.Append(".");
            sb.Append(SequenceID);
            sb.Append(".");
            sb.Append(fileExtension);
            form.AddField("key", Key);
            form.AddField("session", SessionID.ToString());
            form.AddBinaryData(fileExtension, Data, sb.ToString());

            return form;
        }

        public bool HandleWWWResponse()
        {
            if (WWW != null)
            {
                if ((WWW.isDone) && (!string.IsNullOrEmpty(WWW.error)))
                {
                    WWWError();
                    return false;
                }
                else if (WWW.isDone)
                {
                    WWWSuccess();
                }
            }
            
            return true;
        }

        private void WWWError()
        {
            Debug.LogWarning("Send Data Error: " + WWW.error);
            Errors++;
        }

        private void WWWSuccess()
        {
            if (!string.IsNullOrEmpty(WWW.text.Trim()))
            {
                Debug.LogWarning("Response from server: " + WWW.text);
            }
            Dispose();
            Successes++;
        }
    }
}