using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Driver;
using Microsoft.Framework.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Dnx.Identity.MongoDB.Models;

namespace Dnx.Identity.MongoDB
{
    public class MongoIdentityUser
    {
        public MongoIdentityUser(string userName, string email)
            : this(userName)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            Email = new MongoUserEmail(email);
        }

        public MongoIdentityUser(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Id = ObjectId.GenerateNewId().ToString();
            UserName = userName;
            CreatedOn = new Occurrence();
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; private set; }
        public string UserName { get; set; }
        public string NormalizedUserName { get; set; }
        public MongoUserEmail Email { get; set; }

        public MongoUserPhoneNumber PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public bool IsLockoutEnabled { get; set; }
        public bool IsTwoFactorEnabled { get; set; }

        public IList<MongoUserClaim> Claims { get; set; }
        public IList<MongoUserLogin> Logins { get; set; }

        public int AccessFailedCount { get; set; }
        public DateTime? LockoutEndDate { get; set; }

        public Occurrence CreatedOn { get; set; }
        public Occurrence DeletedOn { get; private set; }

        public void Delete()
        {
            if(DeletedOn != null)
            {
                throw new InvalidOperationException($"User '{Id}' has already been deleted.");
            }

            DeletedOn = new Occurrence();
        }
    }

    public class MongoUserStore<TUser> : IUserStore<TUser> where TUser : MongoIdentityUser
    {
        private readonly IMongoCollection<TUser> _usersCollection;
        private readonly ILogger _logger;

        public MongoUserStore(IMongoCollection<TUser> usersCollection, ILoggerFactory loggerFactory)
        {
            if(usersCollection == null)
            {
                throw new ArgumentNullException(nameof(usersCollection));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _usersCollection = usersCollection;
            _logger = loggerFactory.CreateLogger(GetType().Name);
        }

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            await _usersCollection.InsertOneAsync(user, cancellationToken).ConfigureAwait(false);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.Delete();

            var query = Builders<TUser>.Filter.Eq(u => u.Id, user.Id);
            var update = Builders<TUser>.Update.Set(u => u.DeletedOn, user.DeletedOn);

            await _usersCollection.UpdateOneAsync(query, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            return IdentityResult.Success;
        }

        public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
