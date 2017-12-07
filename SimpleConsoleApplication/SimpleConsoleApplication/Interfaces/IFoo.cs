namespace SimpleConsoleApplication.Interfaces
{
    /// <summary>
    /// Example interface
    /// </summary>
    public interface IFoo
    {
        /// <summary>
        /// Bar
        /// </summary>
        /// <param name="seed">Seed for Random generator</param>
        /// <returns>Returns some int</returns>
        int Bar(int seed);

        /// <summary>
        /// Baz
        /// </summary>
        /// <param name="seed">Seed for Random generator</param>
        /// <returns>Returns some string</returns>
        string Baz(int seed);
    }
}