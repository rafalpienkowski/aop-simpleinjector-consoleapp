### Introduction
___

In this article I want to show you an example of AOP usage in a .Net real life scenario. My goal is to show you easy is to plugin interception using build-in [RealProxy](https://msdn.microsoft.com/en-us/library/system.runtime.remoting.proxies.realproxy(v=vs.110).aspx) class. 

At the beginning I want you to tell few words about Aspect Oriented Programming (AOP). I really like the definition from [Wikipedia](https://en.wikipedia.org/wiki/Aspect-oriented_programming):

>_Aspect-oriented programming (AOP) is a programming paradigm that aims to increase modularity by allowing the separation of cross-cutting concerns. It does so by adding additional behavior to existing code without modifying the code itself._

It sounds easy and it's very simple. Aspect Oriented Programming paradigm help us to achieve _Open/Close Principle_ from _SOLID principles_. By the way _Open/Close Principle_ stands for _O_ from acronym SOLID.If you aren't familiar with Open/Close or SOLID principle I encourage you to read some articles from [dev.to](https://dev.to/theodesp/understanding-solid-principles-open-closed-principle-5h0) or just type "solid" in any web search engine you like.

AOP can be reached in two main ways, by:
- [the decorator pattern](https://en.wikipedia.org/wiki/Decorator_pattern)
- [the interceptor pattern](https://en.wikipedia.org/wiki/Interceptor_pattern)

First approach with _the decorator pattern_ is more precise. We can apply AOP to specific method in specific implementation. On the other hand working with _the interceptor pattern_ will be more beneficial in scenarious which are requiring more general approach.

I intentionally didn't dive deeper into details of AOP. It's a separate branch of science. I assume that you've minimal knowledge about AOP and you're wondering if or how apply AOP in daily work. In my opinion the easiest way to understand a problem is to work with an example, so lets begin our story...


### Story background
___

Lets imagine such a scenario. A manager comes to us and tells us:
> We've problem with performance of our application. You're the best developer in our company. You need to investigate and fix poor performance issue as soon as possible. 

By the way how many times did you hear similar speech in the past? You don't have to answer.

![wink](https://media.giphy.com/media/wrBURfbZmqqXu/giphy.gif)

### Example solution
___

I've set up an [GitHub repository](https://github.com/rafalpienkowski/aop-simpleinjector-consoleapp) where I've created a simple console application which is using AOP using [SimpleInjector](https://simpleinjector.org/index.html) dependency injection (DI) library. 

Lets treat this project as our poor performance, legacy application.

Fortunately, previous dev team used an Dependency Inversion principle. They've created well defined IFoo interface which is crucial for our application. It consists of two methods: _Bar_ and _Baz_. Below you can see code snippet for this interface:

```csharp
public interface IFoo
{
    int Bar(int seed);
    string Baz(int seed);
}
```

We have only one implementation of IFoo interface which is in use in our application. It generates some random ints and strings based on build-in Random class. Below is a code snippet for RandomFoo class which is IFoo concrete implementation:

```csharp
public class RandomFoo : IFoo
{
    private Random _random;
    private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public int Bar(int seed)
    {
        _random = new Random(seed);
        return _random.Next(0, 100);
    }

    public string Baz(int seed)
    {
        _random = new Random(seed);
        return new string(Enumerable.Repeat(_chars, _random.Next(1,30))
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }
}
```

Listing below shows fundamental application functionality where IFoo interface is used:


```csharp
static void Main(string[] args)
{
    var foo = Container.GetInstance<IFoo>();
    Console.WriteLine($"Result for Bar(): {foo.Bar(DateTime.Now.Millisecond)}");
    Console.WriteLine($"Result for Baz(): {foo.Baz(DateTime.Now.Millisecond)}");
    Console.ReadKey();
}
```

and an example output from our appliction looks like:

```
Result for Bar(): 8
Result for Baz(): MIMRKA9BJUKQ
```

Now you're aware how our situation looks like. What would you like to do as a first step? Are you thinking about changing current implementation of _RandomFoo_? Add some [System.Diagnostic.Stopwatches](https://msdn.microsoft.com/en-us/library/system.diagnostics.stopwatch(v=vs.110).aspx) to check method execution times? 

![Thinking Dobromir](https://i.makeagif.com/media/5-04-2015/ehcG6h.gif)

Did you think about AOP? No? I'm going to show you how can you use AOP and  concept of _Interceptor_ to solve this problem without change of any single line of code in _RandomFoo_ concrete.

First I want to explain you what an _Interceptor_ is? An _Interceptor_ wraps calls to method so we can add some logic before and after we actually will call the right _RandomFoo_ implementation. Image below shows the concept of _Interceptor_ and its usage:

![interceptor](https://thepracticaldev.s3.amazonaws.com/i/8pa9o868t8tr885zf8xt.png)


So lets start our work. First what we need to do is to implement an _IInterceptor_ interface which is a part of _SimpleInjector_ framework. This interface consists of one method `void Intercept(IInvocation)` which has to be implemented in derived class. 

Below an example implementation of **MonitoringInterceptor** class is shown:


```csharp
public class MonitoringInterceptor : IInterceptor
{
    private readonly ILogger _logger;

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

        _logger.Log($"{decoratedType.Name}.{invocation.GetConcreteMethod().Name}()
     executed in {watch.ElapsedMilliseconds} ms.");
    }
}
```

As we can see implementation of _IInterceptor_'s interface method wraps every call. That allow us to add some extra code before and after every call. In our case I've added a [Stopwatch](https://msdn.microsoft.com/en-us/library/system.diagnostics.stopwatch(v=vs.110).aspx) to calculate method's execution time which is logged by _ILogger_ implementation.

__*Note:*__ _In my repository I've introduced an extra abstraction between IInterceptor interface and its concrete implementation (I hope I didn't make over engineering in). To make the example cleaner I show you code without an extra abstraction. [Link](https://github.com/rafalpienkowski/aop-simpleinjector-consoleapp/blob/master/SimpleConsoleApplication/SimpleConsoleApplication/Interceptors/CustomBaseInterceptor.cs)_

After I've created an interceptor, the only thing I need to do is to register it. During a registration of an interceptor we need to specify on which interface methods will it be used.

Example interceptor's registration is listed below:

```csharp
//Interceptor registration
Container.InterceptWith<MonitoringInterceptor>(type => type == typeof(IFoo));
```

After I finished implementation of my first interceptor I realized that there will be nice to log method's arguments and response too. So I've created second interceptor which covers that functionality. It's listed below:


```csharp
public class LoggingInterceptor : IInterceptor
{
    private readonly ILogger _logger;

    public LoggingInterceptor(ILogger logger)
    {
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        _logger.Log($"{invocation.GetConcreteMethod().Name} argument(s):
                 {string.Join(", ",invocation.Arguments.Select(a => a))}");

        // Calls the decorated instance.
        invocation.Proceed();

        _logger.Log($"{invocation.GetConcreteMethod().Name} result 
                    {invocation.ReturnValue}");

    }
}
```
Our program's output after we've added _Monitoring-_ and _LoggingInterceptor_ is presented in code snippet below:

```
Bar argument(s): 908
RandomFoo.Bar() executed in 0 ms.
Bar result 8
Result for Bar(): 8

Bizz argument(s): 922
RandomFoo.Baz() executed in 4 ms.
Bizz result MIMRKA9BJUKQ
Result for Baz(): MIMRKA9BJUKQ
```

I think that with that knowledge we can make some tests and start work of applications performance improvement.


### Summary
___

We end our day with application which is richer of logs and performance monitoring. We achieved that without any change in _RandomFoo_ concrete implementation. Furthermore we can utilize out interceptor is the future for other (even not created) interfaces and its implementations. Furthermore we can in few steps revert our application to the previous state. Any revert merge isn't required. Changes in dependency configuration are enough.

Be careful about changing the value of the arguments and/or results. You shouldn't do that. This isn't good practice and it could confuse you in the future because it isn't easy to find where and why values were changed. Interceptors weren't designed for something like that. 

![Hammer in finger](http://assets8.heart.co.uk/2012/39/oops-1348495688-large-article-1.jpg)

In given example I've used SimpleInjector DI framework. Most of DI frameworks support AOP. Here is list of some DIs with links to documentation which describes how to achieve Aspect Oriented Programming paradigm. Check if your DI supports AOP too.
- [SimpleInjector](https://simpleinjector.readthedocs.io/en/latest/aop.html#)
- [Unity](https://msdn.microsoft.com/en-us/library/dn178466(v=pandp.30).aspx)
- [Autofac](http://autofaccn.readthedocs.io/en/latest/advanced/interceptors.html)


I hope you enjoy this article. I'm trying to show you new possibilities and help you what to type in Google if you would face such a problem in the future. You can use my tip or not. The choice is yours.

### Extras
___

- I've made my draws with [draw.io](https://www.draw.io/)
- Are you wondering why I pasted an image of a car at the beginning of this article? The answer is easy. This car's name is [Jensen Interceptor](https://en.wikipedia.org/wiki/Jensen_Interceptor) which has been manufactured in Great Britain. Such a joke from me.