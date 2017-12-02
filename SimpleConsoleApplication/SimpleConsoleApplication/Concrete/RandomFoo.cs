using System;
using System.Linq;
using System.Runtime.InteropServices;
using SimpleConsoleApplication.Interfaces;

namespace SimpleConsoleApplication.Concrete
{
    /// <inheritdoc />
    /// <summary>
    /// Random Foo implementation
    /// </summary>
    public class RandomFoo : IFoo
    {
        private Random _random;
        private const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public int Bar(int seed)
        {
            _random = new Random(seed);
            return _random.Next(0, 100);
        }

        public string Bizz(int seed)
        {
            _random = new Random(seed);
            return new string(Enumerable.Repeat(_chars, _random.Next(1,30))
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }
    }
}