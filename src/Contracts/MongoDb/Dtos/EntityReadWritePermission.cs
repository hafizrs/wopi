using System;
using System.Collections.Generic;

namespace Selise.Ecap.SC.Wopi.Contracts.MongoDb.Dtos
{
    public class EntityReadWritePermission
    {
        public Guid Id { get; set; }

        public List<string> RolesAllowedToRead { get; set; }

        public List<string> RolesAllowedToReadForRemove { get; set; }

        public List<string> IdsAllowedToRead { get; set; }

        public List<string> IdsAllowedToReadForRemove { get; set; }

        public List<string> RolesAllowedToUpdate { get; set; }

        public List<string> RolesAllowedToUpdateForRemove { get; set; }

        public List<string> IdsAllowedToUpdate { get; set; }

        public List<string> IdsAllowedToUpdateForRemove { get; set; }

        public List<string> RolesAllowedToDelete { get; set; }

        public List<string> RolesAllowedToDeleteForRemove { get; set; }

        public List<string> IdsAllowedToDelete { get; set; }

        public List<string> IdsAllowedToDeleteForRemove { get; set; }

        public EntityReadWritePermission()
        {
            RolesAllowedToRead = new List<string>();
            RolesAllowedToReadForRemove = new List<string>();
            IdsAllowedToRead = new List<string>();
            IdsAllowedToReadForRemove = new List<string>();
            RolesAllowedToUpdate = new List<string>();
            RolesAllowedToUpdateForRemove = new List<string>();
            IdsAllowedToUpdate = new List<string>();
            IdsAllowedToUpdateForRemove = new List<string>();
            RolesAllowedToDelete = new List<string>();
            RolesAllowedToDeleteForRemove = new List<string>();
            IdsAllowedToDelete = new List<string>();
            IdsAllowedToDeleteForRemove = new List<string>();
        }
    }
}