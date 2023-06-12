using System.IO;
using UnityEditor;
using UnityEngine;

namespace Version.Extensions
{
    public static class ProjectVersionExtensions
    {
        private const string Assets = "Assets";
        private const string ResourcesFolder = "Resources";
        private const string ProjectVersion = "Project Version";
        private const string AssetFileEnding = ".asset";

        public static ProjectVersionSO FindOrCreateProjectVersion()
        {
            var path = Path.Combine(Application.dataPath, ResourcesFolder);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                CreateProjectVersion();
            }

            var projectVersion = Resources.Load<ProjectVersionSO>(ProjectVersion);

            if (projectVersion == null)
            {
                projectVersion = CreateProjectVersion();
            }

            return projectVersion;
        }

        private static ProjectVersionSO CreateProjectVersion()
        {
#if UNITY_EDITOR
            var version = ScriptableObject.CreateInstance<ProjectVersionSO>();
            AssetDatabase.CreateAsset(version, Path.Combine(Assets, ResourcesFolder, ProjectVersion + AssetFileEnding));
            AssetDatabase.SaveAssets();
            return version;
#endif
            return null;
        }
    }
}