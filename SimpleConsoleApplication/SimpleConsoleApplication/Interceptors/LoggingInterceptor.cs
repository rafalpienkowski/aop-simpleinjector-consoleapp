using System.Linq;
using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Interceptors
{
    /// <inheritdoc />
    /// <summary>
    /// Monitoring interceptor to method arguments and result call
    /// </summary>
    public class LoggingInterceptor : CustomBaseInterceptor
    {
        // Using constructor injection on the interceptor
        public LoggingInterceptor(ILogger logger) : base(logger){ }

        protected override void PreProceedAction(IInvocation invocation)
        {
            Logger.Log($"{invocation.GetConcreteMethod().Name} argument(s): {string.Join(", ", invocation.Arguments.Select(a => a))}");
        }

        protected override void PostProceedAction(IInvocation invocation)
        {
            Logger.Log($"{invocation.GetConcreteMethod().Name} result { invocation.ReturnValue}");
        }
        
    }
}
