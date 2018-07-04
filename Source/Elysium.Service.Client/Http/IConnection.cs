using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Elysium
{
    public interface IConnection
    {
        Uri BaseAddress { get; }

        ICredentialStore CredentialStore { get; }

        Credentials Credentials { get; set; }

        void SetRequestTimeout(TimeSpan timeout);

        Task<IApiResponse<T>> SendData<T>(
            Uri uri,
            HttpMethod method,
            object body,
            IDictionary<string, string> headers,
            TimeSpan? customTimeout,
            CancellationToken cancellationToken,
            Uri baseAddress = null);
    }
}