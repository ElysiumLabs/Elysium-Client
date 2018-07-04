using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elysium.Service.Client
{
    public class ServiceClient
    {
        public IConnection Connection { get; private set; }

        private ApiConnection apiConnection;

        public ServiceClient(Uri baseUri)
            : this(new Connection(baseUri))
        {
        }

        public ServiceClient(IConnection connection)
        {
            Ensure.ArgumentNotNull(connection, nameof(connection));

            Connection = connection;
            apiConnection = new ApiConnection(connection);
        }

        public Task<T> InvokeApiAsync<T>(string apiName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.apiConnection.InvokeApiAsync<string, T>(apiName, null, HttpMethod.Get, null, cancellationToken);
        }

        public Task<U> InvokeApiAsync<T, U>(string apiName, T body, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.apiConnection.InvokeApiAsync<T, U>(apiName, body, HttpMethod.Post, null, cancellationToken);
        }

        public Task<T> InvokeApiAsync<T>(string apiName, HttpMethod method, IDictionary<string, string> parameters, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.apiConnection.InvokeApiAsync<string, T>(apiName, null, method, parameters, cancellationToken);
        }
    }
}