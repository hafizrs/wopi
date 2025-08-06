using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Equipment;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor.Shared;
using Selise.Ecap.Entities.PrimaryEntities.SmartCity.PraxisMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.EntityResponse
{
    public record ProjectedUserResponse(
        string UserId,
        PraxisImage Image,
        string Salutation,
        string FirstName,
        string LastName,
        string DisplayName,
        string Gender,
        DateTime DateOfBirth,
        string Nationality,
        string MotherTongue,
        IEnumerable<string> OtherLanguage,
        string Designation,
        string Email,
        string Phone,
        string AcademicTitle,
        string WorkLoad,
        string KuNumber,
        int NumberOfChildren,
        IEnumerable<string> Roles,
        IEnumerable<string> Skills,
        IEnumerable<PraxisSpeciality> Specialities,
        PraxisCertificate CertificateOfCompetence,
        DateTime DateOfJoining,
        int NumberOfPatient,
        string Telephone,
        string GlnNumber,
        string ZsrNumber,
        string KNumber,
        string Remarks,
        string PhoneExtensionNumber,
        bool Active,
        IEnumerable<PraxisClientInfo> ClientList,
        bool IsEmailVerified,
        bool ShowIntroductionTutorial,
        IEnumerable<PraxisUserAdditionalInfo> AdditionalInfo,
        string ClientId,
        string ClientName,
        string CreatedBy,
        DateTime CreateDate,
        string ItemId,
        DateTime LastUpdateDate
    );

}
