using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.TwoFactorAuthentication;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Queries;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.TwoFactorAuthentication
{
    public class AnonymousUserTwoFactorAuthenticationService : ITwoFactorAuthenticationService
    {
        private readonly IKeyStore _keyStore;
        private readonly ILogger<AnonymousUserTwoFactorAuthenticationService> _logger;
        private readonly IEmailNotifierService _emailNotifierService;
        private const string Invalid_Two_Factor_Code = "invalid_two_factor_code";
        private readonly ITokenService _tokenService;
        private readonly ISecurityContextProvider _securityContextProvider;
        public AnonymousUserTwoFactorAuthenticationService(
            IKeyStore keyStore,
            ILogger<AnonymousUserTwoFactorAuthenticationService> logger,
            IEmailNotifierService emailNotifierService,
            ITokenService tokenService,
            ISecurityContextProvider securityContextProvider
        )
        {
            _keyStore = keyStore;
            _logger = logger;
            _emailNotifierService = emailNotifierService;
            _tokenService = tokenService;
            _securityContextProvider = securityContextProvider;
        }


        private static string GenerateRandomAccessCode()
        {
            return new Random().Next(11111, 99999).ToString();
        }
        public async Task<string> GenerateCode(string twoFactorId, string email, string name)
        {
            try
            {
                var twoFactorAuthenticationInfo = new TwoFactorAuthenticationInfo
                {
                    Email = email,
                    UserId = null,
                    TwoFactorId = twoFactorId,
                    TwoFactorCode = GenerateRandomAccessCode()
                };
                var twoFactorAuthenticationInfoJson = twoFactorAuthenticationInfo.Serilize();
                await _keyStore.AddKeyWithExprityAsync(twoFactorId, twoFactorAuthenticationInfoJson, 10000);
                await Send2FactorCodeThroughEmail(email, name, twoFactorAuthenticationInfo.TwoFactorCode);
                return twoFactorId;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occurred in {Name} in GenerateCode with error -> {ExMessage} trace -> {ExStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }

            return string.Empty;
        }

        public Task<string> GenerateCode(string twoFactorId)
        {
            throw new NotImplementedException();
        }

        private async Task<bool> Send2FactorCodeThroughEmail(string email, string name, string twoFactorCode)
        {

            try
            {

                var dataContext = new Dictionary<string, string>
                {
                    { "DisplayName" ,name},
                    { "OwnerName" ,""},
                    { "TwoFactorCode" , twoFactorCode},

                };
                var response = await _emailNotifierService.SendAnonymousUser2faEmail(email, dataContext);

                if (response)
                {
                    _logger.LogInformation("Multifactor code sent to: {Email} by email", email.ToLower());
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send multifactor code to: {Email} by email", email.ToLower());
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                   "Fail to sent multifactor code to {Email} in VerifyCode with error -> {ExMessage} trace -> {ExStackTrace}",
                  email.ToLower(), ex.Message, ex.StackTrace);
                return false;
            }
        }

        public async Task<TwoFAVerifyResponse> VerifyCode(TwoFactorCodeVerifyQuery query)
        {
            try
            {
                var twoFactorIdExists = await _keyStore.KeyExistsAsync(query.TwoFactorId);
                if (twoFactorIdExists == false)
                {
                    return TwoFAVerifyResponse.CreateError(Invalid_Two_Factor_Code);
                }

                var twoFactorAuthenticationInfoJsonString = await _keyStore.GetValueAsync(query.TwoFactorId);

                var twoFactorAuthenticationInfo = TwoFactorAuthenticationInfo.Deserialize(twoFactorAuthenticationInfoJsonString);

                var twoFactorAuthenticationCodeMatched = twoFactorAuthenticationInfo.TwoFactorCode.Equals(query.TwoFactorCode);

                if (twoFactorAuthenticationCodeMatched == false)
                {
                    return TwoFAVerifyResponse.CreateError(Invalid_Two_Factor_Code);
                }
                else
                {
                    await _keyStore.RemoveKeyAsync(twoFactorAuthenticationInfo.TwoFactorId);
                    return TwoFAVerifyResponse.CreateSuccess(query.TwoFactorId);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception occured in {Name} in VerifyCode with error -> {ExMessage} trace -> {ExStackTrace}",
                    GetType().Name, ex.Message, ex.StackTrace);
            }

            return null;
        }
    }
}
