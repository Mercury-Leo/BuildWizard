using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Wizard;

namespace Build
{
    public static class WizardExtensions
    {
        public static void BuildPlayer(BuildPlayerOptions options)
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

        public static string[] GetBuildScenes()
        {
            var buildScenes = EditorBuildSettings.scenes;
            var scenes = new string[buildScenes.Length];
            for (var index = 0; index < buildScenes.Length; index++)
            {
                scenes[index] = EditorBuildSettings.scenes[index].path;
            }

            return scenes;
        }

        public static BuildPlayerOptions GetBuildOptions(BuildOptions option, BuildTarget target, string path, string[] scenes,
            bool isHeadless = false)
        {
            var buildOptions = new BuildPlayerOptions
            {
                options = option, locationPathName = path, target = target, scenes = scenes
            };
            if (isHeadless)
            {
                buildOptions.subtarget = (int)StandaloneBuildSubtarget.Server;
            }

            return buildOptions;
        }

        public static string GetFolderBranchName(string branch)
        {
            var regex = new Regex(WizardConventions.Pattern);
            var match = regex.Match(branch);
            return match.Success ? match.Groups[2].Value : string.Empty;
        }

        public static string RequestFileName(string outputName)
        {
            if (EditorUserBuildSettings.activeBuildTarget is BuildTarget.EmbeddedLinux or BuildTarget.StandaloneLinux64
                or BuildTarget.LinuxHeadlessSimulation)
            {
                return outputName + ".x86_64";
            }

            return outputName + ".exe";
        }
    }
}
