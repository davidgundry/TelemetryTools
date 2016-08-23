using System;

namespace TelemetryTools
{
    public struct UniqueKey
    {

        private readonly char[] key;

        public bool IsSet { get { if (key == null) return false; return true; } }
        public string AsString { get { return new string(key); } }

        public UniqueKey(string key)
        {
            this.key = key.ToCharArray(0, 16);
        }

    }
}