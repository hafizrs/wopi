using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Selise.Ecap.Entities.PrimaryEntities.Security;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Contracts;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.EventHandlers.Users
{
    public class UserCreatedEventHandler : IBaseEventHandler<User>
    {
        private readonly ILogger<UserCreatedEventHandler> _logger;
        private readonly IRepository repository;
        private readonly IUserPersonService userService;

        public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger, IUserPersonService userService, IRepository repository)
        {
            _logger = logger;
            this.repository = repository;
            this.userService = userService;
        }

        public bool Handle(User user)
        {
            _logger.LogInformation("Enter {EventHandlerName} with userId: {UserId}", nameof(UserCreatedEventHandler), user.ItemId);
            if (string.IsNullOrEmpty(user?.ItemId)) return false;
            try
            {
                var userId = user.ItemId;
                var person = userService.GetByUserId(userId);
                var praxisUser = repository.GetItem<PraxisUser>(p => p.ItemId == person.ItemId && !p.IsMarkedToDelete);
                if (praxisUser != null)
                {
                    if ((praxisUser?.ClientList != null || praxisUser.ClientList?.Count() > 0) && !string.IsNullOrEmpty(person.CreatedBy))
                    {
                        var clientIds = praxisUser.ClientList.Select(p => p.ClientId).ToList();
                        if (clientIds.Count == 1)
                        {
                            var praxisClient = repository.GetItem<PraxisClient>(pc => pc.ItemId.Equals(clientIds[0]) && !pc.IsMarkedToDelete);

                            if (praxisClient?.CompanyTypes != null && praxisClient.CompanyTypes.ToList().Exists(c => c.Equals(RoleNames.TechnicalClient)))
                            {
                                _logger.LogInformation("User.Created, Praxis User");
                                userService.RoleAssignToUser(person, praxisUser.ClientList, true);
                            }
                            else
                            {
                                _logger.LogInformation("User.Created, Non Praxis User");
                                userService.RoleAssignToUser(person, praxisUser.ClientList);
                            }

                            userService.AddRowLevelSecurity(person.ItemId, new[] { praxisClient?.ItemId });
                        }
                        else if (clientIds.Count > 1)
                        {
                            _logger.LogInformation("User.Created, Non Praxis User");
                            userService.RoleAssignToUser(person, praxisUser.ClientList);
                            userService.AddRowLevelSecurity(person.ItemId, clientIds.ToArray());
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Organization ID or Person Created ID missing: For UserID {UserId}", user.ItemId);
                    }
                }
                else
                {
                    _logger.LogInformation("No Praxis user data found in {EntityName} entity with user Id: {UserId}.", nameof(PraxisUser), user.ItemId);
                }


                _logger.LogInformation("Handled by {EventHandlerName} with userId: {UserId}", nameof(UserCreatedEventHandler), user.ItemId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception occurred during insert dynamic role while creating new user with ItemId: {UserId} and Email: {Email}. Exception message: {ExceptionMessage}. Exception Details: {ExceptionDetails}.",
                    user.ItemId, user.Email, ex.Message, ex.StackTrace);
                return false;
            }
        }
    }
}