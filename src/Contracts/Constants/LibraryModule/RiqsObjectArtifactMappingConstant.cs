using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public class RiqsObjectArtifactMappingConstant
    {
        public const string ApprovalAdminsKey = "ApprovalAdmins";

        public RiqsObjectArtifactMappingConstant()
        {
        }

        public static RiqsObjectArtifactMapping riqsArtifactMappingData = null;
        public static RiqsObjectArtifactMapping riqsArtifactFolderMappingData = null;

        public static void ResetRiqsArtifactMappingData(RiqsObjectArtifactMapping mappingData)
        {
            if (mappingData == null || mappingData?.ObjectArtifactId == riqsArtifactMappingData?.ObjectArtifactId)
            {
                riqsArtifactMappingData = mappingData;
            }
            if (mappingData == null || mappingData?.ObjectArtifactId == riqsArtifactFolderMappingData?.ObjectArtifactId)
            {
                riqsArtifactFolderMappingData = mappingData;
            }
        }

        public static RiqsObjectArtifactMapping GetRiqsObjectArtifactMappingByArtifactId(string artifactId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            if (string.IsNullOrEmpty(artifactId)) return null;
            if (riqsArtifactMappingData?.ObjectArtifactId == artifactId)
            {
                return riqsArtifactMappingData;
            }
            if (artifactMappingDatas != null)
            {
                riqsArtifactFolderMappingData = artifactMappingDatas?.Find(a => a.ObjectArtifactId == artifactId);
                return riqsArtifactFolderMappingData;
            }
            var _repository = ServiceLocator.GetService<IRepository>();
            riqsArtifactMappingData = _repository.GetItem<RiqsObjectArtifactMapping>(l => l.ObjectArtifactId == artifactId);
            
            return riqsArtifactMappingData;
        }

        public static RiqsObjectArtifactMapping GetRiqsObjectArtifactMappingByParentFolderId(string parentId, List<RiqsObjectArtifactMapping> artifactMappingDatas = null)
        {
            if (string.IsNullOrEmpty(parentId)) return null;
            if (riqsArtifactFolderMappingData?.ObjectArtifactId == parentId)
            {
                return riqsArtifactFolderMappingData;
            }
            if (artifactMappingDatas != null)
            {
                riqsArtifactFolderMappingData = artifactMappingDatas?.Find(a => a.ObjectArtifactId == parentId);
                return riqsArtifactFolderMappingData;
            }
            var _repository = ServiceLocator.GetService<IRepository>();
            riqsArtifactFolderMappingData = _repository.GetItem<RiqsObjectArtifactMapping>(l => l.ObjectArtifactId == parentId);

            return riqsArtifactFolderMappingData;
        }
    }
}
