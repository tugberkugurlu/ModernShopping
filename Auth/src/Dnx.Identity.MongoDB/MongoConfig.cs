using Dnx.Identity.MongoDB.Models;
using Microsoft.AspNet.Identity;
using MongoDB.Bson.Serialization;
using System.Threading;

namespace Dnx.Identity.MongoDB
{
    internal static class MongoConfig
    {
        private static bool _initialized = false;
        private static object _initializationLock = new object();
        private static object _initializationTarget;

        public static void EnsureConfigured()
        {
            EnsureConfiguredImpl();
        }

        private static void EnsureConfiguredImpl()
        {
            LazyInitializer.EnsureInitialized(ref _initializationTarget, ref _initialized, ref _initializationLock, () =>
            {
                Configure();
                return null;
            });
        }

        private static void Configure()
        {
            BsonClassMap.RegisterClassMap<MongoIdentityUser>(cm =>
            {
                cm.AutoMap();
                cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                cm.MapCreator(user => new MongoIdentityUser(user.UserName, user.Email));
            });

            BsonClassMap.RegisterClassMap<MongoUserClaim>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(c => new MongoUserClaim(c.ClaimType, c.ClaimValue));
            });

            BsonClassMap.RegisterClassMap<MongoUserEmail>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(cr => new MongoUserEmail(cr.Value));
            });

            BsonClassMap.RegisterClassMap<MongoUserPhoneNumber>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(cr => new MongoUserPhoneNumber(cr.Value));
            });

            BsonClassMap.RegisterClassMap<MongoUserLogin>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(l => new MongoUserLogin(new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)));
            });
        }
    }
}
