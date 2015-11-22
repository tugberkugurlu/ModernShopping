using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.OptionsModel;

namespace ModernShopping.Auth.SampleApp
{
    public class JwtBearerAuthSettings 
    {
        public string Authority { get; set; }
    }
    
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;
        private readonly IHostingEnvironment _hostingEnv;
        private readonly ILogger<Startup> _logger;
        private readonly IConfiguration _configuration;

        public Startup(IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv, ILoggerFactory loggerFactory)
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.TextWriter(Console.Out)
                .MinimumLevel.Verbose()
                .CreateLogger();

            var config = new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables("ModernShoppingAuthSampleApp_")
                .Build();

            Log.Logger = serilogLogger;
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddSerilog(serilogLogger);

            _appEnv = appEnv;
            _hostingEnv = hostingEnv;
            _logger = new Logger<Startup>(loggerFactory);
            _configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<JwtBearerAuthSettings>(_configuration.GetSection("JwtBearerAuth"));
            services.AddCors();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IOptions<JwtBearerAuthSettings> jwtAuthSettings)
        {   
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap = new Dictionary<string, string>();
            
            app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());

            _logger.LogInformation($"Configuring JwtBearerAuthentication with Authority {jwtAuthSettings.Value.Authority}");

            // IdentityServer3 hardcodes the audience as '{host-address}/resources'.
            // It is suggested to do the validation on scopes.
            // That's why audience validation is disabled with 'ValidateAudience = false' below.
            app.UseJwtBearerAuthentication(options =>
            {
                options.Authority = jwtAuthSettings.Value.Authority;
                options.AutomaticAuthentication = true;
                options.TokenValidationParameters.ValidateAudience = false;

                if (_hostingEnv.IsDevelopment())
                {
                    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                        metadataAddress: $"{options.Authority}.well-known/openid-configuration",
                        configRetriever: new OpenIdConnectConfigurationRetriever(),
                        docRetriever: new HttpDocumentRetriever { RequireHttps = false }
                    );
                }
            });

            app.UseMiddleware<RequiredScopesMiddleware>(new List<string> { "write" });
            app.UseMvcWithDefaultRoute();
        }
    }
}
