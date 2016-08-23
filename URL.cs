namespace TelemetryTools
{
    public class URL
    {
        private readonly string url;

        public string AsString { get { return url; } }

        public URL(string url)
        {
            this.url = url;
        }
    }
}