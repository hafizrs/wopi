using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.ProcessGuide;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Selise.Ecap.SC.PraxisMonitor.Domain.Infrastructure;

namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public static class ProcessGuidePermissionHelper
    {
        public static ProcessGuideDetailsResponse PrepareProcessGuidePermissionResponse(
            ISecurityHelperService securityHelperService,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            string praxisClientId,
            ProcessGuideDetailsResponse response,
            PraxisProcessGuide processGuide,
            PraxisForm form,
            List<PraxisUser> praxisUsers,
            int timezoneOffsetInMinutes = 0)
        {
            if (securityHelperService.IsADepartmentLevelUser() && praxisClientId != null)
            {
                var loggedInClientId = securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                praxisClientId = loggedInClientId == praxisClientId ? praxisClientId : null;
            }

            var loggedInUserId = securityContextProvider.GetSecurityContext().UserId;
            var isDueDateCrossed = IsDueDateCrossed(processGuide, timezoneOffsetInMinutes);
            var processGuideCreatorRoles = repository.GetItem<PraxisUser>(x => x.UserId == processGuide.CreatedBy)?.Roles?.ToList() ?? new List<string>();
            var loggedInPraxisUser = repository.GetItem<PraxisUser>(x => x.UserId == loggedInUserId);
            var canEditGuideData = CanEditGuideData(loggedInPraxisUser?.ItemId, praxisClientId, processGuide, securityHelperService);

            response.Permissions = new Dictionary<string, bool>
            {
                { "CanEditTemplate", CanEdit(processGuideCreatorRoles, securityHelperService) },
                { "CanDeleteTemplate", CanDelete(loggedInUserId, processGuide, processGuideCreatorRoles, securityHelperService) },
                { "CanEditCaseId1", processGuide.CompletionStatus!=100 && CanEditCaseId(isDueDateCrossed, canEditGuideData, processGuideCreatorRoles, securityHelperService) },
                { "CanEditCaseId2", processGuide.CompletionStatus!=100 && CanEditCaseId(isDueDateCrossed, canEditGuideData, processGuideCreatorRoles, securityHelperService) },
                { "CanEditIdDate", processGuide.CompletionStatus!=100 && CanEditCaseId(isDueDateCrossed, canEditGuideData, processGuideCreatorRoles, securityHelperService) },
                { "CanDownloadReport", CanDownloadReport(securityHelperService) },
                { "IsDueDateCrossed", isDueDateCrossed }
            };

            response.PraxisForm = PreparePraxisFormWithPermission(
                canEditGuideData,loggedInPraxisUser?.ItemId, isDueDateCrossed, response, processGuide, form, praxisClientId, repository, securityContextProvider, securityHelperService);

            return response;
        }

        private static bool IsCreatedByPowerUser(List<string> roles, ISecurityHelperService securityHelperService)
        {
            return roles.Contains(RoleNames.PowerUser) && securityHelperService.IsAPowerUser();
        }

        private static bool IsCreatedByDeptLevelUser(List<string> roles, ISecurityHelperService securityHelperService)
        {
            var hideRoles = new string[] { RoleNames.Admin, RoleNames.AdminB, RoleNames.GroupAdmin };
            return !hideRoles.Any(h => roles.Contains(h)) && securityHelperService.IsAPowerUser();
        }

        private static bool CanDelete(string loggedInUserId, PraxisProcessGuide processGuide, List<string> processGuideCreatorRoles, ISecurityHelperService securityHelperService)
        {
            return securityHelperService.IsAAdminBUser() || securityHelperService.IsAAdmin() || IsCreatedByDeptLevelUser(processGuideCreatorRoles, securityHelperService);
        }

        private static bool CanEdit(List<string> processGuideCreatorRoles, ISecurityHelperService securityHelperService)
        {
            return securityHelperService.IsAAdminBUser() || securityHelperService.IsAAdmin() || IsCreatedByPowerUser(processGuideCreatorRoles, securityHelperService);
        }

        private static bool CanEditCaseId( bool isDueDateCrossed, bool canEditGuideData, List<string> processGuideCreatorRoles, ISecurityHelperService securityHelperService)
        {
            return  !isDueDateCrossed && (canEditGuideData || CanEdit(processGuideCreatorRoles, securityHelperService));
        }

        private static bool CanDownloadReport(ISecurityHelperService securityHelperService)
        {
            return securityHelperService.IsAAdminBUser() || securityHelperService.IsAAdmin() || securityHelperService.IsAPowerUser();
        }

        private static bool IsDueDateCrossed(PraxisProcessGuide processGuide, int timezoneOffsetInMinutes)
        {
            if (processGuide?.Shifts != null && processGuide.Shifts.Any() && processGuide?.TaskSchedule?.TaskDateTime != null)
            {
                DateTime taskUtc = processGuide.TaskSchedule.TaskDateTime;
                DateTime taskLocal = taskUtc.AddMinutes(timezoneOffsetInMinutes);
                DateTime dueDate = taskLocal.Date.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                DateTime currentDate = DateTime.UtcNow.AddMinutes(timezoneOffsetInMinutes);

                return currentDate > dueDate;
            }
            return false;
        }

        private static bool CanEditGuideData(string loggedInUserId, string praxisClientId, PraxisProcessGuide processGuide, ISecurityHelperService securityHelperService)
        {
            var client = processGuide.Clients?.FirstOrDefault(c => c.ClientId == praxisClientId);
            if (client?.HasSpecificControlledMembers == true)
            {
                return client.ControlledMembers?.Contains(loggedInUserId) == true;
            }

            return securityHelperService.IsAAdminBUser() || (securityHelperService.IsADepartmentLevelUser() && securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser() == praxisClientId);
        }


        
        private static PraxisForm PreparePraxisFormWithPermission(
            bool canEditProcessGuideData,
            string loggedInPraxisUserId,
            bool isDueDateCrossed,
            ProcessGuideDetailsResponse response,
            PraxisProcessGuide processGuide,
            PraxisForm form,
            string praxisClientId,
            IRepository repository,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService)
        {
            var attachedDocuments = new List<PraxisDocument>();
            var guideQuestionAnswerPermissions = new List<GuideQuestionAnswerPermission>();
            var processGuideCheckList = new List<ClientSpecificCheckList>();
       
            foreach (var pgClient in processGuide.Clients)
            {
                var clientGroupTask = new ClientSpecificCheckList();
                var taskList = new List<ProcessGuideTask>();
                var praxisClient = repository.GetItem<PraxisClient>(x => x.ItemId == praxisClientId);
                clientGroupTask.ClientInfos= new List<FormSpecificClientInfo>
                    {
                        new FormSpecificClientInfo
                        {
                            ClientId = pgClient.ClientId,
                            ClientName = pgClient.ClientName
                        }
                   }.AsEnumerable();
        
               // Filter items and add them to the HashSet to ensure uniqueness
               var itemsToAdd = form?.ProcessGuideCheckList
                    ?.Where(item => item.OrganizationIds?.Contains(praxisClient?.ParentOrganizationId) == true || item.ClientInfos?.Any(client => client.ClientId == pgClient.ClientId) == true)?
                    .ToList();


                foreach (var item in itemsToAdd)
                {
                    
                    var questions = item.ProcessGuideTask?.ToList();
                    taskList.AddRange(questions);
                    var userCompletionList = response.ProcessGuideAnswers
                                .FirstOrDefault(x => x.ProcessGuideId == response.ProcessGuide.ItemId && x.SubmittedBy == loggedInPraxisUserId && x.ClientId == pgClient.ClientId)?
                                .Answers?.Where(a => a.MetaDataList?.Any(m => m.Key == "IsDraft" && m.MetaData?.Value == "1") != true)?.ToList();

                    var questionPermissions = questions.Select(x => PrepareQuestionAnswerPermission(
                        canEditProcessGuideData,
                        loggedInPraxisUserId,
                        pgClient.ClientId,
                        isDueDateCrossed,
                        x,
                        pgClient.HasSpecificControlledMembers,
                        pgClient.ControlledMembers?.ToList() ?? new List<string>(),
                        userCompletionList,
                        securityContextProvider,
                        securityHelperService
                    )).ToList();

                    var documents = item.ProcessGuideTask.SelectMany(t => t.Files).ToList();

                    attachedDocuments.AddRange(documents);
                    guideQuestionAnswerPermissions.AddRange(questionPermissions);
                  
                }
                clientGroupTask.ProcessGuideTask = taskList.AsEnumerable();
                processGuideCheckList.Add(clientGroupTask);

            }

            response.AttachedDocuments = attachedDocuments.Distinct().ToList();
            response.QuestionAnswerPermissions = guideQuestionAnswerPermissions;
            form.ProcessGuideCheckList = processGuideCheckList.ToList();
            form.IdsAllowedToUpdate = null;

            return form;
        }


     private static void ResetPermission(PraxisForm form)
        {
            form.IdsAllowedToUpdate = null;
            form.RolesAllowedToDelete = null;
            form.RolesAllowedToRead = null;
            form.RolesAllowedToWrite = null;
            form.IdsAllowedToDelete = null;
            form.IdsAllowedToRead = null;
        }

        private static bool canAnswerByDeptFilter(string guideClientId, ISecurityHelperService securityHelperService)
        {
            if (securityHelperService.IsAAdmin()) return false;
            if (securityHelperService.IsAAdminBUser()) return true;
            if (securityHelperService.IsADepartmentLevelUser())
            {
                var loggedInClientId = securityHelperService.ExtractDepartmentIdFromDepartmentLevelUser();
                return loggedInClientId == guideClientId;
            }
            return false;
        }



        private static bool CanAnswerQuestion(string praxisClientId, bool isDueDateCrossed, bool hasSpecificControlledMembers, List<string> controlledMembers, string loggedInPraxisUserId, ISecurityHelperService securityHelperService)
        {
            if (isDueDateCrossed || securityHelperService.IsAAdmin()) return false;


            if (hasSpecificControlledMembers && controlledMembers.Contains(loggedInPraxisUserId) && canAnswerByDeptFilter(praxisClientId, securityHelperService)) return true;
            if (!hasSpecificControlledMembers && canAnswerByDeptFilter(praxisClientId, securityHelperService)) return true;
            return false;

        }


        private static GuideQuestionAnswerPermission PrepareQuestionAnswerPermission(
            bool showPermission,
            string loggedInPraxisUserId,
            string praxisClientId,
            bool isDueDateCrossed,
            ProcessGuideTask question,
            bool hasSpecificControlledMembers,
            List<string> controlledMembers,
            List<PraxisProcessGuideSingleAnswer> userCompletionList,
            ISecurityContextProvider securityContextProvider,
            ISecurityHelperService securityHelperService)
        {
            var canAnswer = CanAnswerQuestion(praxisClientId, isDueDateCrossed, hasSpecificControlledMembers, controlledMembers, loggedInPraxisUserId, securityHelperService);
            var isAnswerCompleted = userCompletionList?.Find(x=>x.QuestionId== question.ProcessGuideTaskId);

            return new GuideQuestionAnswerPermission
            {
                PraxisClientId = praxisClientId,
                QuestionId = question.ProcessGuideTaskId,
                Permission = new Dictionary<string, bool>
                {
                    { "CanAnswerQuestion", showPermission && canAnswer },
                    { "IsAnswerCompleted", isAnswerCompleted!=null }
                }
            };
        }
    }
}
