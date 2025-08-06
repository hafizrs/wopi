using Microsoft.Extensions.Logging;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class UserActivityService : IUserActivityService
    {
        private readonly ILogger<UserActivityService> _logger;
        private readonly IKeyStore _keyStore;

        public UserActivityService(
            ILogger<UserActivityService> logger,
            IKeyStore keyStore
        )
        {
            _logger = logger;
            _keyStore = keyStore;
        }

     /*   public async Task<List<object>> GetUserActivity(string userId, string action)
        {
            
            var activitiesJson = await _keyStore.GetValueAsync(userId);

            if (activitiesJson == null || !activitiesJson.Any())
            {
                _logger.LogInformation("No activities found for user ID: {UserId}", userId);
                return new List<object>();
            }

            
            var activities = activitiesJson
                .Select(json => System.Text.Json.JsonSerializer.Deserialize<object>(json))
                .Where(activity => activity != null)
                .ToList();

            var filteredActivities = activities
                .Where(activity => activity.ActionName.Equals(action, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return filteredActivities;
        } */
        
        public async Task SaveUserActivity(string userId)
        {
            var data = new
            {
                UserId = userId,
                ActionName = "CLIENT_CHANGE",
                ActionId = Guid.NewGuid().ToString(),
                ClientId = Guid.NewGuid().ToString()
            };

            var dataJson = System.Text.Json.JsonSerializer.Serialize(data);
            await _keyStore.AddKeyWithExprityAsync(data.ActionId, dataJson, 10000);
        }

        Task<object> IUserActivityService.GetUserActivity(string userId, string action)
        {
            throw new NotImplementedException();
        }
    }

}
