using System;

namespace Selise.Ecap.SC.PraxisMonitor.Contracts
{
    public static class PraxisTag
    {
        public const string PersonForUser = "Person-For-User";
        public const string ResizeImage = "Resize-Image";
        public const string ResizeImage_1024_1024 = "Resize-Image-1024-1024";
        public const string ResizeImage_960_960 = "Resize-Image-960-960";
        public const string ResizeImage_256_256 = "Resize-Image-256-256";
        public const string ResizeImage_128_128 = "Resize-Image-128-128";
        public const string ResizeImage_64_64 = "Resize-Image-128-128";
        public const string FileIsOriginal = "File-Is-Original";
        public const string FileIsEdited = "File-Is-Edited";
        public const string FileOfProcessGuide = "File-Of-ProcessGuide";
        public const string IsValidPraxisProcessGuide = "Is-Valid-PraxisProcessGuide";
        public const string FileOfOpenItem = "File-Of-OpenItem";
        public const string FileOfEquipment = "File-Of-Equipment";
        public const string FileOfTraining = "File-Of-Training";
        public const string FileOfDeveloper = "File-Of-Developer";
        public const string IsValidConvertedFileMap = "Is-Valid-ConvertedFileMap";
        [Obsolete] public const string IsValidIncident = "Is-Valid-RiqsIncident";
        [Obsolete] public const string IsValidDuplicateIncident = "Is-Valid-Duplicated-RiqsIncident";
        public const string IsValidCirsReport = "Is-Valid-CirsReport";
        public const string IsValidDuplicatedCirsReport = "Is-Valid-Duplicated-CirsReport";
        public const string IsValidPraxisOrganization = "Is-Valid-PraxisOrganization";
        public const string LogoOfOrganization = "Logo-Of-PraxisOrganization";
        public const string IsValidRiqsShift = "Is-Valid-RiqsShift";
        public const string IsValidRiqsShiftPlan = "Is-Valid-RiqsShiftPlan";
        public const string IsValidCockpitObjectArtifactSummary = "Is-Valid-CockpitObjectArtifactSummary";
        public const string IsValidCockpitDocumentActivityMetrics = "Is-Valid-CockpitDocumentActivityMetrics";
        public const string IsValidRiqsQuickTask = "Is-Valid-RiqsQuickTask";
        public const string IsValidRiqsQuickTaskPlan = "Is-Valid-RiqsQuickTaskPlan";
        public const string IsValidRiqsAbsencePlan = "Is-Valid-RiqsAbsencePlan";
    }
}
