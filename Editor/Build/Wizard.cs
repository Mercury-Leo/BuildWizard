#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Profiles;
using ProjectVersion.Extensions;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Utility.Git.Extensions;
using Utility.TextFile;
using ProjectVersion;
using Wizard;
using static Wizard.WizardConventions;
using static Build.WizardExtensions;

namespace Build
{
    public class Wizard : OdinEditorWindow
    {
        private static readonly GUIContent WindowTitle = new("Build Wizard");

        [InfoBox("Select the profile you want to build with")]
        [InlineButton(nameof(CreateProfile), SdfIconType.PersonBadge)]
        [Tooltip("Build Profile")]
        [SerializeField]
        private BuildProfileSO profile;

        [ShowIf("@profile == null")] [InlineButton(nameof(CreateBuildData), SdfIconType.File)] [SerializeField]
        private List<BuildProfileDataSO> defaultBuildData;

        [FolderPath(RequireExistingPath = true, AbsolutePath = true)] [SerializeField]
        private string buildLocation = string.Empty;

        [FoldoutGroup("Version", order: 3), ReadOnly] [ShowInInspector]
        private int _major;

        [FoldoutGroup("Version"), ReadOnly] [ShowInInspector]
        private int _minor;

        [FoldoutGroup("Version"), ReadOnly]
        [ShowInInspector]
        [Tooltip(
            "The number of commits since the last Tag. In case there are no tags, number of commits since the start.")]
        private int _commit;

        // https://gist.github.com/imurashka/7e743f35c953881857c9aee8db04d117
        /*[ShowInInspector, BoxGroup("" ,order: 4)]
        private void OpenPreferences()
        {
            EditorPrefs.SetString("PreferencesWindowSelectedSection", WizardPreferencesName);
            EditorApplication.ExecuteMenuItem("Edit/Preferences");
        }*/

        private string _productName = "Product";
        private string _fileName = "Product";

#pragma warning disable CS0414
        private bool _upgradedMajor;
        private bool _upgradedMinor;
#pragma warning restore CS0414

        public static bool IsBuilding
        {
            get
            {
                if (EditorPrefs.HasKey(EditorIsBuildingKey))
                {
                    return EditorPrefs.GetBool(EditorIsBuildingKey);
                }

                return false;
            }
        }

        private readonly TextFileWriter _textFile = new();
        private ProjectVersionSO _projectVersion;
        private BuildTarget _currentBuildTarget = BuildTarget.StandaloneWindows;
        private BuildTargetGroup _currentBuildTargetGroup = BuildTargetGroup.Standalone;
        private int _currentSubTarget;

        public int callbackOrder => 0;

        [MenuItem("Build/Wizard")]
        private static void ShowBuildWindow()
        {
            var window = GetWindow<Wizard>();
            window.titleContent = WindowTitle;
            window.Show();
        }

        [ShowIf("@(this.defaultBuildData.Count > 0 && this.profile == null)")]
        [Button("Build Default")]
        public void BuildDefault()
        {
            BuildAllData(defaultBuildData, "Default");
        }

