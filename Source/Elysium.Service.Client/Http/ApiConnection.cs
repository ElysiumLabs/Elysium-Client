using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elysium
{
    /// <summary>
    /// A connection for making API requests against URI endpoints. Provides type-friendly
    /// convenience methods that wrap <see cref="IConnection"/> methods.
    /// </summary>
    public class ApiConnection : IApiConnection
    {
        public ApiConnection(IConnection connection)
        {
            Ensure.ArgumentNotNull(connection, nameof(connection));

            Connection = connection;
        }

        public IConnection Connection { get; private set; }

        public async Task<U> InvokeApiAsync<T, U>(string apiName, T body, HttpMethod method, IDictionary<string, string> headers, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(apiName))
            {
                Ensure.ArgumentNotNull(apiName, nameof(apiName));
            }

            var response = await this.Connection.SendData<U>(apiName.FormatUri(), method, body, headers, null, CancellationToken.None);

            return response.Body;
        }
    }
}