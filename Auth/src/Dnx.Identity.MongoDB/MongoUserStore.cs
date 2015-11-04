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
using System.Linq;
using System.Globalization;
using System.Security.Claims;

namespace Dnx.Identity.MongoDB
{
    public class MongoIdentityUser
    {
        private readonly List<MongoUserClaim> _claims;
        private readonly List<MongoUserLogin> _logins;

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

            Id = GenerateId(userName);
            UserName = userName;
            CreatedOn = new Occurrence();

            _claims = new List<MongoUserClaim>();
            _logins = new List<MongoUserLogin>();
        }

        [BsonId]
        public string Id { get; private set; }
        public string UserName { get; private set; }
        public string NormalizedUserName { get; private set; }
        public MongoUserEmail Email { get; private set; }

        public MongoUserPhoneNumber PhoneNumber { get; private set; }
        public string PasswordHash { get; private set; }
        public string SecurityStamp { get; private set; }
        public bool IsTwoFactorEnabled { get; private set; }

        public IEnumerable<MongoUserClaim> Claims
        {
            get
            {
                return _claims;
            }

            private set
            {
                if (value != null)
                {
                    _claims.AddRange(value);
                }
            }
        }

        public IEnumerable<MongoUserLogin> Logins
        {
            get
            {
                return _logins;
            }

            private set
            {
                if (value != null)
                {
                    _logins.AddRange(value);
                }
            }
        }

        public int AccessFailedCount { get; private set; }
        public bool IsLockoutEnabled { get; private set; }
        public FutureOccurrence LockoutEndDate { get; private set; }

        public Occurrence CreatedOn { get; private set; }
        public Occurrence DeletedOn { get; private set; }

