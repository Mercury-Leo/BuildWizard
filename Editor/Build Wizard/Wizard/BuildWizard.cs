/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

using System.IO;
using Editor.Build_Wizard.Git.Extensions;
using Editor.Build_Wizard.TextFile;
using Editor.Build_Wizard.Version;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor.Build_Wizard.Wizard
{
    public class BuildWizard : OdinEditorWindow
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
        private const string AbsorberName = "Launcher";
        private const string FileName = AbsorberName + ".exe";
        private const string VersionInformationName = "VersionInformation.txt";
        private const string ProjectVersionName = "Project Version";
        private const string EditorBuildLocationKey = "LastBuildLocation";

#pragma warning disable CS0414
        private bool _upgradedMajor;
        private bool _upgradedMinor;
#pragma warning restore CS0414

        public static bool IsBuilding;

        private readonly TextFileWriter _textFile = new();

        private ProjectVersionSO _projectVersion;

        private void Awake()
        {
            LoadProjectVersion();
            SetVersionVisuals();

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

            IsBuilding = true;
            if (releaseBuild)
            {
                var path = GetBuildPath(version, ReleaseFolder);
                var buildOptions = GetBuildOptions(ReleaseOptions, path, scenes);
                BuildPlayer(buildOptions);
                WriteBuildInformation(path, GitExtensions.FullCommitHash, GitExtensions.Branch);
            }

            version = SetVersion();

            if (debugBuild)
            {
                var path = GetBuildPath(version, DebugFolder);
                var buildOptions = GetBuildOptions(DebugOptions, path, scenes);
                BuildPlayer(buildOptions);
                WriteBuildInformation(path, GitExtensions.FullCommitHash, GitExtensions.Branch);
            }

            if (deepProfileBuild)
            {
                var path = GetBuildPath(version, DeepProfileFolder);
                var buildOptions = GetBuildOptions(DeepProfileOptions, path, scenes);
                BuildPlayer(buildOptions);
                WriteBuildInformation(path, GitExtensions.FullCommitHash, GitExtensions.Branch);
            }

            IsBuilding = false;
            Close();
        }

        [Button]
        public void BuildAll()
        {
            releaseBuild = true;
            debugBuild = true;
            deepProfileBuild = true;
            Build();
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
            var window = GetWindow<BuildWizard>();
            window.titleContent = WindowTitle;
            window.Show();
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

        private string GetBuildPath(string version, string folder)
        {
            var mainPath = buildLocation;

            EditorPrefs.SetString(EditorBuildLocationKey, buildLocation);

            var buildFolderPath = Path.Combine(mainPath, folder, version);

            if (!Directory.Exists(buildFolderPath))
            {
                Directory.CreateDirectory(buildFolderPath);
            }

            return Path.Combine(buildFolderPath, FileName);
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

        private void LoadProjectVersion()
        {
            _projectVersion = Resources.Load<ProjectVersionSO>(ProjectVersionName);
        }
    }
}