/*
 * This document is the property of Oversight Technologies Ltd that reserves its rights document and to
 * the data / invention / content herein described.This document, including the fact of its existence, is not to be
 * disclosed, in whole or in part, to any other party and it shall not be duplicated, used, or copied in any
 * form, without the express prior written permission of Oversight authorized person. Acceptance of this document
 * will be construed as acceptance of the foregoing conditions.
 */

using Build_Wizard.Git.Extensions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Build_Wizard.Version
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
        public string FullVersion => $"{Version}-{GitExtensions.CommitHash}";

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
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
