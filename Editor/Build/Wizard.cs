#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using Profiles;
using ProjectVersion.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utility.Git.Extensions;
using Utility.TextFile;
using ProjectVersion;
using UnityEditor.Build;

namespace Build
{
    public class Wizard : OdinEditorWindow, IPostprocessBuildWithReport
    {
        private static readonly GUIContent WindowTitle = new("Build Wizard");

        [InfoBox("Select the profile you want to build with")] [SerializeField]
        private BuildProfileSO profile;

        [ShowIf("@this.profile == null")]
        [BoxGroup("Build Type")]
        [InfoBox("Builds a clean release exe")]
        [SerializeField]
        private bool releaseBuild;

        [ShowIf("@this.profile == null")] [BoxGroup("Build Type")] [InfoBox("Builds a development exe")]
        public bool debugBuild;

        [ShowIf("@this.profile == null")]
        [BoxGroup("Build Type")]
        [InfoBox("Builds a development exe with Deep profiling enabled")]
        [SerializeField]
        private bool deepProfileBuild;

        [FolderPath(RequireExistingPath = true, AbsolutePath = true)] [SerializeField]
        private string buildLocation = string.Empty;

        [FoldoutGroup("Version", order: 3), ReadOnly] [ShowInInspector]
        private int _major;

        [FoldoutGroup("Version"), ReadOnly] [ShowInInspector]
        private int _minor;

        [FoldoutGroup("Version"), ReadOnly]
        [ShowInInspector]
        [Tooltip(
            "The number of commits since the last Tag. In case there are no tags found, number of commits since the start.")]
        private int _commit;

        private const BuildOptions ReleaseOptions = BuildOptions.CleanBuildCache | BuildOptions.ShowBuiltPlayer;

        private const BuildOptions DebugOptions =
            BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ShowBuiltPlayer |
            BuildOptions.ConnectToHost;

        private const BuildOptions DeepProfileOptions =
            DebugOptions | BuildOptions.EnableDeepProfilingSupport | BuildOptions.DetailedBuildReport;

        private const string ReleaseFolder = "Release";
        private const string DebugFolder = "Debug";
        private const string DeepProfileFolder = "DeepProfile";
        private const string BuildFolder = "Builds";
        private const string VersionInformationName = "VersionInformation.txt";
        private const string EditorBuildLocationKey = "LastBuildLocation";
        private const string EditorProfileKey = "LastUsedProfile";
        private const string Pattern = @"^(.*\/){0,1}(.{1,15})";
        private const string Dash = "-";

        private string _productName = "Product";
        private string _fileName;

#pragma warning disable CS0414
        private bool _upgradedMajor;
        private bool _upgradedMinor;
#pragma warning restore CS0414

        public static bool IsBuilding;

        private readonly TextFileWriter _textFile = new();
        private ProjectVersionSO _projectVersion;
        private BuildTarget _currentBuildTarget = BuildTarget.StandaloneWindows;
        private BuildTargetGroup _currentBuildTargetGroup = BuildTargetGroup.Standalone;
        private int _currentSubTarget;

        private void Awake()
        {
            _productName = Application.productName;
            _projectVersion = ProjectVersionExtensions.FindOrCreateProjectVersion();
            SetVersionVisuals();
            _commit = int.Parse(GitExtensions.GetNumberOfCommits());
            _fileName = _productName + ".exe";
            if (EditorPrefs.HasKey(EditorBuildLocationKey))
            {
                buildLocation = EditorPrefs.GetString(EditorBuildLocationKey);
            }

            if (EditorPrefs.HasKey(EditorProfileKey))
            {
                LoadProfile();
            }
        }

        [Button]
        [DisableIf("@!(this.releaseBuild || this.debugBuild || this.deepProfileBuild || this.profile != null)")]
        public void Build()
        {
            SaveCurrentBuildTarget();
            if (profile != null)
            {
                SaveProfile();
                BuildProfile();
                return;
            }

            if (releaseBuild || debugBuild || deepProfileBuild)
            {
                BuildPresets();
                return;
            }

            Debug.LogWarning("Tried to build without any options selected.", this);
        }

        [HorizontalGroup("Version/Upgrade"), Button, DisableIf("_upgradedMajor")]
        public void UpgradeMajor()
        {
            _projectVersion.UpgradeMajor();
            _upgradedMajor = true;
            SetVersionVisuals();
        }

        [HorizontalGroup("Version/Upgrade"), Button, DisableIf("_upgradedMinor")]
        public void UpgradeMinor()
        {
            _projectVersion.UpgradeMinor();
            _upgradedMinor = true;
            SetVersionVisuals();
        }

        private void BuildPresets()
        {
            CheckBuildLocation();

            _fileName = _productName + ".exe";
            var version = SetVersion(true);
            var scenes = GetBuildScenes();
            var branchName = GetFolderBranchName(GitExtensions.Branch);

            IsBuilding = true;
            if (releaseBuild)
            {
                BuildFlow(version, scenes, branchName, ReleaseFolder, ReleaseOptions);
            }

            version = SetVersion();

            if (debugBuild)
            {
                BuildFlow(version, scenes, branchName, DebugFolder, DebugOptions);
            }

            if (deepProfileBuild)
            {
                EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(),
                    "CopyPDBFiles", "true");
                BuildFlow(version, scenes, branchName, DeepProfileFolder, DeepProfileOptions);
            }

            CleanupWizard();
        }

