using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Server.Kestrel.Https;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ModernShopping.Auth.SampleApp
{
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;
        private readonly IHostingEnvironment _hostingEnv;
        private readonly ILoggerFactory _loggerFactory;
        private readonly X509Certificate2 _certificate;

        public Startup(IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv, ILoggerFactory loggerFactory)
        {
            var serverCertPath = Path.Combine(appEnv.ApplicationBasePath, "servercert.pfx");
            var certificate = new X509Certificate2(serverCertPath, "testPassword");
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.TextWriter(Console.Out)
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Logger = serilogLogger;
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddSerilog(serilogLogger);

            _appEnv = appEnv;
            _hostingEnv = hostingEnv;
            _loggerFactory = loggerFactory;
            _certificate = certificate;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap = new Dictionary<string, string>();

            app.UseKestrelHttps(_certificate);

            // IdentityServer3 hardcodes the audience as '{host-address}/resources'.
            // It is suggested to do the validation on scopes.
            // That's why audience validation is disabled with 'ValidateAudience = false' below.
            app.UseJwtBearerAuthentication(options =>
            {
                options.Authority = "https://localhost:44300/";
                options.TokenValidationParameters.ValidateAudience = false;
            });

            app.UseCors(policy => policy.WithOrigins("*"));
            app.UseMiddleware<RequiredScopesMiddleware>(new List<string> { "write" });
            app.UseMvcWithDefaultRoute();
        }
    }
}
