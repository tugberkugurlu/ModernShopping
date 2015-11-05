using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using ModernShopping.Auth.Config;
using Microsoft.Dnx.Runtime;
using System.IO;
using IdentityServer3.Core.Configuration;
using System.Security.Cryptography.X509Certificates;
using ModernShopping.Auth.Host.Config;

namespace ModernShopping.Auth
{
    public class Startup
    {
        private readonly IApplicationEnvironment _appEnv;

        public Startup(IApplicationEnvironment appEnv)
        {
            _appEnv = appEnv;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection();
        }

        public void Configure(IApplicationBuilder app)
        {
            // TOOD: PR this to https://github.com/IdentityServer/IdentityServer3.Samples/blob/master/source/AspNet5Host/src/IdentityServerAspNet5/Startup.cs#L21
            var certFile = Path.Combine(_appEnv.ApplicationBasePath, "idsrv3test.pfx");
            var idsrvOptions = new IdentityServerOptions
            {
                SigningCertificate = new X509Certificate2(certFile, "idsrv3test"),
                RequireSsl = false,

                AuthenticationOptions = new AuthenticationOptions
                {
                    EnablePostSignOutAutoRedirect = true
                },

                Factory = new IdentityServerServiceFactory()
                    .UseInMemoryUsers(Users.Get())
                    .UseInMemoryClients(Clients.Get())
                    .UseInMemoryScopes(Scopes.Get())
            };

            app.UseDeveloperExceptionPage();
            app.UseIdentityServer(idsrvOptions);
        }
    }
}
