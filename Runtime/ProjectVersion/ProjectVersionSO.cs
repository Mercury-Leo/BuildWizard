using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Utility.Git.Extensions;

namespace ProjectVersion
{
    [CreateAssetMenu(fileName = "Project Version", menuName = "Build Wizard/Version")]
    public class ProjectVersionSO : ScriptableObject
    {
        [SerializeField, ReadOnly] private int major;
        [SerializeField, ReadOnly] private int minor;

        public int Major => major;
        public int Minor => minor;

        public string CoreVersion => $"{major}.{minor}";
        public string Version => $"{CoreVersion}.{GitExtensions.GetNumberOfCommits()}";
        public string FullVersion => $"{Version}-{GitExtensions.GetCommitHash()}";

        private void OnValidate()
        {
            if (major < 0)
            {
                major = 0;
            }

            if (minor < 0)
            {
                minor = 0;
            }
        }

        public void UpgradeMajor()
        {
            major++;
            SaveSO();
        }

        public void UpgradeMinor()
        {
            minor++;
            SaveSO();
        }

        private void SaveSO()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
#endif
        }
    }
}