        public virtual void EnableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = true;
        }

        public virtual void DisableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = false;
        }

        public virtual void EnableLockout()
        {
            IsLockoutEnabled = true;
        }

        public virtual void DisableLockout()
        {
            IsLockoutEnabled = false;
        }

        public virtual void SetEmail(string email)
        {
            var mongoUserEmail = new MongoUserEmail(email);
            SetEmail(mongoUserEmail);
        }

        public virtual void SetEmail(MongoUserEmail mongoUserEmail)
        {
            Email = mongoUserEmail;
        }

        public virtual void SetNormalizedUserName(string normalizedUserName)
        {
            if(normalizedUserName == null)
            {
                throw new ArgumentNullException(nameof(normalizedUserName));
            }

            NormalizedUserName = normalizedUserName;
        }

        public virtual void SetPhoneNumber(string phoneNumber)
        {
            var mongoUserPhoneNumber = new MongoUserPhoneNumber(phoneNumber);
            SetPhoneNumber(mongoUserPhoneNumber);
        }

        public virtual void SetPhoneNumber(MongoUserPhoneNumber mongoUserPhoneNumber)
        {
            PhoneNumber = mongoUserPhoneNumber;
        }

        public virtual void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public virtual void SetSecurityStamp(string securityStamp)
        {
            SecurityStamp = securityStamp;
        }

        public virtual void IncrementAccessFailedCount()
        {
            AccessFailedCount++;
        }

        public virtual void SetAccessFailedCount(int accessFailedCount)
        {
            AccessFailedCount = accessFailedCount;
        }

        public virtual void ResetAccessFailedCount()
        {
            AccessFailedCount = 0;
        }

        public virtual void LockUntil(DateTime lockoutEndDate)
        {
            LockoutEndDate = new FutureOccurrence(lockoutEndDate);
        }

        public virtual void AddClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            AddClaim(new MongoUserClaim(claim));
        }

        public virtual void AddClaim(MongoUserClaim mongoUserClaim)
        {
            if (mongoUserClaim == null)
            {
                throw new ArgumentNullException(nameof(mongoUserClaim));
            }

            _claims.Add(mongoUserClaim);
        }

        public virtual void RemoveClaim(MongoUserClaim mongoUserClaim)
        {
            if (mongoUserClaim == null)
            {
                throw new ArgumentNullException(nameof(mongoUserClaim));
            }

            _claims.Remove(mongoUserClaim);
        }

        public virtual void AddLogin(MongoUserLogin mongoUserLogin)
        {
            if (mongoUserLogin == null)
            {
                throw new ArgumentNullException(nameof(mongoUserLogin));
            }

            _logins.Add(mongoUserLogin);
        }

        public virtual void RemoveLogin(MongoUserLogin mongoUserLogin)
        {
            if (mongoUserLogin == null)
            {
                throw new ArgumentNullException(nameof(mongoUserLogin));
            }

            _logins.Remove(mongoUserLogin);
        }

        public void Delete()
        {
            if(DeletedOn != null)
            {
                throw new InvalidOperationException($"User '{Id}' has already been deleted.");
            }

            DeletedOn = new Occurrence();
        }

        private static string GenerateId(string userName)
        {
            return userName.ToLower(CultureInfo.InvariantCulture);
        }
    }

    public class MongoUserStore<TUser> : 
        IUserStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserTwoFactorStore<TUser>
        where TUser : MongoIdentityUser
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

        public void Dispose()
        {
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
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.Id, userId),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            return _usersCollection.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            if (normalizedUserName == null)
            {
                throw new ArgumentNullException(nameof(normalizedUserName));
            }

            var query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.NormalizedUserName, normalizedUserName),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            return _usersCollection.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.UserName);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (normalizedName == null)
            {
                throw new ArgumentNullException(nameof(normalizedName));
            }

            user.SetNormalizedUserName(normalizedName);

            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("Changing the username is not supported.");
        }

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.Id, user.Id),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            var replaceResult = await _usersCollection.ReplaceOneAsync(query, user, new UpdateOptions { IsUpsert = false }).ConfigureAwait(false);

            return replaceResult.IsModifiedCountAvailable && replaceResult.ModifiedCount == 1
                ? IdentityResult.Success
                : IdentityResult.Failed();
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            // NOTE: Not the best way to ensure uniquness.
            if (user.Logins.Any(x => x.Equals(login)))
            {
                throw new InvalidOperationException("Login already exists.");
            }

            user.AddLogin(new MongoUserLogin(login));

            return Task.CompletedTask;
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }

            if (providerKey == null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }

            var login = new UserLoginInfo(loginProvider, providerKey, string.Empty);
            var loginToRemove = user.Logins.FirstOrDefault(x => x.Equals(login));

            if (loginToRemove != null)
            {
                user.RemoveLogin(loginToRemove);
            }

            return Task.CompletedTask;
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var logins = user.Logins.Select(login => 
                new UserLoginInfo(login.LoginProvider, login.ProviderKey, login.ProviderDisplayName));

            return Task.FromResult<IList<UserLoginInfo>>(logins.ToList());
        }

        public Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }

            if (providerKey == null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }

            var notDeletedQuery = Builders<TUser>.Filter.Eq(u => u.DeletedOn, null);
            var loginQuery = Builders<TUser>.Filter.ElemMatch(usr => usr.Logins,
                Builders<MongoUserLogin>.Filter.And(
                    Builders<MongoUserLogin>.Filter.Eq(lg => lg.LoginProvider, loginProvider),
                    Builders<MongoUserLogin>.Filter.Eq(lg => lg.ProviderKey, providerKey)
                )
            );

            var query = Builders<TUser>.Filter.And(notDeletedQuery, loginQuery);

            return _usersCollection.Find(query).FirstOrDefaultAsync();
        }

        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var claims = user.Claims.Select(clm => new Claim(clm.ClaimType, clm.ClaimValue)).ToList();

            return Task.FromResult<IList<Claim>>(claims);
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            foreach (var claim in claims)
            {
                user.AddClaim(claim);
            }

            return Task.CompletedTask;
        }

        public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }

            user.RemoveClaim(new MongoUserClaim(claim));
            user.AddClaim(newClaim);

            return Task.CompletedTask;
        }

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            foreach (var claim in claims)
            {
                user.RemoveClaim(new MongoUserClaim(claim));
            }

            return Task.CompletedTask;
        }

        public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            var notDeletedQuery = Builders<TUser>.Filter.Eq(u => u.DeletedOn, null);
            var claimQuery = Builders<TUser>.Filter.ElemMatch(usr => usr.Claims,
                Builders<MongoUserClaim>.Filter.And(
                    Builders<MongoUserClaim>.Filter.Eq(c => c.ClaimType, claim.Type),
                    Builders<MongoUserClaim>.Filter.Eq(c => c.ClaimValue, claim.Value)
                )
            );

            var query = Builders<TUser>.Filter.And(notDeletedQuery, claimQuery);
            var users = await _usersCollection.Find(query).ToListAsync().ConfigureAwait(false);

            return users;
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.SetPasswordHash(passwordHash);

            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash != null);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (stamp == null)
            {
                throw new ArgumentNullException(nameof(stamp));
            }

            user.SetSecurityStamp(stamp);

            return Task.CompletedTask;
        }

        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.SecurityStamp);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (enabled)
            {
                user.EnableTwoFactorAuthentication();
            }
            else
            {
                user.DisableTwoFactorAuthentication();
            }

            return Task.CompletedTask;
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.IsTwoFactorEnabled);
        }
    }
}
