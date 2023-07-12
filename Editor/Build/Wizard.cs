/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using ProjectVersion.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utility.Git.Extensions;
using Utility.TextFile;
using ProjectVersion;

namespace Build
{
    public class Wizard : OdinEditorWindow
    {
        private static readonly GUIContent WindowTitle = new("Build Wizard");

        [BoxGroup("Build Type")] [InfoBox("Builds a clean release exe")] [SerializeField]
        private bool releaseBuild;

        [BoxGroup("Build Type")] [InfoBox("Builds a development exe")]
        public bool debugBuild;

        [BoxGroup("Build Type")] [InfoBox("Builds a development exe with Deep profiling enabled")] [SerializeField]
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

        private void Awake()
        {
            _productName = Application.productName;
            _fileName = _productName + ".exe";
            _projectVersion = ProjectVersionExtensions.FindOrCreateProjectVersion();
            SetVersionVisuals();
            _commit = int.Parse(GitExtensions.GetNumberOfCommits());

            if (EditorPrefs.HasKey(EditorBuildLocationKey))
            {
                buildLocation = EditorPrefs.GetString(EditorBuildLocationKey);
            }
        }

        [Button]
        [DisableIf("@!(this.releaseBuild || this.debugBuild || this.deepProfileBuild)")]
        public void Build()
        {
            if (!(releaseBuild || debugBuild || deepProfileBuild))
            {
                return;
            }

            CheckBuildLocation();

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

            IsBuilding = false;
            PlayerSettings.bundleVersion = _projectVersion.CoreVersion;
            Close();
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

        [MenuItem("Build/Wizard")]
        private static void ShowBuildWindow()
        {
            var window = GetWindow<Wizard>();
            window.titleContent = WindowTitle;
            window.Show();
        }

        private void BuildFlow(string version, string[] scenes, string branchName, string folder, BuildOptions options)
        {
            var path = GetBuildPath(version, branchName, folder);
            var buildOptions = GetBuildOptions(options, path, scenes);
            BuildPlayer(buildOptions);
            WriteBuildInformation(path, GitExtensions.FullCommitHash, GitExtensions.Branch,
                _projectVersion.FullVersion, DateTime.Now.ToString("dd/MM/yyyy hh:mm tt"));
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

        private string GetBuildPath(string version, string branch, string folder)
        {
            var mainPath = buildLocation;

            EditorPrefs.SetString(EditorBuildLocationKey, buildLocation);

            var buildFolderName = branch + Dash + version;
            var buildFolderPath = Path.Combine(mainPath, _productName, folder, buildFolderName);

            if (!Directory.Exists(buildFolderPath))
            {
                Directory.CreateDirectory(buildFolderPath);
            }

            return Path.Combine(buildFolderPath, _fileName);
        }

        private BuildPlayerOptions GetBuildOptions(BuildOptions option, string path, string[] scenes)
        {
            return new BuildPlayerOptions
            {
                options = option, locationPathName = path, target = BuildTarget.StandaloneWindows64, scenes = scenes
            };
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
    }
}
#endif
