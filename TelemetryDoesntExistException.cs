namespace TelemetryTools
{
    public class TelemetryDoesntExistException : System.Exception
    {
        public TelemetryDoesntExistException()
            : base()
        { }

        public TelemetryDoesntExistException(string message)
            : base(message)
        { }

        public TelemetryDoesntExistException(string message, System.Exception innerException)
            : base(message, innerException)
        { }
    }
}