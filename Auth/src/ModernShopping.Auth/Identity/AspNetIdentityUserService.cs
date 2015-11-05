using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Extensions;
using IdentityServer3.Core;
using IdentityModel;
using IdentityServer3.Core.Services.Default;
using Dnx.Identity.MongoDB;

namespace ModernShopping.Auth.Identity
{
    /// <remarks>
    /// Taken from https://github.com/IdentityServer/IdentityServer3.Samples/blob/cf3ba612ead847d868dc64d4e39857b70f2b35ca/source/AspNetIdentity/WebHost/App_Packages/IdentityServer3.AspNetIdentity/IdentityServer3.AspNetIdentity.cs
    /// </remarks>
    public class AspNetIdentityUserService<TUser> : UserServiceBase
        where TUser : class
    {
        protected readonly Microsoft.AspNet.Identity.UserManager<TUser> _userManager;

        public AspNetIdentityUserService(Microsoft.AspNet.Identity.UserManager<TUser> userManager, Func<string> parseSubject = null)
        {
            if (userManager == null) throw new ArgumentNullException("userManager");

            _userManager = userManager;
            EnableSecurityStamp = true;
        }

        public string DisplayNameClaimType { get; set; }
        public bool EnableSecurityStamp { get; }

        public override async Task GetProfileDataAsync(ProfileDataRequestContext ctx)
        {
            var subject = ctx.Subject;
            var requestedClaimTypes = ctx.RequestedClaimTypes;

            if (subject == null) throw new ArgumentNullException("subject");

            string key = subject.GetSubjectId();
            var acct = await _userManager.FindByIdAsync(key);
            if (acct == null)
            {
                throw new ArgumentException("Invalid subject identifier");
            }

            var claims = await GetClaimsFromAccount(acct);
            if (requestedClaimTypes != null && requestedClaimTypes.Any())
            {
                claims = claims.Where(x => requestedClaimTypes.Contains(x.Type));
            }

            ctx.IssuedClaims = claims;
        }

        protected virtual async Task<IEnumerable<Claim>> GetClaimsFromAccount(TUser user)
        {
            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
            var userName = await _userManager.GetUserNameAsync(user).ConfigureAwait(false);

            var claims = new List<Claim>{
                new Claim(Constants.ClaimTypes.Subject, userId),
                new Claim(Constants.ClaimTypes.PreferredUserName, userName),
            };

            if (_userManager.SupportsUserEmail)
            {
                var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(email))
                {
                    claims.Add(new Claim(Constants.ClaimTypes.Email, email));
                    var verified = await _userManager.IsEmailConfirmedAsync(user).ConfigureAwait(false);
                    claims.Add(new Claim(Constants.ClaimTypes.EmailVerified, verified ? "true" : "false"));
                }
            }

            if (_userManager.SupportsUserPhoneNumber)
            {
                var phone = await _userManager.GetPhoneNumberAsync(user).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(phone))
                {
                    claims.Add(new Claim(Constants.ClaimTypes.PhoneNumber, phone));
                    var verified = await _userManager.IsPhoneNumberConfirmedAsync(user);
                    claims.Add(new Claim(Constants.ClaimTypes.PhoneNumberVerified, verified ? "true" : "false"));
                }
            }

            if (_userManager.SupportsUserClaim)
            {
                claims.AddRange(await _userManager.GetClaimsAsync(user).ConfigureAwait(false));
            }

            if (_userManager.SupportsUserRole)
            {
                var roleClaims =
                    from role in await _userManager.GetRolesAsync(user).ConfigureAwait(false)
                    select new Claim(Constants.ClaimTypes.Role, role);

                claims.AddRange(roleClaims);
            }