        [ShowIf("@(this.profile != null && this.profile.BuildTargets.Count > 0)")]
        [Button("Build Profile")]
        public void BuildProfile()
        {
            SaveCurrentBuildTarget();
            if (profile == null)
            {
                Debug.LogError("Tried to build with a null profile!", this);
                return;
            }

            SaveProfile();
            BuildAllData(profile.BuildTargets, profile.name);
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

        private void Awake()
        {
            SetProjectInformation();

            if (EditorPrefs.HasKey(EditorBuildLocationKey))
            {
                buildLocation = EditorPrefs.GetString(EditorBuildLocationKey);
            }

            if (EditorPrefs.HasKey(EditorProfileKey))
            {
                LoadProfile();
            }

            defaultBuildData = new List<BuildProfileDataSO> { CreateInstance<BuildProfileDataSO>() };
        }

        private void SetProjectInformation()
        {
            _productName = Application.productName;
            _projectVersion = ProjectVersionExtensions.FindOrCreateProjectVersion();
            SetVersionVisuals();
            _commit = int.Parse(GitExtensions.GetNumberOfCommits());
        }


        private void BuildAllData(IEnumerable<BuildProfileDataSO> builds, string folder)
        {
            CheckBuildLocation();
            var scenes = GetBuildScenes();
            var branchName = GetFolderBranchName(GitExtensions.GetBranchName());

            EditorPrefs.SetBool(EditorIsBuildingKey, true);

            foreach (var data in builds)
            {
                if (data == null)
                {
                    continue;
                }

                BuildData(data, scenes, branchName, folder);
            }

            if (IsBuilding)
            {
                RevertToCurrentBuildTarget();
                EditorPrefs.SetBool(EditorIsBuildingKey, false);
            }

            CleanupWizard();
        }

        private void BuildData(BuildProfileDataSO data, string[] scenes, string branchName, string folder)
        {
            if (data == null)
            {
                return;
            }

            SwitchBuildTargets(data);

            _fileName = RequestFileName(_productName);

            if (data.overrideExecutableName)
            {
                if (string.IsNullOrWhiteSpace(data.executableName))
                {
                    Debug.LogWarning("Executable path was invalid, using default exe.", this);
                    _fileName = RequestFileName(_productName);
                }
                else
                {
                    _fileName = RequestFileName(data.executableName);
                }
            }

            var version = SetVersion(data.isReleaseBuild);

            BuildFlow(data.Target, version, scenes, branchName, folder, data.GetBuildOptions(),
                data.name, data.isHeadless);
        }

        private void SwitchBuildTargets(BuildProfileDataSO data)
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = data.isHeadless
                ? StandaloneBuildSubtarget.Server
                : StandaloneBuildSubtarget.Player;

            if (!(EditorUserBuildSettings.selectedBuildTargetGroup == data.TargetGroup &&
                  EditorUserBuildSettings.activeBuildTarget == data.Target))
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(data.TargetGroup, data.Target))
                {
                    Debug.LogError($"Failed to switch build target to {data.platformTarget}.", this);
                }
            }
        }

        private void CleanupWizard()
        {
            defaultBuildData.Clear();
            defaultBuildData = null;
            PlayerSettings.bundleVersion = _projectVersion.CoreVersion;
            EditorUtility.RequestScriptReload();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            Close();
        }

        private void SaveCurrentBuildTarget()
        {
            _currentBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            _currentBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            _currentSubTarget = (int)EditorUserBuildSettings.standaloneBuildSubtarget;

            EditorPrefs.SetString(EditorBuildTargetKey, _currentBuildTarget.ToString());
            EditorPrefs.SetString(EditorBuildTargetGroupKey, _currentBuildTargetGroup.ToString());
            EditorPrefs.SetInt(EditorBuildSubtargetKey, _currentSubTarget);
        }

        private void LoadCurrentBuildTarget()
        {
            if (EditorPrefs.HasKey(EditorBuildTargetKey))
            {
                if (!Enum.TryParse(EditorPrefs.GetString(EditorBuildTargetKey), out _currentBuildTarget))
                {
                    Debug.LogWarning($"Failed to load {typeof(BuildTarget)}");
                }
            }

            if (EditorPrefs.HasKey(EditorBuildTargetGroupKey))
            {
                if (!Enum.TryParse(EditorPrefs.GetString(EditorBuildTargetGroupKey), out _currentBuildTargetGroup))
                {
                    Debug.LogWarning($"Failed to load {typeof(BuildTargetGroup)}");
                }
            }

            if (EditorPrefs.HasKey(EditorBuildSubtargetKey))
            {
                _currentSubTarget = EditorPrefs.GetInt(EditorBuildSubtargetKey);
            }
        }

        private void BuildFlow(BuildTarget target, string version, string[] scenes, string branchName, string folder,
            BuildOptions options,
            string buildName = "", bool isHeadless = false)
        {
            var path = GetBuildPath(version, branchName, folder, buildName);
            var buildOptions = GetBuildOptions(options, target, path, scenes, isHeadless);
            BuildPlayer(buildOptions);
            try
            {
                WriteBuildInformation(path, GitExtensions.GetFullCommitHash(), GitExtensions.GetBranchName(),
                    _projectVersion.FullVersion, DateTime.Now.ToString(DateTimeFormat), buildName);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write version information: {e}.", this);
            }
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

        private void CreateProfile()
        {
            var profilePath = Path.Combine(Application.dataPath, ProfileFolder);
            if (!Directory.Exists(profilePath))
            {
                Directory.CreateDirectory(profilePath);
            }

            CreateBuildDataWindow.ShowWindow(profilePath, CreateProfileAsset);
        }

        private void CreateProfileAsset(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogWarning("Received an empty file to save.", this);
                return;
            }

            var newProfile = CreateInstance<BuildProfileSO>();
            newProfile.name = fileName;

            profile = newProfile;

            var path = Path.Combine(ProfileFolderPath, newProfile.name + ".asset");
            AssetDatabase.CreateAsset(newProfile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateBuildData()
        {
            defaultBuildData.Add(CreateInstance<BuildProfileDataSO>());
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

        private void RevertToCurrentBuildTarget()
        {
            LoadCurrentBuildTarget();

            EditorUserBuildSettings.standaloneBuildSubtarget = (StandaloneBuildSubtarget)_currentSubTarget;

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(_currentBuildTargetGroup, _currentBuildTarget))
            {
                Debug.LogError($"Failed to switch build target to {_currentBuildTarget}.", this);
            }
        }
    }
}
#endif
