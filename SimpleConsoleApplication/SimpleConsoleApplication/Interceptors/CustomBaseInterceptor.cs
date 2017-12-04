using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Interceptors
{
    /// <summary>
    /// Base class for custom interceptors
    /// </summary>
    public abstract class CustomBaseInterceptor : IInterceptor
    {
        protected readonly ILogger Logger;

        /// <summary>
        /// Using constructor injection on the interceptor
        /// </summary>
        /// <param name="logger">Logger</param>
        protected CustomBaseInterceptor(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Pre procced action method
        /// </summary>
        protected virtual void PreProceedAction(IInvocation invocation) { }

        /// <summary>
        /// Post procced action method
        /// </summary>
        protected virtual void PostProceedAction(IInvocation invocation) { }

        public void Intercept(IInvocation invocation)
        {
            PreProceedAction(invocation);

            // Calls the decorated instance.
            invocation.Proceed();

            PostProceedAction(invocation);
        }
    }
}
