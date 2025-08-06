using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts.Models
{
    public class PraxisUserForReport
    {
        public string DisplayName { get; set; }
        public string ClientName { get; set; }
        public List<string> Roles { get; set; }

        public string Designation { get; set; }
        public DateTime DateOfJoining { get; set; }
        public string PhoneExtensionNumber { get; set; }
        public bool IsCreateProcessGuideEnabled { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public bool Active { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string MotherTongue { get; set; }
        public IEnumerable<string> OtherLanguage { get; set; }
        public string AcademicTitle { get; set; }
        public string WorkLoad { get; set; }
        public int NumberOfChildren { get; set; }
        public string Telephone { get; set; }
        public string GlnNumber { get; set; }
        public string ZsrNumber { get; set; }
        public string KNumber { get; set; }

        public PraxisUserForReport(
            string displayName,
            string designation, List<string> roles, DateTime dateOfJoining, string phoneExtensionNumber, bool isCreateProcessGuideEnabled,
            string email, string phone, bool active, string gender, DateTime dateOfBirth, string nationality, string motherTongue, IEnumerable<string> otherLanguage,
            string academicTitle, string workLoad, int numberOfChildren, string telePhone,
            string glnNumber, string zsrNumber, string kNumber)
        {
            DisplayName = displayName;
            ClientName = null;
            Designation = designation;
            Roles = roles;
            DateOfJoining = dateOfJoining;
            PhoneExtensionNumber = phoneExtensionNumber;
            IsCreateProcessGuideEnabled = isCreateProcessGuideEnabled;
            Email = email;
            Phone = phone;
            Active = active;
            Gender = gender;
            DateOfBirth = dateOfBirth;
            Nationality = nationality;
            MotherTongue = motherTongue;
            OtherLanguage = otherLanguage;
            AcademicTitle = academicTitle;
            WorkLoad = workLoad;
            NumberOfChildren = numberOfChildren;
            Telephone = telePhone;
            GlnNumber = glnNumber;
            ZsrNumber = zsrNumber;
            KNumber = kNumber;
        }

        public PraxisUserForReport(
            string displayName,
            string clientName,
            string designation, List<string> roles, DateTime dateOfJoining, string phoneExtensionNumber, bool isCreateProcessGuideEnabled,
            string email, string phone, bool active, string gender, DateTime dateOfBirth, string nationality, string motherTongue, IEnumerable<string> otherLanguage,
            string academicTitle, string workLoad, int numberOfChildren, string telePhone,
            string glnNumber, string zsrNumber, string kNumber)
        {
            DisplayName = displayName;
            ClientName = clientName;
            Designation = designation;
            Roles = roles;
            DateOfJoining = dateOfJoining;
            PhoneExtensionNumber = phoneExtensionNumber;
            IsCreateProcessGuideEnabled = isCreateProcessGuideEnabled;
            Email = email;
            Phone = phone;
            Active = active;
            Gender = gender;
            DateOfBirth = dateOfBirth;
            Nationality = nationality;
            MotherTongue = motherTongue;
            OtherLanguage = otherLanguage;
            AcademicTitle = academicTitle;
            WorkLoad = workLoad;
            NumberOfChildren = numberOfChildren;
            Telephone = telePhone;
            GlnNumber = glnNumber;
            ZsrNumber = zsrNumber;
            KNumber = kNumber;
        }
    }
}