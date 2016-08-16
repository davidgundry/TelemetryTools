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
    public delegate void UploadErrorHandler(UploadRequest uploadRequest, string error);
    public delegate void UploadSuccessHandler(UploadRequest uploadRequest, string message = null);

    public class UploadConnection
    {
        public bool Busy { get; protected set; }

        public URL URL { get; protected set; }

        public bool NoRequestDelay { get { return RequestDelay <= 0; } }
        protected float RequestDelay { get; set; }
        protected float TotalRequestDelay { get; set; }

        protected int Requests { get; set; }
        protected int Errors { get; private set; }
        protected int Successes { get; private set; }

        protected UploadRequest UploadRequest { get; private set; }

        public event UploadErrorHandler OnError;
        public event UploadSuccessHandler OnSuccess;

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
            return UploadRequest.WWW != null;
        }

        public virtual void Dispose()
        {
            Busy = false;
            UploadRequest.Dispose();
            UploadRequest = null;
        }

        public void Send(UploadRequest uploadRequest)
        {
            UploadRequest = uploadRequest;
            Busy = true;
            Requests++;
        }

        public void Update(float deltaTime)
        {
            CheckForWWWResponse();
            ReduceRequestDelay(deltaTime);
        }

        private void CheckForWWWResponse()
        {
            if (UploadRequest.WWW != null)
            {
                if ((UploadRequest.WWW.isDone) && (!string.IsNullOrEmpty(UploadRequest.WWW.error)))
                {
                    Errors++;
                    Debug.LogWarning("WWW Error: " + UploadRequest.WWW.error);
                    OnError.Invoke(UploadRequest,UploadRequest.WWW.error);
                }
                else if (UploadRequest.WWW.isDone)
                {
                    Successes++;
                    if (!string.IsNullOrEmpty(UploadRequest.WWW.text.Trim()))
                    {
                        Debug.LogWarning("Response from server: " + UploadRequest.WWW.text);
                        OnSuccess.Invoke(UploadRequest,UploadRequest.WWW.text);
                    }
                    else
                        OnSuccess.Invoke(UploadRequest);     
                }
            }
        }

        public void ResetRequestDelay()
        {
            RequestDelay = TotalRequestDelay;
        }

        private void ReduceRequestDelay(float deltaTime)
        {
            RequestDelay -= deltaTime;
        }
    }
}

#endif