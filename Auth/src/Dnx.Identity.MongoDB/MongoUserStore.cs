using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using MongoDB.Driver;
using Microsoft.Framework.Logging;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System.Security.Claims;

namespace Dnx.Identity.MongoDB
{
    public class ConfirmationRecord : Occurance
    {
        public ConfirmationRecord() : base()
        {
        }

        public ConfirmationRecord(DateTime confirmedOn) : base(confirmedOn)
        {
        }
    }

    public class Occurance
    {
        public Occurance() : this(DateTime.UtcNow)
        {
        }

        public Occurance(DateTime occuranceInstanceUtc)
        {
            OccuredOn = occuranceInstanceUtc;
        }

        public DateTime OccuredOn { get; private set; }
    }

    public class MongoUserClaim : IEquatable<MongoUserClaim>, IEquatable<Claim>
    {
        public MongoUserClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            ClaimType = claim.Type;
            ClaimValue = claim.Value;
        }

        public MongoUserClaim(string claimType, string claimValue)
        {
            if (claimType == null)
            {
                throw new ArgumentNullException(nameof(claimType));
            }
            if (claimValue == null)
            {
                throw new ArgumentNullException(nameof(claimValue));
            }

            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        public string ClaimType { get; private set; }
        public string ClaimValue { get; private set; }

        public bool Equals(MongoUserClaim other)
        {
            return other.ClaimType.Equals(ClaimType, StringComparison.InvariantCultureIgnoreCase) 
                && other.ClaimValue.Equals(ClaimValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Equals(Claim other)
        {
            return other.Type.Equals(ClaimType, StringComparison.InvariantCultureIgnoreCase) 
                && other.Value.Equals(ClaimValue, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class MongoUserPhoneNumber : MongoUserContactRecord
    {
        public MongoUserPhoneNumber(string phoneNumber) : base(phoneNumber)
        {
        }
    }

    public abstract class MongoUserContactRecord : IEquatable<MongoUserEmail>
    {
        protected MongoUserContactRecord(string value)
        {
            if(value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Value = value;
        }

        public string Value { get; private set; }
        public ConfirmationRecord ConfirmationRecord { get; private set; }

        public bool IsConfirmed()
        {
            return ConfirmationRecord != null;
        }

        internal void SetConfirmed()
        {
            SetConfirmed(new ConfirmationRecord());
        }

        internal void SetConfirmed(ConfirmationRecord confirmationRecord)
        {
            if (ConfirmationRecord == null)
            {
                ConfirmationRecord = confirmationRecord;
            }
        }

        internal void SetUnconfirmed()
        {
            ConfirmationRecord = null;
        }

        public bool Equals(MongoUserEmail other)
        {
            return other.Value.Equals(Value, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class MongoUserEmail : MongoUserContactRecord
    {
        public MongoUserEmail(string email) : base(email)
        {
        }
    }

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
            CreatedOn = new Occurance();
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
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

        public Occurance CreatedOn { get; set; }
        public Occurance DeletedOn { get; private set; }

        public void Delete()
        {
            if(DeletedOn != null)
            {
                throw new InvalidOperationException($"User '{Id}' has already been deleted.");
            }

            DeletedOn = new Occurance();
        }
    }

    public class MongoUserLogin : IEquatable<MongoUserLogin>, IEquatable<UserLoginInfo>
    {
        public MongoUserLogin(UserLoginInfo loginInfo)
        {
            if (loginInfo == null)
            {
                throw new ArgumentNullException(nameof(loginInfo));
            }

            LoginProvider = loginInfo.LoginProvider;
            ProviderKey = loginInfo.ProviderKey;
            ProviderDisplayName = loginInfo.ProviderDisplayName;
        }

        public string LoginProvider { get; private set; }
        public string ProviderKey { get; private set; }
        public string ProviderDisplayName { get; private set; }

        public bool Equals(MongoUserLogin other)
        {
            return other.LoginProvider.Equals(LoginProvider, StringComparison.InvariantCultureIgnoreCase) 
                && other.ProviderKey.Equals(ProviderKey, StringComparison.InvariantCulture);
        }

        public bool Equals(UserLoginInfo other)
        {
            return other.LoginProvider.Equals(LoginProvider, StringComparison.InvariantCultureIgnoreCase) 
                && other.ProviderKey.Equals(ProviderKey, StringComparison.InvariantCulture);
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
