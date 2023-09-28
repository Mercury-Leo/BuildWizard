#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Profiles
{
    [Serializable]
    [InlineEditor]
    [CreateAssetMenu(fileName = "BuildData", menuName = "Build Wizard/BuildData")]
    public class BuildProfileDataSO : SerializedScriptableObject
    {
        [SerializeField] [ValueDropdown(nameof(_groupValues))]
        public string platformTargetGroup = BuildTargetGroup.Standalone.ToString();

        [SerializeField] [ValueDropdown(nameof(_targetValues))]
        public string platformTarget = BuildTarget.StandaloneWindows.ToString();

        [SerializeField] public bool isReleaseBuild;
        [SerializeField] public bool isHeadless;
        [SerializeField] public bool overrideExecutableName;

        [SerializeField] [ShowIf(nameof(overrideExecutableName))]
        public string executableName;

        [FoldoutGroup("Build Options Data")] [SerializeField]
        public bool isDevelopmentBuild;

        [FoldoutGroup("Build Options Data")] [SerializeField] [ShowIf(nameof(isDevelopmentBuild))]
        public bool autoconnectProfiler;

        [FoldoutGroup("Build Options Data")] [SerializeField] [ShowIf(nameof(isDevelopmentBuild))]
        public bool enableDeepProfile;

        [FoldoutGroup("Build Options Data")] [SerializeField] [ShowIf(nameof(isDevelopmentBuild))]
        public bool enableScriptDebugging;

        [FoldoutGroup("Build Options Data")]
        [SerializeField]
        [ShowIf("@this.isDevelopmentBuild && this.enableScriptDebugging")]
        public bool waitForDebugger;

        [FoldoutGroup("Build Options Data")] [SerializeField]
        public bool detailedBuildReport;

        public BuildTargetGroup TargetGroup => Enum.TryParse<BuildTargetGroup>(platformTargetGroup, out var result)
            ? result
            : BuildTargetGroup.Standalone;

        public BuildTarget Target => Enum.TryParse<BuildTarget>(platformTargetGroup, out var result)
            ? result
            : BuildTarget.StandaloneWindows;

        private BuildTargetGroup[] _allTargetGroups = (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup));
        private BuildTarget[] _allTargets = (BuildTarget[])Enum.GetValues(typeof(BuildTarget));
        private IEnumerable _groupValues = new ValueDropdownList<string>();
        private IEnumerable _targetValues = new ValueDropdownList<string>();

        private void OnEnable()
        {
            _groupValues = GetSupportedBuildTargetGroups();
            _targetValues = GetSupportedBuildTargets();
        }

        public BuildOptions GetBuildOptions()
        {
            var options = BuildOptions.CleanBuildCache | BuildOptions.ShowBuiltPlayer;

            if (isDevelopmentBuild)
            {
                options |= BuildOptions.Development;
                if (autoconnectProfiler)
                {
                    options |= BuildOptions.ConnectToHost;
                }

                if (enableDeepProfile)
                {
                    options |= BuildOptions.EnableDeepProfilingSupport;
                }

                if (enableScriptDebugging)
                {
                    options |= BuildOptions.AllowDebugging;

                    if (waitForDebugger)
                    {
                        options |= BuildOptions.WaitForPlayerConnection;
                    }
                }
            }

            if (detailedBuildReport)
            {
                options |= BuildOptions.DetailedBuildReport;
            }

            return options;
        }

        private string[] GetSupportedBuildTargetGroups()
        {
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;

            var supportedGroups = new List<string>();

            foreach (var group in _allTargetGroups)
            {
                if (BuildPipeline.IsBuildTargetSupported(group, activeBuildTarget))
                {
                    supportedGroups.Add(group.ToString());
                }
            }

            return supportedGroups.ToArray();
        }

        private string[] GetSupportedBuildTargets()
        {
            var activeBuildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;

            var supportedGroups = new List<string>();

            foreach (var target in _allTargets)
            {
                if (BuildPipeline.IsBuildTargetSupported(activeBuildTarget, target))
                {
                    supportedGroups.Add(target.ToString());
                }
            }

            return supportedGroups.ToArray();
        }
    }
}
#endif