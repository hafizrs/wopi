using Newtonsoft.Json;
using RabbitMQ.Client.Impl;
using Selise.Ecap.Entities.PrimaryEntities.DWT;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.RiqsInterfaces;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.RiqsInterface
{
    public class RiqsInterfaceSessionService : IRiqsInterfaceSessionService
    {
      
        private readonly IKeyStore _keyStore;
        private readonly IRepository _repository;
        public RiqsInterfaceSessionService(
            IKeyStore keyStore, IRepository repository)
        {
            _keyStore = keyStore;
            _repository = repository;
        }


        public async Task AddRefreshTokenSessionAsync(ExternalUserTokenResponse response, string userId)
        {
            var session = GetRefreshTokenSessionAsyncByUserId(userId);
            if (session != null)
            {
                await _keyStore.RemoveKeyAsync(session.RefreshTokenSessionId);
                await _repository.DeleteAsync<RiqsInterfaceSession>(x => x.ItemId == session.ItemId);
            }
            var sessionObj = new RiqsInterfaceSession()
            {
                ItemId = response.refresh_token_id,
                UserId = userId,
                RefreshTokenSessionId = response.refresh_token_id,
                Provider= response.provider,
            };
            var twoFactorAuthenticationInfoJson = JsonConvert.SerializeObject(response);
            await _keyStore.AddKeyWithExprityAsync(sessionObj.RefreshTokenSessionId, twoFactorAuthenticationInfoJson, 10000);
            await _repository.SaveAsync(sessionObj);
        }

        public async Task DeleteRefreshTokenSessionAsync(string userId)
        {
            var session = GetRefreshTokenSessionAsyncByUserId(userId);
            if (session != null)
            {
              await  _repository.DeleteAsync<RiqsInterfaceSession>(x => x.ItemId == session.ItemId);
            }

        }

        public async Task<string> GetRefreshTokenIdAsync(string userId)
        {
            var session = await _repository.GetItemAsync<RiqsInterfaceSession>(x => x.UserId == userId);
            
            if (session!=null)
            {
                return session.RefreshTokenSessionId;
            }
            return string.Empty;
        }

        public Task GetRefreshTokenSessionAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<string> GetRefreshTokenSessionAsync(string userId)
        {
            var session = _repository.GetItem<RiqsInterfaceSession>(x => x.UserId == userId);
            var twoFactorIdExists = await _keyStore.KeyExistsAsync(session.RefreshTokenSessionId);
            if (twoFactorIdExists)
            {
                var twoFactorAuthenticationInfoJsonString = await _keyStore.GetValueAsync(session.RefreshTokenSessionId);

                var twoFactorAuthenticationInfo = JsonConvert.DeserializeObject<ExternalUserTokenResponse>(twoFactorAuthenticationInfoJsonString);

                return twoFactorAuthenticationInfo.refresh_token;
            }
            return string.Empty;

        }

        public async Task<ExternalUserTokenResponse> GetRefreshTokenSessionByRefTokenIdAsync(string refreshtokenId)
        {
            var refreshtokenExists = await _keyStore.KeyExistsAsync(refreshtokenId);
            if (refreshtokenExists)
            {
                var refreshtokenInfoJsonString = await _keyStore.GetValueAsync(refreshtokenId);

                var refreshtokenInfo = JsonConvert.DeserializeObject<ExternalUserTokenResponse>(refreshtokenInfoJsonString);

                return refreshtokenInfo;
            }
            return null;
        }

        private RiqsInterfaceSession GetRefreshTokenSessionAsyncByUserId(string userId)
        {
            var configuration = _repository.GetItem<RiqsInterfaceSession>(x => x.UserId == userId);
            return configuration;
        }
    }
}
