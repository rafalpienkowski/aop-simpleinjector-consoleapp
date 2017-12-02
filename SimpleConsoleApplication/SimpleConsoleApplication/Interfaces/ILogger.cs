namespace SimpleConsoleApplication.Interfaces
{
    /// <summary>
    /// Simple logger interface
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Logs messages
        /// </summary>
        /// <param name="message">Message to log</param>
        void Log(string message);
    }
}