            return claims;
        }

        protected virtual async Task<string> GetDisplayNameForAccountAsync(string userID)
        {
            string displayName;
            Claim nameClaim = null;
            var user = await _userManager.FindByIdAsync(userID);
            var claims = await GetClaimsFromAccount(user);

            if (DisplayNameClaimType != null)
            {
                nameClaim = claims.FirstOrDefault(x => x.Type == DisplayNameClaimType);
            }

            if (nameClaim == null)
            {
                nameClaim = claims.FirstOrDefault(x => x.Type == Constants.ClaimTypes.Name);
            }

            if (nameClaim == null)
            {
                nameClaim = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name);
            }

            if (nameClaim != null)
            {
                displayName = nameClaim.Value;
            }
            else
            {
                displayName = await _userManager.GetUserNameAsync(user).ConfigureAwait(false);
            }

            return displayName;
        }

        protected async virtual Task<TUser> FindUserAsync(string username)
        {
            return await _userManager.FindByNameAsync(username);
        }

        protected virtual Task<AuthenticateResult> PostAuthenticateLocalAsync(TUser user, SignInMessage message)
        {
            return Task.FromResult<AuthenticateResult>(null);
        }

        public override async Task AuthenticateLocalAsync(LocalAuthenticationContext ctx)
        {
            var username = ctx.UserName;
            var password = ctx.Password;
            var message = ctx.SignInMessage;

            ctx.AuthenticateResult = null;

            if (_userManager.SupportsUserPassword)
            {
                var user = await FindUserAsync(username);
                if (user != null)
                {
                    if (_userManager.SupportsUserLockout &&
                        await _userManager.IsLockedOutAsync(user).ConfigureAwait(false))
                    {
                        return;
                    }

                    if (await _userManager.CheckPasswordAsync(user, password))
                    {
                        if (_userManager.SupportsUserLockout)
                        {
                            await _userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
                        }

                        var result = await PostAuthenticateLocalAsync(user, message);
                        if (result == null)
                        {
                            var claims = await GetClaimsForAuthenticateResult(user);
                            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
                            result = new AuthenticateResult(userId, await GetDisplayNameForAccountAsync(userId), claims);
                        }

                        ctx.AuthenticateResult = result;
                    }
                    else if (_userManager.SupportsUserLockout)
                    {
                        await _userManager.AccessFailedAsync(user).ConfigureAwait(false);
                    }
                }
            }
        }

        protected virtual async Task<IEnumerable<Claim>> GetClaimsForAuthenticateResult(TUser user)
        {
            List<Claim> claims = new List<Claim>();
            if (EnableSecurityStamp && _userManager.SupportsUserSecurityStamp)
            {
                var stamp = await _userManager.GetSecurityStampAsync(user).ConfigureAwait(false);
                if (!String.IsNullOrWhiteSpace(stamp))
                {
                    claims.Add(new Claim("security_stamp", stamp));
                }
            }
            return claims;
        }

        public override async Task AuthenticateExternalAsync(ExternalAuthenticationContext ctx)
        {
            var externalUser = ctx.ExternalIdentity;
            var message = ctx.SignInMessage;

            if (externalUser == null)
            {
                throw new ArgumentNullException("externalUser");
            }

            var user = await _userManager.FindByLoginAsync(externalUser.Provider, externalUser.ProviderId).ConfigureAwait(false);
            if (user == null)
            {
                ctx.AuthenticateResult = await ProcessNewExternalAccountAsync(externalUser.Provider, externalUser.ProviderId, externalUser.Claims);
            }
            else
            {
                ctx.AuthenticateResult = await ProcessExistingExternalAccountAsync(user, externalUser.Provider, externalUser.ProviderId, externalUser.Claims);
            }
        }

        protected virtual async Task<AuthenticateResult> ProcessNewExternalAccountAsync(string provider, string providerId, IEnumerable<Claim> claims)
        {
            var user = await TryGetExistingUserFromExternalProviderClaimsAsync(provider, claims);
            if (user == null)
            {
                user = await InstantiateNewUserFromExternalProviderAsync(provider, providerId, claims);
                if (user == null)
                    throw new InvalidOperationException("CreateNewAccountFromExternalProvider returned null");

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new AuthenticateResult($"{createResult.Errors.First().Code}-{createResult.Errors.First().Description}");
                }
            }

            var externalLogin = new Microsoft.AspNet.Identity.UserLoginInfo(provider, providerId, null);
            var addExternalResult = await _userManager.AddLoginAsync(user, externalLogin).ConfigureAwait(false);
            if (!addExternalResult.Succeeded)
            {
                return new AuthenticateResult($"{addExternalResult.Errors.First().Code}-{addExternalResult.Errors.First().Description}");
            }

            var result = await AccountCreatedFromExternalProviderAsync(user, provider, providerId, claims);
            if (result != null) return result;

            return await SignInFromExternalProviderAsync(user, provider);
        }

        // TODO!!! GIANT HACK!
        protected virtual Task<TUser> InstantiateNewUserFromExternalProviderAsync(string provider, string providerId, IEnumerable<Claim> claims)
        {
            var user = new MongoIdentityUser(Guid.NewGuid().ToString("N"));
            return Task.FromResult(user as TUser);
        }

        protected virtual Task<TUser> TryGetExistingUserFromExternalProviderClaimsAsync(string provider, IEnumerable<Claim> claims)
        {
            return Task.FromResult<TUser>(null);
        }

        protected virtual async Task<AuthenticateResult> AccountCreatedFromExternalProviderAsync(TUser user, string provider, string providerId, IEnumerable<Claim> claims)
        {
            claims = await SetAccountEmailAsync(user, claims);
            claims = await SetAccountPhoneAsync(user, claims);

            return await UpdateAccountFromExternalClaimsAsync(user, provider, providerId, claims);
        }

        protected virtual async Task<AuthenticateResult> SignInFromExternalProviderAsync(TUser user, string provider)
        {
            var userId = await _userManager.GetUserIdAsync(user).ConfigureAwait(false);
            var claims = await GetClaimsForAuthenticateResult(user);

            return new AuthenticateResult(
                userId.ToString(),
                await GetDisplayNameForAccountAsync(userId),
                claims,
                authenticationMethod: Constants.AuthenticationMethods.External,
                identityProvider: provider);
        }

        protected virtual async Task<AuthenticateResult> UpdateAccountFromExternalClaimsAsync(TUser user, string provider, string providerId, IEnumerable<Claim> claims)
        {
            var existingClaims = await _userManager.GetClaimsAsync(user).ConfigureAwait(false);
            var intersection = existingClaims.Intersect(claims, new ClaimComparer());
            var newClaims = claims.Except(intersection, new ClaimComparer());

            foreach (var claim in newClaims)
            {
                var result = await _userManager.AddClaimAsync(user, claim).ConfigureAwait(false);
                if (!result.Succeeded)
                {
                    return new AuthenticateResult($"{result.Errors.First().Code}-{result.Errors.First().Description}");
                }
            }

            return null;
        }

        protected virtual async Task<AuthenticateResult> ProcessExistingExternalAccountAsync(TUser user, string provider, string providerId, IEnumerable<Claim> claims)
        {
            return await SignInFromExternalProviderAsync(user, provider);
        }

        protected virtual async Task<IEnumerable<Claim>> SetAccountEmailAsync(TUser user, IEnumerable<Claim> claims)
        {
            var email = claims.FirstOrDefault(x => x.Type == Constants.ClaimTypes.Email);
            if (email != null)
            {
                var userEmail = await _userManager.GetEmailAsync(user);
                if (userEmail == null)
                {
                    // if this fails, then presumably the email is already associated with another account
                    // so ignore the error and let the claim pass thru
                    var result = await _userManager.SetEmailAsync(user, email.Value);
                    if (result.Succeeded)
                    {
                        var email_verified = claims.FirstOrDefault(x => x.Type == Constants.ClaimTypes.EmailVerified);
                        if (email_verified != null && email_verified.Value == "true")
                        {
                            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            await _userManager.ConfirmEmailAsync(user, token);
                        }

                        var emailClaims = new string[] { Constants.ClaimTypes.Email, Constants.ClaimTypes.EmailVerified };
                        return claims.Where(x => !emailClaims.Contains(x.Type));
                    }
                }
            }

            return claims;
        }

        protected virtual async Task<IEnumerable<Claim>> SetAccountPhoneAsync(TUser user, IEnumerable<Claim> claims)
        {
            var phone = claims.FirstOrDefault(x => x.Type == Constants.ClaimTypes.PhoneNumber);
            if (phone != null)
            {
                var userPhone = await _userManager.GetPhoneNumberAsync(user);
                if (userPhone == null)
                {
                    // if this fails, then presumably the phone is already associated with another account
                    // so ignore the error and let the claim pass thru
                    var result = await _userManager.SetPhoneNumberAsync(user, phone.Value);
                    if (result.Succeeded)
                    {
                        var phone_verified = claims.FirstOrDefault(x => x.Type == Constants.ClaimTypes.PhoneNumberVerified);
                        if (phone_verified != null && phone_verified.Value == "true")
                        {
                            var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phone.Value);
                            await _userManager.ChangePhoneNumberAsync(user, phone.Value, token);
                        }

                        var phoneClaims = new string[] { Constants.ClaimTypes.PhoneNumber, Constants.ClaimTypes.PhoneNumberVerified };
                        return claims.Where(x => !phoneClaims.Contains(x.Type));
                    }
                }
            }

            return claims;
        }

        public override async Task IsActiveAsync(IsActiveContext ctx)
        {
            var subject = ctx.Subject;

            if (subject == null) throw new ArgumentNullException("subject");

            var id = subject.GetSubjectId();
            var acct = await _userManager.FindByIdAsync(id).ConfigureAwait(false);

            ctx.IsActive = false;

            if (acct != null)
            {
                if (EnableSecurityStamp && _userManager.SupportsUserSecurityStamp)
                {
                    var security_stamp = subject.Claims.Where(x => x.Type == "security_stamp").Select(x => x.Value).SingleOrDefault();
                    if (security_stamp != null)
                    {
                        var db_security_stamp = await _userManager.GetSecurityStampAsync(acct).ConfigureAwait(false);
                        if (db_security_stamp != security_stamp)
                        {
                            return;
                        }
                    }
                }

                ctx.IsActive = true;
            }
        }
    }
}
