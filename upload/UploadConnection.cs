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
        public bool Busy { get; protected set; }
        public URL URL { get; private set; }
        public bool BadURL { get; private set; }
        protected UploadRequest UploadRequest { get; private set; }

        public delegate void ErrorHandler(UploadRequest uploadRequest, string error);
        public delegate void SuccessHandler(UploadRequest uploadRequest, string message = null);

        public event ErrorHandler OnError = delegate { };
        public event SuccessHandler OnSuccess = delegate { };

        public bool ReadyToSend { get { return ((RequestDelay <= 0) && (!BadURL) && (!Busy)); } }
        protected float RequestDelay { get; set; }
        protected float TotalRequestDelay { get; set; }
        private const float defaultTotalRequestDelay = 1;

        public int Requests { get; protected set; }
        public int Errors { get; private set; }
        public int Successes { get; private set; }
        public int InvalidResponse { get; set; }
        protected Bytes BytesSent { get; private set; }

        public UploadConnection(URL url)
        {
            URL = url;
            TotalRequestDelay = defaultTotalRequestDelay;
        }

        public void SetURL(URL url)
        {
            URL = url;
            BadURL = false;
        }

        public bool ConnectionActive
        {
            get
            {
                if (UploadRequest == null)
                    return false;
                if (UploadRequest.WWW == null)
                    return false;
                return true;
            }
        }

        public virtual void DisposeRequest()
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
            BytesSent += uploadRequest.RequestSizeInBytes();
        }

        public void Update(float deltaTime)
        {
            ReduceRequestDelay(deltaTime);
            CheckForWWWResponse();
        }

        public void ResetRequestDelay()
        {
            RequestDelay = TotalRequestDelay;
        }

        public void ClearRequestDelay()
        {
            RequestDelay = 0;
        }

        private void CheckForWWWResponse()
        {
            if (ConnectionActive)
                if ((UploadRequest.WWW.isDone) && (!string.IsNullOrEmpty(UploadRequest.WWW.error)))
                {
                    Error();
                }
                else if (UploadRequest.WWW.isDone)
                {
                    Success();
                }
        }

        private void Success()
        {
            Successes++;
            string message = UploadRequest.WWW.text;
            if (!string.IsNullOrEmpty(message.Trim()))
            {
                Debug.LogWarning("Response from server: " + message);
                OnSuccess.Invoke(UploadRequest, message);
            }
            else
                OnSuccess.Invoke(UploadRequest);

            DisposeRequest();
        }

        private void Error()
        {
            Errors++;

            string error = UploadRequest.WWW.error;

            if (error == "<url> malformed")
            {
                BadURL = true;
                Debug.LogWarning("WWW Error: " + error + ". Will not retry until URL is changed");
            }
            else
                Debug.LogWarning("WWW Error: " + error);

            OnError.Invoke(UploadRequest, error);
            ResetRequestDelay();
            DisposeRequest();
        }

        private void ReduceRequestDelay(float deltaTime)
        {
            if (RequestDelay > 0)
                RequestDelay -= deltaTime;
        }
    }
}

#endif