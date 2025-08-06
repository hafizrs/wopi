using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Commands;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Notifier;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class RiqsPediaViewControlService : IRiqsPediaViewControlService
    {
        private readonly IRepository _repository;
        private readonly ILogger<RiqsPediaViewControlService> _logger;
        private readonly ISecurityContextProvider _securityContextProvider;
        private readonly ISecurityHelperService _securityHelperService;
        private readonly INotificationService _notificationProviderService;
        private readonly IMongoClientRepository _mongoClientRepository;

        public RiqsPediaViewControlService(
            IRepository repository,
            ILogger<RiqsPediaViewControlService> logger,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService,
            INotificationService notificationProviderService,
            IMongoClientRepository mongoClientRepository
        )
        {
            _repository = repository;
            _logger = logger;
            _securityContextProvider = securityContextProvider;
            _securityHelperService = securityHelperService;
            _notificationProviderService = notificationProviderService;
            _mongoClientRepository = mongoClientRepository;
        }

        public async Task UpsertRiqsPediaViewControl(UpsertRiqsPediaViewControlCommand command)
        {
            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var praxisUser = await _repository.GetItemAsync<PraxisUser>(u => u.UserId == securityContext.UserId);

                var existingData = await _repository.GetItemAsync<RiqsPediaViewControl>(x => x.PraxisUserId == praxisUser.ItemId && x.UserId == securityContext.UserId);

                if (existingData == null)
                {
                    var newRiqsPediaViewControl = new RiqsPediaViewControl();
                    newRiqsPediaViewControl.ItemId = Guid.NewGuid().ToString();
                    newRiqsPediaViewControl.CreateDate = DateTime.Now;
                    newRiqsPediaViewControl.CreatedBy = securityContext.UserId;
                    newRiqsPediaViewControl.PraxisUserId = praxisUser.ItemId;
                    newRiqsPediaViewControl.UserId = securityContext.UserId;
                    newRiqsPediaViewControl.ViewState = command.ViewState;
                    await _repository.SaveAsync(newRiqsPediaViewControl);
                }
                else
                {
                    existingData.LastUpdateDate = DateTime.Now;
                    existingData.LastUpdatedBy = securityContext.UserId;
                    existingData.ViewState = command.ViewState;
                    await _repository.UpdateAsync<RiqsPediaViewControl>(u => u.ItemId.Equals(existingData.ItemId), existingData);
                }

                await SendViewControlNotification(command, true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in {Name} in CreateRiqsPediaViewControl with error -> {ExMessage} trace -> {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }
        }

        private async Task SendViewControlNotification(UpsertRiqsPediaViewControlCommand command, bool response, string denormalizePyload = null)
        {
            var result = new
            {
                NotifiySubscriptionId = command.NotificationSubscriptionId,
                Success = response,
                command.NotificationSubscriptionId
            };

            await _notificationProviderService.PaymentNotification(
                    response,
                    command.NotificationSubscriptionId,
                    result,
                    command.Context,
                    command.ActionName,
                    denormalizePyload);
        }

        public async Task<RiqsPediaViewControlResponse> GetRiqsPediaViewControl()
        {
            var response = new RiqsPediaViewControlResponse();

            try
            {
                var securityContext = _securityContextProvider.GetSecurityContext();
                var praxisUser = await _repository.GetItemAsync<PraxisUser>(u => u.UserId == securityContext.UserId);

                var existingData = await _repository.GetItemAsync<RiqsPediaViewControl>(x => x.PraxisUserId == praxisUser.ItemId && x.UserId == securityContext.UserId);
                bool hasApprovalOrUploadAdmin = await IsUserApprovalOrUploadAdmin(securityContext.UserId);

                if (existingData != null)
                {
                    response.PraxisUserId = existingData.PraxisUserId;
                    response.UserId = existingData.UserId;
                    response.ViewState = existingData.ViewState;
                    response.IsShowViewState = hasApprovalOrUploadAdmin && _securityHelperService.IsADepartmentLevelUser();
                    response.IsAdminViewEnabled = response.ViewState && response.IsShowViewState;
                    return response;
                }

                if (!_securityHelperService.IsADepartmentLevelUser()) return response;

                var command = new UpsertRiqsPediaViewControlCommand { ViewState = false };
                await UpsertRiqsPediaViewControl(command);

                response.PraxisUserId = praxisUser.ItemId;
                response.UserId = securityContext.UserId;
                response.ViewState = false;
                response.IsShowViewState = hasApprovalOrUploadAdmin;
                response.IsAdminViewEnabled = false;

            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred in {Name} in GetRiqsPediaViewControl: {ExMessage}, StackTrace: {ExStackTrace}", GetType().Name, ex.Message, ex.StackTrace);
            }

            return response;
        }

        private async Task<bool> IsUserApprovalOrUploadAdmin(string userId)
        {
            return await _repository.ExistsAsync<RiqsLibraryControlMechanism>(x => !string.IsNullOrEmpty(x.OrganizationId) &&
                ((x.ApprovalAdmins != null && x.ApprovalAdmins.Any(y => y.UserId == userId)) ||
                (x.UploadAdmins != null && x.UploadAdmins.Any(y => y.UserId == userId)))
            );
        }
    }
}
