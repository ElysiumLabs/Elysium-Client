using Elysium.Internal;
using Elysium.Service.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

#if !HAS_ENVIRONMENT

#endif

namespace Elysium
{
    // NOTE: Every request method must go through the `RunRequest` code path. So if you need to add a
    // new method ensure it goes through there. :)
    /// <summary>
    /// A connection for making HTTP requests against URI endpoints.
    /// </summary>
    public class Connection : IConnection
    {
        private static readonly ICredentialStore _anonymousCredentials = new InMemoryCredentialStore(Credentials.Anonymous);

        private readonly Authenticator _authenticator;
        private readonly JsonHttpPipeline _jsonPipeline;
        private readonly IHttpClient _httpClient;

        public Connection(Uri baseAddress)
            : this(baseAddress, _anonymousCredentials)
        {
        }

        public Connection(Uri baseAddress, ICredentialStore credentialStore)
          : this(baseAddress, credentialStore, new HttpClientAdapter(HttpMessageHandlerFactory.CreateDefault), new NewtonsoftJsonSerializer())
        {
        }

        public Connection(Uri baseAddress, IHttpClient httpClient)
            : this(baseAddress, _anonymousCredentials, httpClient, new NewtonsoftJsonSerializer())
        {
        }

        public Connection(Uri baseAddress, IJsonSerializer serializer)
           : this(baseAddress, _anonymousCredentials, new HttpClientAdapter(HttpMessageHandlerFactory.CreateDefault), serializer)
        {
        }

        public Connection(
            Uri baseAddress,
            ICredentialStore credentialStore,
            IHttpClient httpClient,
            IJsonSerializer serializer)
        {
            Ensure.ArgumentNotNull(baseAddress, nameof(baseAddress));
            Ensure.ArgumentNotNull(credentialStore, nameof(credentialStore));
            Ensure.ArgumentNotNull(httpClient, nameof(httpClient));
            Ensure.ArgumentNotNull(serializer, nameof(serializer));

            if (!baseAddress.IsAbsoluteUri)
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.InvariantCulture, "The base address '{0}' must be an absolute URI",
                        baseAddress), nameof(baseAddress));
            }

            //UserAgent = FormatUserAgent(productInformation);
            BaseAddress = baseAddress;
            _authenticator = new Authenticator(credentialStore);
            _httpClient = httpClient;
            _jsonPipeline = new JsonHttpPipeline(serializer);
        }

        /// <summary>
        /// Base address for the connection.
        /// </summary>
        public Uri BaseAddress { get; private set; }

        public virtual Task<IApiResponse<T>> SendData<T>(
            Uri uri,
            HttpMethod method,
            object body,
            IDictionary<string, string> headers,
            TimeSpan? customTimeout,
            CancellationToken cancellationToken,
            Uri baseAddress = null)
        {
            Ensure.ArgumentNotNull(uri, nameof(uri));

            if (customTimeout != null)
            {
                Ensure.GreaterThanZero(customTimeout.Value, nameof(customTimeout));
            }

            var request = new Request
            {
                Method = method,
                BaseAddress = baseAddress ?? BaseAddress,
                Endpoint = uri,
            };

            headers?.ForEach(x => request.Headers.Add(x.Key, x.Value));

            if (customTimeout != null)
            {
                request.Timeout = customTimeout.Value;
            }

            if (body != null)
            {
                request.Body = body;
            }

            return Run<T>(request, cancellationToken);
        }

        /// <summary>
        /// Gets the <seealso cref="ICredentialStore"/> used to provide credentials for the connection.
        /// </summary>
        public ICredentialStore CredentialStore
        {
            get { return _authenticator.CredentialStore; }
        }

        /// <summary>
        /// Gets or sets the credentials used by the connection.
        /// </summary>
        /// <remarks>
        /// You can use this property if you only have a single hard-coded credential. Otherwise,
        /// pass in an <see cref="ICredentialStore"/> to the constructor. Setting this property will
        /// change the <see cref="ICredentialStore"/> to use the default <see
        /// cref="InMemoryCredentialStore"/> with just these credentials.
        /// </remarks>
        public Credentials Credentials
        {
            get
            {
                var credentialTask = CredentialStore.GetCredentials();
                if (credentialTask == null) return Credentials.Anonymous;
                return credentialTask.Result ?? Credentials.Anonymous;
            }
            // Note this is for convenience. We probably shouldn't allow this to be mutable.
            set
            {
                Ensure.ArgumentNotNull(value, nameof(value));
                _authenticator.CredentialStore = new InMemoryCredentialStore(value);
            }
        }

        protected async Task<IApiResponse<T>> Run<T>(IRequest request, CancellationToken cancellationToken)
        {
            _jsonPipeline.SerializeRequest(request);
            var response = await RunRequest(request, cancellationToken).ConfigureAwait(false);
            return _jsonPipeline.DeserializeResponse<T>(response);
        }

        // THIS IS THE METHOD THAT EVERY REQUEST MUST GO THROUGH!
        private async Task<IResponse> RunRequest(IRequest request, CancellationToken cancellationToken)
        {
            //request.Headers.Add("User-Agent", UserAgent);
            await _authenticator.Apply(request).ConfigureAwait(false);
            var response = await _httpClient.Send(request, cancellationToken).ConfigureAwait(false);
            if (response != null)
            {
                //// Use the clone method to avoid keeping hold of the original (just in case it effect
                //// the lifetime of the whole response
                //_lastApiInfo = response.ApiInfo.Clone();
            }
            HandleErrors(response);
            return response;
        }

        private static readonly Dictionary<HttpStatusCode, Func<IResponse, Exception>> _httpExceptionMap =
            new Dictionary<HttpStatusCode, Func<IResponse, Exception>>
            {
                { HttpStatusCode.Unauthorized, GetExceptionForUnauthorized },
                { HttpStatusCode.Forbidden, GetExceptionForForbidden },
                { HttpStatusCode.NotFound, response => new NotFoundException(response) },
                { (HttpStatusCode)422, response => new ApiValidationException(response) },
            };

        private static void HandleErrors(IResponse response)
        {
            Func<IResponse, Exception> exceptionFunc;
            if (_httpExceptionMap.TryGetValue(response.StatusCode, out exceptionFunc))
            {
                throw exceptionFunc(response);
            }

            if ((int)response.StatusCode >= 400)
            {
                throw new ApiException(response);
            }
        }

        private static Exception GetExceptionForUnauthorized(IResponse response)
        {
            return
                new AuthorizationException(response);
        }

        private static Exception GetExceptionForForbidden(IResponse response)
        {
            string body = response.Body as string ?? "";

            //if (body.Contains("rate limit exceeded"))
            //{
            //    return new RateLimitExceededException(response);
            //}

            //if (body.Contains("number of login attempts exceeded"))
            //{
            //    return new LoginAttemptsExceededException(response);
            //}

            if (body.Contains("abuse-rate-limits") || body.Contains("abuse detection mechanism"))
            {
                return new AbuseException(response);
            }

            return new ForbiddenException(response);
        }

        public void SetRequestTimeout(TimeSpan timeout)
        {
            _httpClient.SetRequestTimeout(timeout);
        }
    }
}