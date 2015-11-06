using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Logging;
using Serilog;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using ModernShopping.Lookup.Middlewares;

namespace ModernShopping.Lookup
{
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;
        private readonly IHostingEnvironment _hostingEnv;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv, ILoggerFactory loggerFactory)
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Logger = serilogLogger;
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddSerilog(serilogLogger);

            _appEnv = appEnv;
            _hostingEnv = hostingEnv;
            _loggerFactory = loggerFactory;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap = new Dictionary<string, string>();

            // IdentityServer3 hardcodes the audience as '{host-address}/resources'.
            // It is suggested to do the validation on scopes.
            // That's why audience validation is disabled with 'ValidateAudience = false' below.
            app.UseJwtBearerAuthentication(options =>
            {
                options.Authority = "http://localhost:44300/";
                options.TokenValidationParameters.ValidateAudience = false;
                options.AutomaticAuthentication = true;

                if (_hostingEnv.IsDevelopment())
                {
                    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataAddress: $"{options.Authority}.well-known/openid-configuration",
                        configRetriever: new OpenIdConnectConfigurationRetriever(),
                        docRetriever: new HttpDocumentRetriever { RequireHttps = false }
                    );
                }
            });

            app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin())
               .UseMiddleware<AuthenticatedUserLoggingMiddleware>()
               .UseMvc();
        }
    }
}
