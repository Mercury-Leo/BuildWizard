#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Profile
{
    public class CreateBuildDataWindow : OdinEditorWindow
    {
        private string _fileName;
        private static string _folderPath = Application.dataPath + "/" + "BuildData";
        private Action<string> _selectedName;

        public static void ShowWindow(string path, Action<string> callback)
        {
            _folderPath = path;
            var window = GetWindow<CreateBuildDataWindow>("Name Build Data");
            window._selectedName = callback;
        }

        protected override void OnGUI()
        {
            GUILayout.Label("Enter a file name for the Build Data");

            _fileName = EditorGUILayout.TextField(_fileName);

            if (GUILayout.Button("Create"))
            {
                if (string.IsNullOrWhiteSpace(_fileName))
                {
                    EditorUtility.DisplayDialog("Build Data Creation", "Enter a valid name", "Ok");
                    return;
                }

                var fileExists = BuildDataExists(_fileName);

                if (fileExists)
                {
                    EditorUtility.DisplayDialog("Build Data already exists!",
                        $"{_fileName} already exists in the folder.", "Ok");
                    return;
                }

                _selectedName?.Invoke(_fileName);

                Close();
            }
        }

        private bool BuildDataExists(string fileName)
        {
            var files = Directory.GetFiles(_folderPath, "*.asset");
            return files.Select(Path.GetFileNameWithoutExtension).Any(item => item.Equals(fileName));
        }
    }
}
#endif
