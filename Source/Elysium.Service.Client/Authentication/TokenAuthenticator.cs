using System;
using System.Globalization;

namespace Elysium.Internal
{
    public class TokenAuthenticator : IAuthenticationHandler
    {
        public void Authenticate(IRequest request, Credentials credentials)
        {
            Ensure.ArgumentNotNull(request, nameof(request));
            Ensure.ArgumentNotNull(credentials, nameof(credentials));
            Ensure.ArgumentNotNull(credentials.Password, nameof(credentials.Password));

            var token = credentials.GetToken();
            if (credentials.Login != null)
            {
                throw new InvalidOperationException("The Login is not null for a token authentication request. You " +
                    "probably did something wrong.");
            }
            if (token != null)
            {
                request.Headers["Authorization"] = string.Format(CultureInfo.InvariantCulture, "Token {0}", token);
            }
        }
    }
}