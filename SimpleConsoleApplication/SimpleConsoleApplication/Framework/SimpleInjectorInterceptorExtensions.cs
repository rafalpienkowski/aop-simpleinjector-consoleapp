﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

using SimpleInjector;

public interface IInterceptor
{
    void Intercept(IInvocation invocation);
}

public interface IInvocation
{
    object InvocationTarget { get; }
    object ReturnValue { get; set; }
    object[] Arguments { get; }
    void Proceed();
    MethodBase GetConcreteMethod();
}

// Extension methods for interceptor registration
// NOTE: These extension methods can only intercept interfaces, not abstract types.
public static class InterceptorExtensions
{
    public static void InterceptWith<TInterceptor>(this Container container,
        Func<Type, bool> predicate)
        where TInterceptor : class, IInterceptor
    {
        container.Options.ConstructorResolutionBehavior.GetConstructor(typeof(TInterceptor));

        var interceptWith = new InterceptionHelper()
        {
            BuildInterceptorExpression =
                e => BuildInterceptorExpression<TInterceptor>(container),
            Predicate = type => predicate(type)
        };

        container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
    }

    public static void InterceptWith(this Container container,
        Func<IInterceptor> interceptorCreator, Func<Type, bool> predicate)
    {
        var interceptWith = new InterceptionHelper()
        {
            BuildInterceptorExpression =
                e => Expression.Invoke(Expression.Constant(interceptorCreator)),
            Predicate = type => predicate(type)
        };

        container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
    }

    public static void InterceptWith(this Container container,
        Func<ExpressionBuiltEventArgs, IInterceptor> interceptorCreator,
        Func<Type, bool> predicate)
    {
        var interceptWith = new InterceptionHelper()
        {
            BuildInterceptorExpression = e => Expression.Invoke(
                Expression.Constant(interceptorCreator),
                Expression.Constant(e)),
            Predicate = type => predicate(type)
        };

        container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
    }

    public static void InterceptWith(this Container container,
        IInterceptor interceptor, Func<Type, bool> predicate)
    {
        var interceptWith = new InterceptionHelper()
        {
            BuildInterceptorExpression = e => Expression.Constant(interceptor),
            Predicate = predicate
        };

        container.ExpressionBuilt += interceptWith.OnExpressionBuilt;
    }

    [DebuggerStepThrough]
    private static Expression BuildInterceptorExpression<TInterceptor>(
        Container container)
        where TInterceptor : class
    {
        var interceptorRegistration = container.GetRegistration(typeof(TInterceptor));

        if (interceptorRegistration == null)
        {
            // This will throw an ActivationException
            container.GetInstance<TInterceptor>();
        }

        return interceptorRegistration.BuildExpression();
    }

    private class InterceptionHelper
    {
        private static readonly MethodInfo NonGenericInterceptorCreateProxyMethod = (
            from method in typeof(Interceptor).GetMethods()
            where method.Name == "CreateProxy"
            where method.GetParameters().Length == 3
            select method)
            .Single();

        internal Func<ExpressionBuiltEventArgs, Expression> BuildInterceptorExpression;
        internal Func<Type, bool> Predicate;

        [DebuggerStepThrough]
        public void OnExpressionBuilt(object sender, ExpressionBuiltEventArgs e)
        {
            if (this.Predicate(e.RegisteredServiceType))
            {
                ThrowIfServiceTypeNotInterface(e);
                e.Expression = this.BuildProxyExpression(e);
            }
        }

        [DebuggerStepThrough]
        private static void ThrowIfServiceTypeNotInterface(ExpressionBuiltEventArgs e)
        {
            // NOTE: We can only handle interfaces, because
            // System.Runtime.Remoting.Proxies.RealProxy only supports interfaces.
            if (!e.RegisteredServiceType.IsInterface)
            {
                throw new NotSupportedException("Can't intercept type " +
                    e.RegisteredServiceType.Name + " because it is not an interface.");
            }
        }

        [DebuggerStepThrough]
        private Expression BuildProxyExpression(ExpressionBuiltEventArgs e)
        {
            var expr = this.BuildInterceptorExpression(e);

            // Create call to
            // (ServiceType)Interceptor.CreateProxy(Type, IInterceptor, object)
            var proxyExpression =
                Expression.Convert(
                    Expression.Call(NonGenericInterceptorCreateProxyMethod,
                        Expression.Constant(e.RegisteredServiceType, typeof(Type)),
                        expr,
                        e.Expression),
                    e.RegisteredServiceType);

            if (e.Expression is ConstantExpression && expr is ConstantExpression)
            {
                return Expression.Constant(CreateInstance(proxyExpression),
                    e.RegisteredServiceType);
            }

            return proxyExpression;
        }

        [DebuggerStepThrough]
        private static object CreateInstance(Expression expression)
        {
            var instanceCreator = Expression.Lambda<Func<object>>(expression,
                new ParameterExpression[0])
                .Compile();

            return instanceCreator();
        }
    }
}

public static class Interceptor
{
    public static T CreateProxy<T>(IInterceptor interceptor, T realInstance) =>
        (T)CreateProxy(typeof(T), interceptor, realInstance);

    [DebuggerStepThrough]
    public static object CreateProxy(Type serviceType, IInterceptor interceptor,
        object realInstance)
    {
        var proxy = new InterceptorProxy(serviceType, realInstance, interceptor);
        return proxy.GetTransparentProxy();
    }

    private sealed class InterceptorProxy : RealProxy
    {
        private static MethodBase GetTypeMethod = typeof(object).GetMethod("GetType");

        private object realInstance;
        private IInterceptor interceptor;

        [DebuggerStepThrough]
        public InterceptorProxy(Type classToProxy, object obj, IInterceptor interceptor)
            : base(classToProxy)
        {
            this.realInstance = obj;
            this.interceptor = interceptor;
        }

        public override IMessage Invoke(IMessage msg)
        {
            if (msg is IMethodCallMessage)
            {
                var message = (IMethodCallMessage)msg;
                return object.ReferenceEquals(message.MethodBase, GetTypeMethod)
                    ? this.Bypass(message)
                    : this.InvokeMethodCall(message);
            }

            return msg;
        }

        private IMessage InvokeMethodCall(IMethodCallMessage msg)
        {
            var i = new Invocation { Proxy = this, Message = msg, Arguments = msg.Args };
            i.Proceeding = () =>
                i.ReturnValue = msg.MethodBase.Invoke(this.realInstance, i.Arguments);
            this.interceptor.Intercept(i);
            return new ReturnMessage(i.ReturnValue, i.Arguments,
                i.Arguments.Length, null, msg);
        }

        private IMessage Bypass(IMethodCallMessage msg)
        {
            object value = msg.MethodBase.Invoke(this.realInstance, msg.Args);
            return new ReturnMessage(value, msg.Args, msg.Args.Length, null, msg);
        }

        private class Invocation : IInvocation
        {
            public Action Proceeding;
            public InterceptorProxy Proxy { get; set; }
            public object[] Arguments { get; set; }
            public IMethodCallMessage Message { get; set; }
            public object ReturnValue { get; set; }
            public object InvocationTarget => this.Proxy.realInstance;
            public void Proceed() => this.Proceeding();
            public MethodBase GetConcreteMethod() => this.Message.MethodBase;
        }
    }
}