using MongoDB.Driver;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Risk;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Constants;
using Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb;
using Selise.Ecap.SC.PraxisMonitor.Contracts.MongoDb.Dtos;
using SeliseBlocks.Genesis.Framework.Infrastructure;
using System;
using System.Collections.Generic;



namespace Selise.Ecap.SC.PraxisMonitor.Domain.DomainServices.Services
{
    public class PraxisAssessmentService : IPraxisAssessmentService
    {
        private readonly IMongoSecurityService mongoSecurityService;
        private readonly IRepository repository;
        private readonly IMongoClientRepository mongoClientRepository;

        public PraxisAssessmentService(IMongoSecurityService mongoSecurityService,
            IMongoClientRepository mongoClientRepository, IRepository repository)
        {
            this.mongoSecurityService = mongoSecurityService;
            this.mongoClientRepository = mongoClientRepository;
            this.repository = repository;
        }
        public void AddRowLevelSecurity(string itemId, string clientId)
        {
            var clientAdminAccessRole = mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientAdmin, clientId);
            var clientReadAccessRole = mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientRead, clientId);
            var clientManagerAccessRole = mongoSecurityService.GetRoleName(DynamicRolePrefix.PraxisClientManager, clientId);

            var permission = new EntityReadWritePermission
            {
                Id = Guid.Parse(itemId)
            };

            permission.RolesAllowedToRead.Add(clientAdminAccessRole);
            permission.RolesAllowedToRead.Add(clientReadAccessRole);
            permission.RolesAllowedToRead.Add(clientManagerAccessRole);

            permission.RolesAllowedToUpdate.Add(clientManagerAccessRole);
            permission.RolesAllowedToUpdate.Add(clientAdminAccessRole);

            mongoSecurityService.UpdateEntityReadWritePermission<PraxisAssessment>(permission);
        }

        public List<PraxisAssessment> GetAllPraxisAssessment()
        {
            throw new NotImplementedException();
        }

        public PraxisAssessment GetPraxisAssessment(string itemId)
        {
            return repository.GetItem<PraxisAssessment>(assesment => assesment.ItemId.Equals(itemId) && !assesment.IsMarkedToDelete);
        }

        public PraxisAssessment GetRecentPraxisAssessment(string riskItemId)
        {
            var assesment = mongoClientRepository.GetCollection<PraxisAssessment>()
                .Find(s => s.RiskId.Equals(riskItemId) && !s.IsMarkedToDelete)
                .SortByDescending(s => s.CreateDate)
                .FirstOrDefault();

            return assesment;
        }

        public void UpdateRecentAssessment(string riskId)
        {
            PraxisRisk risk = repository.GetItem<PraxisRisk>(r => r.ItemId.Equals(riskId) && !r.IsMarkedToDelete);
            PraxisAssessment recentAssessment = GetRecentPraxisAssessment(riskId);

            if (risk != null && recentAssessment != null)
            {
                risk.RecentAssessment = recentAssessment;
                risk.IsResolved = false;
                repository.Update(r => r.ItemId.Equals(risk.ItemId), risk);
            }
        }

        public void RemoveRowLevelSecurity(string clientId)
        {
            throw new NotImplementedException();
        }
    }
}
