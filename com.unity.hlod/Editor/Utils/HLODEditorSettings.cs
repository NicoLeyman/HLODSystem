using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace Unity.HLODSystem
{
    static class HLODEditorSettings
    {
        const string k_PackageName = "com.unity.hlod";

        static Settings s_Instance;

        static Settings instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new Settings(k_PackageName);

                return s_Instance;
            }
        }

        [UserSetting("Simple Batcher","Default Shader")]
        public static UserSetting<Shader> DefaultShader = new UserSetting<Shader>(instance, "DefaultShader", null, SettingsScope.Project);
        
        static class HLODEditorSettingsProvider
        {
            const string k_PreferencesPath = "Preferences/HLOD";
        
            [SettingsProvider]
            static SettingsProvider CreateSettingsProvider()
            {
                // The last parameter tells the provider where to search for settings.
                var provider = new UserSettingsProvider(k_PreferencesPath,
                    HLODEditorSettings.instance,
                    new [] { typeof(HLODEditorSettingsProvider).Assembly });

                return provider;
            }
        }
    }
}