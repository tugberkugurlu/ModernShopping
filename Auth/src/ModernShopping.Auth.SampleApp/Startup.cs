using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;

namespace ModernShopping.Auth.SampleApp
{
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IApplicationEnvironment appEnv, ILoggerFactory loggerFactory)
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.TextWriter(Console.Out)
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Logger = serilogLogger;
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddSerilog(serilogLogger);

            _appEnv = appEnv;
            _loggerFactory = loggerFactory;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap = new Dictionary<string, string>();

            app.UseJwtBearerAuthentication(options =>
            {
                options.Authority = "https://localhost:44300/";
                options.Audience = "https://localhost:44300/resources";
                options.AutomaticAuthentication = true;
            });

            app.UseCors(policy => policy.WithOrigins("*"));
            app.UseMiddleware<RequiredScopesMiddleware>(new List<string> { "write" });
            app.UseMvcWithDefaultRoute();
        }
    }
}
