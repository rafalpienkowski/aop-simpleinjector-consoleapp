using System.Diagnostics;
using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Interceptors
{
    /// <inheritdoc />
    /// <summary>
    /// Monitoring interceptor to check method execution time
    /// </summary>
    public class MonitoringInterceptor : CustomBaseInterceptor
    {
        private Stopwatch _watch;

        // Using constructor injection on the interceptor
        public MonitoringInterceptor(ILogger logger) : base(logger){ }
        
        protected override void PreProceedAction(IInvocation invocation)
        {
            _watch = Stopwatch.StartNew();
        }

        protected override void PostProceedAction(IInvocation invocation)
        {
            var decoratedType = invocation.InvocationTarget.GetType();

            Logger.Log($"{decoratedType.Name}.{invocation.GetConcreteMethod().Name}() executed in {_watch.ElapsedMilliseconds} ms.");
        }
    }
}
