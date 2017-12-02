using System;
using SimpleConsoleApplication.Concrete;
using SimpleConsoleApplication.Interceptors;
using SimpleConsoleApplication.Interfaces;
using SimpleInjector;

namespace SimpleConsoleApplication
{
    class Program
    {
        private static readonly Container Container;

        static Program()
        {
            Container = new Container();
            Container.Register<ILogger, ConsoleLogger>();
            Container.Register<IFoo, RandomFoo>();
            
            //Interceptor registration
            Container.InterceptWith<MonitoringInterceptor>(type => type == typeof(IFoo));
            Container.InterceptWith<LoggingInterceptor>(type => type == typeof(IFoo));

            Container.Verify();
        }

        static void Main(string[] args)
        {
            var foo = Container.GetInstance<IFoo>();
            Console.WriteLine($"Result for Bar(): {foo.Bar(DateTime.Now.Millisecond)}");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine($"Result for Bizz(): {foo.Bizz(DateTime.Now.Millisecond)}");
            Console.ReadKey();
        }
    }
}
