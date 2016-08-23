using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

using BytesPerSecond = System.Single;
using Bytes = System.UInt32;
using Megabytes = System.UInt32;
using Milliseconds = System.Int64;
using FilePath = System.String;
using SequenceID = System.Nullable<System.UInt32>;
using SessionID = System.Nullable<System.UInt32>;
using FrameID = System.UInt32;
using UserDataKey = System.String;
using System.ComponentModel;

namespace TelemetryTools
{
    public static class Utility
    {
        public static byte[] StringToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string BytesToString(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes);
        }

        public static byte[] RemoveTrailingNulls(byte[] data)
        {
            int i = data.Length - 1;
            while (data[i] == 0)
                i--;
            byte[] trimmed = new byte[i + 2];
            System.Buffer.BlockCopy(data, 0, trimmed, 0, trimmed.Length);
            return trimmed;
        }


    }
}
