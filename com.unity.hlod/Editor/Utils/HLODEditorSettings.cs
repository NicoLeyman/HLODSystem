using System.Collections.Generic;
using System.IO;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.SettingsManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.HLODSystem.GUIUtils;

namespace Unity.HLODSystem
{
    class HLODEditorSettings : ScriptableObject
    {
        const string k_PackageName = "com.unity.hlod";
        public const string k_MyCustomSettingsPath = "HLOD/Editor/HLODSettings.asset";

        private static HLODEditorSettings instance = null;
        public static HLODEditorSettings Instance 
        { 
            get 
            {
                if (instance == null)
                {
                    instance = GetOrCreateSettings();
                }
                return instance;
            }
        }

        // Common
        public bool OverrideDefaultShader;
        public Shader DefaultShader;

        // Simple Batcher
        public bool OverrideDefaultMaterialMapping;
        public MaterialMapping DefaultMaterialMapping;

        // Terrain
        public bool OverrideDefaultTerrainShader;
        public Shader DefaultTerrainShader;

        internal static HLODEditorSettings GetOrCreateSettings()
        {
            var folderPath = Path.Combine(Application.dataPath, Path.GetDirectoryName(k_MyCustomSettingsPath));
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);      
            }

            var projectPath = "Assets/" + k_MyCustomSettingsPath;
            var settings = AssetDatabase.LoadAssetAtPath<HLODEditorSettings>(projectPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<HLODEditorSettings>();
                AssetDatabase.CreateAsset(settings, projectPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        static class HLODEditorSettingsProvider
        {
            [SettingsProvider]
            public static SettingsProvider CreateSettingsProvider()
            {
                const string k_PreferencesPath = "Preferences/HLOD";

                var provider = new SettingsProvider(k_PreferencesPath, SettingsScope.User)
                {
                    label = "HLOD",
                    activateHandler = (searchContext, rootElement) =>
                    {
                        var settings = HLODEditorSettings.GetSerializedSettings();

                        var title = new Label()
                        {
                            text = "HLOD"
                        };
                        title.AddToClassList("title");
                        rootElement.Add(title);

                        var properties = new VisualElement()
                        {
                            style =
                            {
                                flexDirection = FlexDirection.Column
                            }
                        };
                        properties.AddToClassList("property-list");
                        rootElement.Add(properties);

                        var commonFoldout = new Foldout() { text = "Common", value = true };
                        properties.Add(commonFoldout);

                        commonFoldout.Add(new OverridablePropertyElement(settings, nameof(DefaultShader), (bool o) => { return GraphicsUtils.GetDefaultShader().name; }, "Default Shader", "A value of null falls back to the current render pipeline's default shader."));
                        
                        var simpleBatcherFoldout = new Foldout() { text = "Simple Batcher", value = true };
                        properties.Add(simpleBatcherFoldout);
                        simpleBatcherFoldout.Add(new PropertyField(settings.FindProperty(nameof(DefaultMaterialMapping)), "Default Material Mapping") { tooltip = "Default Material Mapping referenced by HLOD Components using the Simple Batcher" });

                        var terrainFoldout = new Foldout() { text = "Terrain", value = true };
                        properties.Add(terrainFoldout);
                        terrainFoldout.Add(new OverridablePropertyElement(settings, nameof(DefaultTerrainShader), (bool o) => { return GraphicsUtils.GetDefaultTerrainShader().name; }, "Default Terrain Shader", "A value of null falls back to the common default shader."));

                        rootElement.Bind(settings);
                    },

                    keywords = new HashSet<string>(new[] { "HLOD", "MaterialMapping" })
                };

                return provider;
            }
        }
    }
}