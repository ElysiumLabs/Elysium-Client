namespace Elysium
{
    /// <summary>
    /// A connection for making API requests against URI endpoints. Provides type-friendly
    /// convenience methods that wrap <see cref="IConnection"/> methods.
    /// </summary>
    public interface IApiConnection
    {
        IConnection Connection { get; }
    }
}