        private void BuildProfile()
        {
            CheckBuildLocation();
            var scenes = GetBuildScenes();
            var branchName = GetFolderBranchName(GitExtensions.Branch);

            IsBuilding = true;

            foreach (var buildData in profile.BuildTargets)
            {
                 EditorUserBuildSettings.standaloneBuildSubtarget = buildData.isHeadless
                    ? StandaloneBuildSubtarget.Server
                    : StandaloneBuildSubtarget.Player;
            
                if (!(EditorUserBuildSettings.selectedBuildTargetGroup == buildData.TargetGroup &&
                      EditorUserBuildSettings.activeBuildTarget == buildData.Target))
                {
                    if (!EditorUserBuildSettings.SwitchActiveBuildTarget(buildData.TargetGroup, buildData.Target))
                    {
                        Debug.LogError($"Failed to switch build target to {buildData.platformTarget}.", this);
                        continue;
                    }
                }

                if (buildData.overrideExecutableName)
                {
                    if (string.IsNullOrWhiteSpace(buildData.executableName))
                    {
                        Debug.LogWarning("Executable path was invalid, using default exe.", this);
                        _fileName = _productName + ".exe";
                    }
                    else
                    {
                        _fileName = buildData.executableName + ".exe";
                    }
                }

                var version = SetVersion(buildData.isReleaseBuild);
                var folder = profile.name;

                BuildFlow(version, scenes, branchName, folder, buildData.GetBuildOptions(),
                    buildData.name, buildData.isHeadless);
            }

            CleanupWizard();
        }

        private void CleanupWizard()
        {
            IsBuilding = false;
            PlayerSettings.bundleVersion = _projectVersion.CoreVersion;
            RevertToCurrentBuildTarget();
            Close();
        }

        private void SaveCurrentBuildTarget()
        {
            _currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            _currentBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        }

        private void RevertToCurrentBuildTarget()
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = (StandaloneBuildSubtarget)_currentSubTarget;
        
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(_currentBuildTargetGroup, _currentBuildTarget))
            {
                Debug.LogError($"Failed to switch build target to {_currentBuildTarget}.", this);
            }
        }

        [MenuItem("Build/Wizard")]
        private static void ShowBuildWindow()
        {
            var window = GetWindow<Wizard>();
            window.titleContent = WindowTitle;
            window.Show();
        }

        private void BuildFlow(string version, string[] scenes, string branchName, string folder, BuildOptions options,
            string buildName = "", bool isHeadless = false)
        {
            var path = GetBuildPath(version, branchName, folder, buildName);
            var buildOptions = GetBuildOptions(options, path, scenes, isHeadless);
            BuildPlayer(buildOptions);
            WriteBuildInformation(path, GitExtensions.FullCommitHash, GitExtensions.Branch,
                _projectVersion.FullVersion, DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"), buildName);
        }

        private void CheckBuildLocation()
        {
            if (string.IsNullOrWhiteSpace(buildLocation))
            {
                var parentFolder = Path.GetDirectoryName(Application.dataPath);
                if (string.IsNullOrWhiteSpace(parentFolder))
                {
                    return;
                }

                buildLocation = Path.Combine(parentFolder, BuildFolder);
            }
        }

        private string[] GetBuildScenes()
        {
            var buildScenes = EditorBuildSettings.scenes;
            var scenes = new string[buildScenes.Length];
            for (int index = 0; index < buildScenes.Length; index++)
            {
                scenes[index] = EditorBuildSettings.scenes[index].path;
            }

            return scenes;
        }

        private string SetVersion(bool isRelease = false)
        {
            var version = isRelease ? _projectVersion.Version : _projectVersion.FullVersion;

            PlayerSettings.bundleVersion = version;

            return version;
        }

        private string GetBuildPath(string version, string branch, string folder, string buildName = "")
        {
            var mainPath = buildLocation;

            EditorPrefs.SetString(EditorBuildLocationKey, buildLocation);

            var buildFolderName = branch + Dash + version;
            var buildFolderPath = Path.Combine(mainPath, _productName, folder, buildFolderName, buildName);

            if (!Directory.Exists(buildFolderPath))
            {
                Directory.CreateDirectory(buildFolderPath);
            }

            return Path.Combine(buildFolderPath, _fileName);
        }

        private BuildPlayerOptions GetBuildOptions(BuildOptions option, string path, string[] scenes,
            bool isHeadless = false)
        {
            var buildOptions = new BuildPlayerOptions
            {
                options = option, locationPathName = path, target = BuildTarget.StandaloneWindows64, scenes = scenes
            };
            if (isHeadless)
            {
                buildOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
            }

            return buildOptions;
        }

        private void BuildPlayer(BuildPlayerOptions options)
        {
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result is BuildResult.Succeeded)
            {
                Debug.Log("Build was a success");
                return;
            }

            Debug.LogError("Failed build process!");
        }

        private void WriteBuildInformation(string path, params string[] data)
        {
            if (data == null)
            {
                return;
            }

            var contents = string.Empty;
            foreach (var content in data)
            {
                contents += content + "\n";
            }

            var filePath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, VersionInformationName);
            _textFile.WriteToTextFileAt(filePath, contents);
        }

        private void SetVersionVisuals()
        {
            _major = _projectVersion.Major;
            _minor = _projectVersion.Minor;
        }

        private string GetFolderBranchName(string branch)
        {
            var regex = new Regex(Pattern);
            var match = regex.Match(branch);
            return match.Success ? match.Groups[2].Value : string.Empty;
        }

        private void LoadProfile()
        {
            var assetPath = EditorPrefs.GetString(EditorProfileKey);

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<BuildProfileSO>(assetPath);
            if (asset == null)
            {
                return;
            }

            profile = asset;
        }

        private void SaveProfile()
        {
            if (profile == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(profile);
            EditorPrefs.SetString(EditorProfileKey, assetPath);
        }
    }
}
#endif
