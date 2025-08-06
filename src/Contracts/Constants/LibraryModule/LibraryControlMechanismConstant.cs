using SeliseBlocks.Genesis.Framework.Infrastructure;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Entities.LibraryModule;
using Selise.Ecap.SC.PraxisMonitor.Contracts.Infrastructure;
using System.Collections.Generic;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.DomainServices.LibraryModule
{
    public class LibraryControlMechanismConstant
    {
        public LibraryControlMechanismConstant()
        {
        }
        public const string Standard = "STANDARD";
        public const string FourEyePrinciple = "FOUR_EYE_PRINCIPLE";
        public const string SixEyePrinciple = "SIX_EYE_PRINCIPLE";
        public static RiqsLibraryControlMechanism LibraryControlMechanismData = null;

        public static void ResetLibraryControlMechanism(RiqsLibraryControlMechanism controlMechanism)
        {
            if (controlMechanism == null || controlMechanism?.OrganizationId == LibraryControlMechanismData?.OrganizationId)
            {
                LibraryControlMechanismData = controlMechanism;
            }
        }

        public static RiqsLibraryControlMechanism GetLibraryControlMechanismDataByOrgId(string orgId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            if (string.IsNullOrEmpty(orgId)) return null;
            if (LibraryControlMechanismData != null && LibraryControlMechanismData.OrganizationId == orgId)
            {
                return LibraryControlMechanismData;
            }
            if (controlMechanismDatas != null)
            {
                LibraryControlMechanismData = controlMechanismDatas.Find(c => c.OrganizationId == orgId);
                return LibraryControlMechanismData;
            }

            var _repository = ServiceLocator.GetService<IRepository>();
            LibraryControlMechanismData = _repository.GetItem<RiqsLibraryControlMechanism>(l => l.OrganizationId == orgId);

            return LibraryControlMechanismData;
        }

        public static RiqsLibraryControlMechanism GetLibraryControlMechanismDataByDeptId(string deptId, List<RiqsLibraryControlMechanism> controlMechanismDatas = null)
        {
            if (string.IsNullOrEmpty(deptId)) return null;
            RiqsLibraryControlMechanism libraryControlMechanism;
            if (controlMechanismDatas != null)
            {
                libraryControlMechanism = controlMechanismDatas.Find(c => c.DepartmentId == deptId);
                return libraryControlMechanism;
            }

            var _repository = ServiceLocator.GetService<IRepository>();
            libraryControlMechanism = _repository.GetItem<RiqsLibraryControlMechanism>(l => l.DepartmentId == deptId);

            return libraryControlMechanism;
        }
    }
}
