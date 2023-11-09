namespace Wizard
{
    public static class WizardConventions
    {
        public const string BuildFolder = "Builds";
        public const string VersionInformationName = "VersionInformation.txt";
        public const string Pattern = @"^(.*\/){0,1}(.{1,15})";
        public const string Dash = "-";
        public const string ProfileFolderPath = "Assets" + "/" + ProfileFolder;
        public const string ProfileFolder = "BuildProfiles";
        public const string DateTimeFormat = "dd/MM/yyyy hh:mm tt";

        public const string WizardPreferencesName = "Build Wizard";

        public const string EditorBuildLocationKey = "LastBuildLocation";
        public const string EditorProfileKey = "LastUsedProfile";
        public const string EditorBuildTargetKey = nameof(EditorBuildTargetKey);
        public const string EditorBuildTargetGroupKey = nameof(EditorBuildTargetGroupKey);
        public const string EditorBuildSubtargetKey = nameof(EditorBuildSubtargetKey);
        public const string EditorIsBuildingKey = nameof(EditorIsBuildingKey);
    }
}
