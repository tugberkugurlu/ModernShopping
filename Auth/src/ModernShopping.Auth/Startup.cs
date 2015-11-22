using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using ModernShopping.Auth.Config;
using Microsoft.Dnx.Runtime;
using System.IO;
using IdentityServer3.Core.Configuration;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Framework.Logging;
using Serilog;
using Microsoft.Framework.OptionsModel;
using Microsoft.AspNet.Identity;
using System.Security.Claims;
using Dnx.Identity.MongoDB;
using MongoDB.Driver;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Configuration;
using Owin;
using Microsoft.Owin.Security.Google;
using System.Security.Cryptography;

namespace ModernShopping.Auth
{
    public class MongoDbSettings 
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
    
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IConfiguration _configuration;

        public Startup(IApplicationEnvironment appEnv, ILoggerFactory loggerFactory)
        {
            var serilogLogger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Verbose()
                .CreateLogger();
                
            var config = new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables("ModernShoppingAuth_")
                .Build();

            Log.Logger = serilogLogger;
            loggerFactory.MinimumLevel = LogLevel.Debug;
            loggerFactory.AddSerilog(serilogLogger);

            _appEnv = appEnv;
            _loggerFactory = loggerFactory;
            _configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<MongoDbSettings>(_configuration.GetSection("MongoDb"));
               
            services.AddSingleton<IUserStore<MongoIdentityUser>>(provider =>
            {
                var options = provider.GetService<IOptions<MongoDbSettings>>();
                var client = new MongoClient(options.Value.ConnectionString);
                var database = client.GetDatabase(options.Value.DatabaseName);
                var loggerFactory = provider.GetService<ILoggerFactory>();

                return new MongoUserStore<MongoIdentityUser>(database, loggerFactory);
            });

            services.Configure<IdentityOptions>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
            });

            services.AddOptions();
            services.AddDataProtection();
            services.TryAddSingleton<IdentityMarkerService>();
            services.TryAddSingleton<IUserValidator<MongoIdentityUser>, UserValidator<MongoIdentityUser>>();
            services.TryAddSingleton<IPasswordValidator<MongoIdentityUser>, PasswordValidator<MongoIdentityUser>>();
            services.TryAddSingleton<IPasswordHasher<MongoIdentityUser>, PasswordHasher<MongoIdentityUser>>();
            services.TryAddSingleton<ILookupNormalizer, UpperInvariantLookupNormalizer>();
            services.TryAddSingleton<IdentityErrorDescriber>();
            services.TryAddSingleton<ISecurityStampValidator, SecurityStampValidator<MongoIdentityUser>>();
            services.TryAddSingleton<IUserClaimsPrincipalFactory<MongoIdentityUser>, UserClaimsPrincipalFactory<MongoIdentityUser>>();
            services.TryAddSingleton<UserManager<MongoIdentityUser>, UserManager<MongoIdentityUser>>();
            services.TryAddScoped<SignInManager<MongoIdentityUser>, SignInManager<MongoIdentityUser>>();

            AddDefaultTokenProviders(services);
        }

        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider)
        {
            var idSvrFactory = IdSrvFactory.Configure();
            idSvrFactory.ConfigureCustomUserService(serviceProvider);

            var idsrvOptions = new IdentityServerOptions
            {    
                SiteName = "ModernShopping",
                SigningCertificate = LoadCertificate(),
                Factory = idSvrFactory,
                RequireSsl = false,

                AuthenticationOptions = new AuthenticationOptions
                {
                    EnablePostSignOutAutoRedirect = true,
                    IdentityProviders = ConfigureIdentityProviders
                }
            };

            app.UseDeveloperExceptionPage();
            app.UseIdentityServer(idsrvOptions);
        }
        
        // Taken from https://github.com/PinpointTownes/AspNet.Security.OpenIdConnect.Server/blob/e405908e9ed10f6eb5b10e5d3c6cf75cd0bf4926/samples/Mvc/Mvc.Server/Startup.cs#L156-L171
        // Related to https://github.com/dotnet/corefx/issues/424, https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/179 
        // Note: in a real world app, you'd probably prefer storing the X.509 certificate
        // in the user or machine store. To keep this sample easy to use, the certificate
        // is extracted from the Certificate.cer/pfx file embedded in this assembly.
        private X509Certificate2 LoadCertificate()
        {
            var cerFile = Path.Combine(_appEnv.ApplicationBasePath, "certs", "Certificate.cer");
            using (var stream = File.OpenRead(cerFile))
            using (var memStream = new MemoryStream())
            {
                stream.CopyTo(memStream);
                memStream.Flush();
                
                return new X509Certificate2(memStream.ToArray()) 
                {
                    PrivateKey = LoadPrivateKey()
                }; 
            }
        }
        
        // Taken from https://github.com/PinpointTownes/AspNet.Security.OpenIdConnect.Server/blob/e405908e9ed10f6eb5b10e5d3c6cf75cd0bf4926/samples/Mvc/Mvc.Server/Startup.cs#L182-L207
        // Note: CoreCLR doesn't support .pfx files yet. To work around this limitation, the private key
        // is stored in a different - an totally unprotected/unencrypted - .keys file and attached to the
        // X509Certificate2 instance in LoadCertificate : NEVER do that in a real world application.
        // See https://github.com/dotnet/corefx/issues/424
        private RSA LoadPrivateKey()
        {
            var keyFile = Path.Combine(_appEnv.ApplicationBasePath, "certs", "Certificate.keys");
            using (var stream = File.OpenRead(keyFile))
            using (var reader = new StreamReader(stream))
            {
                // See https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/179
                var key = new RSACryptoServiceProvider(new CspParameters { ProviderType = 24 });
                
                key.ImportParameters(new RSAParameters 
                {
                    D = Convert.FromBase64String(reader.ReadLine()),
                    DP = Convert.FromBase64String(reader.ReadLine()),
                    DQ = Convert.FromBase64String(reader.ReadLine()),
                    Exponent = Convert.FromBase64String(reader.ReadLine()),
                    InverseQ = Convert.FromBase64String(reader.ReadLine()),
                    Modulus = Convert.FromBase64String(reader.ReadLine()),
                    P = Convert.FromBase64String(reader.ReadLine()),
                    Q = Convert.FromBase64String(reader.ReadLine())
                });

                return key;
            }
        }

        private void AddDefaultTokenProviders(IServiceCollection services)
        {
            var dataProtectionProviderType = typeof(DataProtectorTokenProvider<>).MakeGenericType(typeof(MongoIdentityUser));
            var phoneNumberProviderType = typeof(PhoneNumberTokenProvider<>).MakeGenericType(typeof(MongoIdentityUser));
            var emailTokenProviderType = typeof(EmailTokenProvider<>).MakeGenericType(typeof(MongoIdentityUser));
            AddTokenProvider(services, TokenOptions.DefaultProvider, dataProtectionProviderType);
            AddTokenProvider(services, TokenOptions.DefaultEmailProvider, emailTokenProviderType);
            AddTokenProvider(services, TokenOptions.DefaultPhoneProvider, phoneNumberProviderType);
        }

        private void AddTokenProvider(IServiceCollection services, string providerName, Type provider)
        {
            services.Configure<IdentityOptions>(options =>
            {
                options.Tokens.ProviderMap[providerName] = new TokenProviderDescriptor(provider);
            });

            services.AddSingleton(provider);
        }
        
        public static void ConfigureIdentityProviders(IAppBuilder app, string signInAsType)
        {
            var google = new GoogleOAuth2AuthenticationOptions
            {
                AuthenticationType = "Google",
                Caption = "Google",
                SignInAsAuthenticationType = signInAsType,
                ClientId = "674291401959-pqa8540v0ul76gcnfiim7jkkrkknke4d.apps.googleusercontent.com",
                ClientSecret = "rbW3zesQcLxhy7yvazxiv25e"
            };
            
            app.UseGoogleAuthentication(google);
        }
    }

    public class UserClaimsPrincipalFactory<TUser> : IUserClaimsPrincipalFactory<TUser>
        where TUser : class
    {
        public UserClaimsPrincipalFactory(
            UserManager<TUser> userManager,
            IOptions<IdentityOptions> optionsAccessor)
        {
            if (userManager == null)
            {
                throw new ArgumentNullException(nameof(userManager));
            }
            if (optionsAccessor == null || optionsAccessor.Value == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            UserManager = userManager;
            Options = optionsAccessor.Value;
        }

        public UserManager<TUser> UserManager { get; private set; }

        public IdentityOptions Options { get; private set; }

        public virtual async Task<ClaimsPrincipal> CreateAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userId = await UserManager.GetUserIdAsync(user);
            var userName = await UserManager.GetUserNameAsync(user);
            var id = new ClaimsIdentity(Options.Cookies.ApplicationCookieAuthenticationScheme,
                Options.ClaimsIdentity.UserNameClaimType,
                Options.ClaimsIdentity.RoleClaimType);
            id.AddClaim(new Claim(Options.ClaimsIdentity.UserIdClaimType, userId));
            id.AddClaim(new Claim(Options.ClaimsIdentity.UserNameClaimType, userName));
            if (UserManager.SupportsUserSecurityStamp)
            {
                id.AddClaim(new Claim(Options.ClaimsIdentity.SecurityStampClaimType,
                    await UserManager.GetSecurityStampAsync(user)));
            }
            if (UserManager.SupportsUserRole)
            {
                var roles = await UserManager.GetRolesAsync(user);
                foreach (var roleName in roles)
                {
                    id.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, roleName));
                }
            }
            if (UserManager.SupportsUserClaim)
            {
                id.AddClaims(await UserManager.GetClaimsAsync(user));
            }

            return new ClaimsPrincipal(id);
        }
    }
}
