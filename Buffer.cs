#define POSTENABLED

using UnityEngine;

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
using System;


namespace TelemetryTools
{
    public class Buffer
    {
        private const Bytes defaultBufferSize = 1048576;
        private readonly Bytes bufferSize;
        private const Bytes defaultFrameBufferSize = 1024 * 128;
        private readonly Bytes frameBufferSize;
        private const Bytes defaultMinSendingThreshold = 1024;
        private readonly Bytes minSendingThreshold;

        public byte[] ActiveBuffer { get { if (buffer1Active) return outboxBuffer1; else return outboxBuffer2; } }
        public byte[] OffBuffer { get { if (buffer1Active) return outboxBuffer2; else return outboxBuffer1; } }

        public bool OffBufferFull { get; set; }

        private byte[] outboxBuffer1;
        private byte[] outboxBuffer2;

        private int bufferPos = 0;
        private bool buffer1Active = true;
        
        private byte[] frameBuffer;
        private int frameBufferPos = 0;

        public bool ReadyToSend { get { return bufferPos > minSendingThreshold; } }

        public Buffer()
        {
            this.bufferSize = defaultBufferSize;
            this.frameBufferSize = defaultFrameBufferSize;

            outboxBuffer1 = new byte[bufferSize];
            outboxBuffer2 = new byte[bufferSize];
            frameBuffer = new byte[frameBufferSize];

            Array.Clear(outboxBuffer1, 0, outboxBuffer1.Length);
            Array.Clear(outboxBuffer2, 0, outboxBuffer2.Length);
            Array.Clear(frameBuffer, 0, frameBuffer.Length);

            minSendingThreshold = defaultMinSendingThreshold;
        }

        public void ResetBufferPosition()
        {
            bufferPos = 0;
        }

        public void ResetFrameBufferPosition()
        {
            frameBufferPos = 0;
        }

        public byte[] GetDataInActiveBuffer()
        {
            byte[] partBuffer = new byte[bufferPos];
            if (bufferPos > 0)
                System.Buffer.BlockCopy(ActiveBuffer, 0, partBuffer, 0, partBuffer.Length);
            return partBuffer;
        }

        private void AddEndFrameBytes()
        {
            byte[] endFrame = Utility.StringToBytes("}\n"); // newline required for mongo import?
            System.Buffer.BlockCopy(endFrame, 0, frameBuffer, frameBufferPos, endFrame.Length);
            frameBufferPos += endFrame.Length;
        }

        public void BufferToFrameBuffer(byte[] data)
        {
            if (frameBufferPos + data.Length < frameBuffer.Length)
            {
                System.Buffer.BlockCopy(data, 0, frameBuffer, frameBufferPos, data.Length);
                frameBufferPos += data.Length;
            }
            else
            {
                Debug.LogWarning("Overflow frame buffer, data lost");
                ConnectionLogger.Instance.AddLostData((uint)data.Length);
            }
        }

        public void BufferInNewFrame(byte[] data, bool firstFrame)
        {
            if (!firstFrame)
            {
                AddEndFrameBytes();
            }

            if (frameBufferPos + bufferPos > bufferSize)
            {
                if (OffBufferFull)
                {
                    Debug.LogWarning("Overflow local telemetry buffer, data overwritten");
                    ConnectionLogger.Instance.AddLostData((uint)Utility.RemoveTrailingNulls(OffBuffer).Length);
                }

                Array.Clear(ActiveBuffer, bufferPos, outboxBuffer1.Length - bufferPos);

                buffer1Active = !buffer1Active;
                OffBufferFull = true;
                ResetBufferPosition();
            }

            System.Buffer.BlockCopy(frameBuffer, 0, ActiveBuffer, bufferPos, frameBufferPos);
            bufferPos += frameBufferPos;
            ResetFrameBufferPosition();


            BufferToFrameBuffer(data);
        }

    }

}