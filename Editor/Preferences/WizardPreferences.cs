using UnityEditor;
using UnityEngine;
using Wizard;

namespace Build
{
    public static class WizardPreferences
    {
        private const string EditorZippingKey = nameof(EditorZippingKey);
        private const string EditorZipTogetherKey = nameof(EditorZipTogetherKey);
        private const string EditorForceWizardKey = nameof(EditorForceWizardKey);

        [SettingsProvider]
        public static SettingsProvider WizardPrefs()
        {
            var provider = new SettingsProvider(WizardConventions.WizardPreferencesName, SettingsScope.User)
            {
                label = "Build Wizard",
                guiHandler = (searchContext) =>
                {
                    GUILayout.Space(10);

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField(new GUIContent("Zip the output", "Zips each build folder by itself."),
                        GUILayout.Width(250));
                    BoolHandler(EditorZippingKey);
                    GUILayout.EndHorizontal();

                    if (EditorPrefs.HasKey(EditorZippingKey))
                    {
                        if (EditorPrefs.GetBool(EditorZippingKey))
                        {
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(60);
                            EditorGUILayout.LabelField(
                                new GUIContent("Zip in one package",
                                    "Zips all the outputted builds into one zip instead of each build by itself."),
                                GUILayout.Width(210));
                            BoolHandler(EditorZipTogetherKey);
                            GUILayout.EndHorizontal();
                        }
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField(new GUIContent("Allow building only via the Build Wizard",
                            "Disables the regular build flow and makes sure only the build wizard is used."),
                        GUILayout.Width(250));
                    BoolHandler(EditorForceWizardKey);
                    GUILayout.EndHorizontal();
                }
            };

            return provider;
        }

        private static void BoolHandler(string editorKey)
        {
            var value = EditorPrefs.GetBool(editorKey);
            value = EditorGUILayout.Toggle(value);
            EditorPrefs.SetBool(editorKey, value);
        }
    }
}
