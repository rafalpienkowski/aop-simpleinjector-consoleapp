using System.Linq;
using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Interceptors
{
    /// <summary>
    /// Monitoring interceptor to method arguments and result call
    /// </summary>
    public class LoggingInterceptor : IInterceptor
    {
        private readonly ILogger _logger;

        // Using constructor injection on the interceptor
        public LoggingInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            _logger.Log($"{invocation.GetConcreteMethod().Name} argument(s): {string.Join(", ",invocation.Arguments.Select(a => a))}");

            // Calls the decorated instance.
            invocation.Proceed();

            _logger.Log($"{invocation.GetConcreteMethod().Name} result { invocation.ReturnValue}");

        }
    }
}
