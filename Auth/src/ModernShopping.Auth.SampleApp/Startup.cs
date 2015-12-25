using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer3.AccessTokenValidation;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Owin.Logging;
using Owin;
using Serilog;
using Microsoft.Owin.BuilderProperties;

namespace ModernShopping.Auth.SampleApp
{
    using Microsoft.Extensions.PlatformAbstractions;
    using DataProtectionProviderDelegate = Func<string[], Tuple<Func<byte[], byte[]>, Func<byte[], byte[]>>>;
    using DataProtectionTuple = Tuple<Func<byte[], byte[]>, Func<byte[], byte[]>>;

    internal class OwinLogger : Microsoft.Owin.Logging.ILogger
    {   
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        
        internal OwinLogger(Microsoft.Extensions.Logging.ILogger logger) 
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));   
            }
            
            _logger = logger;
        }
        
        public bool WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            _logger.Log(MapToLogLevel(eventType), eventId, state, exception, formatter);
            return true;
        }
        
        private LogLevel MapToLogLevel(TraceEventType eventType)
        {
            LogLevel logLevel;
            
            switch (eventType)
            {
                case TraceEventType.Critical:
                    logLevel = LogLevel.Critical;
                    break;
                    
                case TraceEventType.Error:
                    logLevel = LogLevel.Error;
                    break;
                    
                case TraceEventType.Information:
                    logLevel = LogLevel.Information;
                    break;
                    
                case TraceEventType.Resume:
                    logLevel = LogLevel.Verbose;
                    break;
                    
                case TraceEventType.Start:
                    logLevel = LogLevel.Verbose;
                    break;
                    
                case TraceEventType.Stop:
                    logLevel = LogLevel.Verbose;
                    break;
                    
                case TraceEventType.Suspend:
                    logLevel = LogLevel.Verbose;
                    break;
                    
                case TraceEventType.Transfer:
                    logLevel = LogLevel.Verbose;
                    break;
                    
                case TraceEventType.Verbose:
                    logLevel = LogLevel.Verbose;
                    break;
                    
                case TraceEventType.Warning:
                    logLevel = LogLevel.Warning;
                    break;
                    
                default:
                    logLevel = LogLevel.Information;
                    break;
            }
            
            return logLevel;
        }
    }

    internal class OwinLoggerFactory : Microsoft.Owin.Logging.ILoggerFactory
    {
        private readonly Microsoft.Extensions.Logging.ILoggerFactory _loggerFactory;
        
        internal OwinLoggerFactory(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));   
            }
                
            _loggerFactory = loggerFactory;
        }
        
        public Microsoft.Owin.Logging.ILogger Create(string name)
        {
            var logger = _loggerFactory.CreateLogger(name);
            
            return new OwinLogger(logger); 
        }
    }

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

        public Startup(IApplicationEnvironment appEnv, IHostingEnvironment hostingEnv, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
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
            // horrible hack to make identity IdentityServerBearerTokenAuthentication not try to connect
            // to authority immedietly. More info: https://github.com/IdentityServer/IdentityServer3.AccessTokenValidation/issues/57 
            Thread.Sleep(5000);
            
            _logger.LogInformation($"Configuring JwtBearerAuthentication with Authority {jwtAuthSettings.Value.Authority}");
            
            app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            app.UseIdentityServerBearerTokenAuthentication(new IdentityServerBearerTokenAuthenticationOptions
            {
                Authority = jwtAuthSettings.Value.Authority,
                ClientId = "write",
                ClientSecret = "secret"
            });

            app.UseMiddleware<RequiredScopesMiddleware>(new List<string> { "write" });
            app.UseMvcWithDefaultRoute();
        }
    }

    public static class IApplicationBuilderExtensions
    {
        public static void UseIdentityServerBearerTokenAuthentication(this IApplicationBuilder app, IdentityServerBearerTokenAuthenticationOptions options)
        {
            app.UseOwin(addToPipeline =>
            {   
                addToPipeline(next =>
                {
                    var builder = new Microsoft.Owin.Builder.AppBuilder();
                    var loggerFactory = app.ApplicationServices.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
                    var lifetime = app.ApplicationServices.GetService<IApplicationLifetime>();
                    var owinLoggerFactory = new OwinLoggerFactory(loggerFactory);
                    var provider = app.ApplicationServices.GetService(typeof(Microsoft.AspNet.DataProtection.IDataProtectionProvider)) as Microsoft.AspNet.DataProtection.IDataProtectionProvider;

                    var properties = new AppProperties(builder.Properties);
                    properties.OnAppDisposing = lifetime.ApplicationStopping;
                    properties.DefaultApp = next;

                    builder.SetLoggerFactory(owinLoggerFactory);
                    builder.Properties["security.DataProtectionProvider"] = new DataProtectionProviderDelegate(purposes =>
                    {
                        var dataProtection = provider.CreateProtector(string.Join(",", purposes));
                        return new DataProtectionTuple(dataProtection.Protect, dataProtection.Unprotect);
                    });
                    
                    builder.UseIdentityServerBearerTokenAuthentication(options);
                    return builder.Build(typeof(Func<IDictionary<string, object>, Task>)) as Func<IDictionary<string, object>, Task>;
                });
            });
        }
    }
}
