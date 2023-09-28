#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using System.IO;
using Editor;

namespace Profiles
{
    [CreateAssetMenu(fileName = "newBuildProfile", menuName = "Build Wizard/Profile")]
    public class BuildProfileSO : ScriptableObject
    {
        [ListDrawerSettings(ShowIndexLabels = true)]
        [InlineButton(nameof(CreateBuildData))]
        [field: SerializeField]
        public List<BuildProfileDataSO> BuildTargets { get; private set; }

        private const string BuildDataPath = "Assets" + "/" + BuildDataFolder;
        private const string BuildDataFolder = "BuildData";

        [Button("Create new Build Data")]
        private void CreateBuildData()
        {
            var buildDataFolder = Path.Combine(Application.dataPath, BuildDataFolder);
            if (!Directory.Exists(buildDataFolder))
            {
                Directory.CreateDirectory(buildDataFolder);
            }

            CreateBuildDataWindow.ShowWindow(buildDataFolder, CreateBuildDataAsset);
        }

        private void CreateBuildDataAsset(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                Debug.LogWarning("Received an empty file to save.", this);
                return;
            }

            var newBuildData = CreateInstance<BuildProfileDataSO>();
            newBuildData.name = fileName;

            BuildTargets.Add(newBuildData);

            var path = Path.Combine(BuildDataPath, newBuildData.name + ".asset");
            UnityEditor.AssetDatabase.CreateAsset(newBuildData, path);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
#endif