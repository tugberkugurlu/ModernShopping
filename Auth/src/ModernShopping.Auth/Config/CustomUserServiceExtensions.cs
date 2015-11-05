using Dnx.Identity.MongoDB;
using IdentityServer3.Core.Configuration;
using IdentityServer3.Core.Services;
using Microsoft.AspNet.Identity;
using ModernShopping.Auth.Identity;
using System;

namespace ModernShopping.Auth.Config
{
    public static class CustomUserServiceExtensions
    {
        public static void ConfigureCustomUserService(this IdentityServerServiceFactory factory, IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetService(typeof(UserManager<MongoIdentityUser>)) as UserManager<MongoIdentityUser>;
            factory.UserService = new Registration<IUserService, CustomUserService>();
            factory.Register(new Registration<UserManager<MongoIdentityUser>>(userManager));
        }
    }

    public class CustomUserService : AspNetIdentityUserService<MongoIdentityUser>
    {
        public CustomUserService(UserManager<MongoIdentityUser> userMgr)
            : base(userMgr)
        {
        }
    }
}
