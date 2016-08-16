using System.Text;

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
