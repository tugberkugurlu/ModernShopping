using Microsoft.Framework.Logging;
using Microsoft.AspNet.Builder;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace ModernShopping.Lookup.Middlewares
{
    public class AuthenticatedUserLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticatedUserLoggingMiddleware> _logger;

        public AuthenticatedUserLoggingMiddleware(RequestDelegate next, ILogger<AuthenticatedUserLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public Task Invoke(HttpContext context)
        {
            var user = context.User;
            if (user.Identity.IsAuthenticated)
            {
                var subject = user.FindFirst("sub");
                var clientId = user.FindFirst("client_id");

                _logger.LogInformation("Authenticated as {subject} through {clientId}", subject.Value, clientId.Value);
            }
            else
            {
                _logger.LogInformation("Not authenticated");
            }

            return _next(context);
        }
    }
}
