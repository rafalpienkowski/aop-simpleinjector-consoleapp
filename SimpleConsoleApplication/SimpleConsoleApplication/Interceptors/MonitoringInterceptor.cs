using System.Diagnostics;
using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Interceptors
{
    /// <summary>
    /// Monitoring interceptor to check method execution time
    /// </summary>
    public class MonitoringInterceptor : IInterceptor
    {
        private readonly ILogger _logger;

        // Using constructor injection on the interceptor
        public MonitoringInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public void Intercept(IInvocation invocation)
        {
            var watch = Stopwatch.StartNew();

            // Calls the decorated instance.
            invocation.Proceed();

            var decoratedType = invocation.InvocationTarget.GetType();

            _logger.Log($"{decoratedType.Name}.{invocation.GetConcreteMethod().Name}() executed in {watch.ElapsedMilliseconds} ms.");
        }
    }
}
