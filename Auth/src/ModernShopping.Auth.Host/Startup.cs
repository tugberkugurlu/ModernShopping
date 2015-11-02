using IdentityServer3.Core.Configuration;
using ModernShopping.Auth.Host.Config;
using Owin;
using Serilog;

namespace ModernShopping.Auth.Host
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Trace(outputTemplate: "{Timestamp} [{Level}] ({Name}){NewLine} {Message}{NewLine}{Exception}")
                .CreateLogger();

            var factory = new IdentityServerServiceFactory()
                        .UseInMemoryUsers(Users.Get())
                        .UseInMemoryClients(Clients.Get())
                        .UseInMemoryScopes(Scopes.Get());

            var options = new IdentityServerOptions
            {
                SigningCertificate = Certificate.Get(),
                Factory = factory,
                SiteName = "ModernShopping"
            };

            appBuilder.UseIdentityServer(options);
        }
    }
}