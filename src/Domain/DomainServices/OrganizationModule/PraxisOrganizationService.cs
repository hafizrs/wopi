using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;

using System;
using System.Linq;
using System.Threading.Tasks;
using Selise.Ecap.Entities.PrimaryEntities.PlatformDataService;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using SeliseBlocks.Genesis.Framework.PDS.Entity;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisOrganizationService : IPraxisOrganizationService
    {
        private readonly ILogger<PraxisOrganizationService> _logger;
        private readonly IRepository _repository;
        private readonly IChangeLogService _changeLogService;

        public PraxisOrganizationService(
            ILogger<PraxisOrganizationService> logger,
            IRepository repository,
            IChangeLogService changeLogService)
        {
            _logger = logger;
            _repository = repository;
            _changeLogService = changeLogService;
        }

        public async Task<bool> UpdateOrganizationAdminIds(string orgId, string userEmail, string userStatus)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(PraxisOrganizationService));
            if (string.IsNullOrWhiteSpace(orgId) || string.IsNullOrWhiteSpace(userEmail) || (userStatus != "Created" && userStatus != "Removed"))
            {
                return false;
            }
            try
            {
                var adminBIds = await PrepareAdminIds(orgId, userEmail, userStatus);
                var updateData = new Dictionary<string, object>
                    {
                        {"AdminBIds",  adminBIds}
                    };

                var filterBuilder = Builders<BsonDocument>.Filter;
                var updateFilters = filterBuilder.Eq("_id", orgId);

                return await _changeLogService.UpdateChange("PraxisOrganization", updateFilters, updateData);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception in the service {ServiceName}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    nameof(PraxisOrganizationService), ex.Message, ex.StackTrace);
            }
            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(PraxisOrganizationService));
            return false;
        }

        public async Task UpdateAdminDeputyAdminId(string orgId, string praxisUserItemId, string designation)
        {
            _logger.LogInformation("Entered into the service {ServiceName}.", nameof(PraxisOrganizationService));

            try
            {
                var updateData = designation == "organizationAdmin" ?
                    new Dictionary<string, object>
                    {
                        {"AdminUserId",  praxisUserItemId}
                    } :
                    new Dictionary<string, object>
                    {
                        {"DeputyAdminUserId",  praxisUserItemId}
                    };

                await _repository.UpdateAsync<PraxisOrganization>(x => x.ItemId == orgId, updateData);

                _logger.LogInformation(
                    "Updated {DesignationType}: {PraxisUserItemId} in {EntityName}  ",
                    designation == "organizationAdmin" ? nameof(PraxisOrganization.AdminUserId) : nameof(PraxisOrganization.DeputyAdminUserId),
                    praxisUserItemId,
                    nameof(PraxisOrganization)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Exception in the service {ServiceName}. Exception Message: {Message}. Exception details: {StackTrace}.",
                    nameof(PraxisOrganizationService), ex.Message, ex.StackTrace);
            }

            _logger.LogInformation("Handled by the service {ServiceName}.", nameof(PraxisOrganizationService));
        }

        public async Task UpdateOrganizationLogoThumbnails(Connection fileConnection, List<PraxisImageThumbnail> thumbnails)
        {
            var praxisOrganization = _repository
                                .GetItems<PraxisOrganization>(pc => pc.ItemId.Equals(fileConnection.ParentEntityID) && !pc.IsMarkedToDelete).FirstOrDefault();

            if (praxisOrganization != null && !string.IsNullOrEmpty(praxisOrganization.ItemId) && praxisOrganization.Logo != null)
            {
                praxisOrganization.Logo.Thumbnails = thumbnails;
                await _repository.UpdateAsync<PraxisOrganization>(pu => pu.ItemId.Equals(praxisOrganization.ItemId), praxisOrganization);

                _logger.LogInformation("Updated Thumbnails of PraxisClient -> {ItemId}", praxisOrganization.ItemId);
            }
        }

        private async Task<List<PraxisIdDto>> PrepareAdminIds(string orgId, string userEmail, string userStatus)
        {
            List<PraxisIdDto> adminBIds = await GetOrganizationAdminIds(orgId);

            var praxisUser = await GetPraxisUserId(userEmail);
            var adminIdDto = new PraxisIdDto
            {
                PraxisUserId = praxisUser?.ItemId,
                UserId = await GetUserId(userEmail),
                PersonId = await GetPersonId(userEmail)
            };

            if (praxisUser.Roles.Contains(RoleNames.GroupAdmin))
            {
                userStatus = "removed";
            }

            if (userStatus == "Created" && !adminBIds.Any( ids => ids.UserId == adminIdDto.UserId && ids.PersonId == adminIdDto.PersonId ))
            {
                adminBIds.Add(adminIdDto);
            }
            else if (userStatus == "Removed")
            {
                adminBIds.Remove( 
                    adminBIds.SingleOrDefault( ids => ids.UserId == adminIdDto.UserId && ids.PersonId == adminIdDto.PersonId ) );
            }

            return adminBIds;
        }

        private async Task<List<PraxisIdDto>> GetOrganizationAdminIds(string orgId)
        {
            var organization = await _repository.GetItemAsync<PraxisOrganization>(o => o.ItemId == orgId);
            return (organization?.AdminBIds) ?? new List<PraxisIdDto>();
        }

        private async Task<string> GetUserId(string email)
        {
            var user = await _repository.GetItemAsync<User>(u => u.Email == email);
            return user?.ItemId;
        }

        private async Task<string> GetPersonId(string email)
        {
            var person = await _repository.GetItemAsync<Person>(u => u.Email == email);
            return person?.ItemId;
        }
        private async Task<PraxisUser> GetPraxisUserId(string email)
        {
            var praxisUser = await _repository.GetItemAsync<PraxisUser>(pu => pu.Email == email);
            return praxisUser;
        }
    }
}
