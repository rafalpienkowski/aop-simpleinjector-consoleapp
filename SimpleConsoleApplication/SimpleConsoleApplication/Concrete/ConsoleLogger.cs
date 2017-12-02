using System;
using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Concrete
{
    /// <inheritdoc />
    /// <summary>
    /// Console logger
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }
    }
}