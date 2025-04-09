using System;
using System.Collections.Generic;
using System.Linq;
using Unity.HLODSystem.SpaceManager;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.HLODSystem
{
    public class HLODBaseEditor : VisualElement
    {
        public static class Styles
        {
            public static GUIContent GenerateButtonEnable = new GUIContent("Generate", "Generate HLOD mesh.");
            public static GUIContent RegenerateButtonEnable = new GUIContent("Regenerate", "Regenerate HLOD mesh.");
            public static GUIContent DestroyButtonEnable = new GUIContent("Destroy", "Destroy HLOD mesh.");
            public static GUIContent DestroyButtonNotExists = new GUIContent("Destroy", "HLOD must be created before the destroying.");

            public static Color RedTextColor = new Color(1.0f, 0.15f, 0.15f);
            public static Color BlueTextColor = new Color(0.4f, 0.5f, 1.0f);
        }  
        
        private SerializedProperty m_ChunkSizeProperty;
        private SerializedProperty m_LODDistanceProperty;
        private SerializedProperty m_CullDistanceProperty;

        private LODSlider m_LODSlider;

        private Type[] m_SpaceSplitterTypes;
        private List<string> m_SpaceSplitterNames;

        private Type[] m_SimplifierTypes;
        private List<string> m_SimplifierNames;

        private Type[] m_StreamingTypes;
        private List<string> m_StreamingNames;

        private Type[] m_UserDataSerializerTypes;
        private List<string> m_UserDataSerializerNames;
        
        private ISpaceSplitter m_splitter;

        [InitializeOnLoadMethod]
        static void InitTagTagUtils()
        {
            if (LayerMask.NameToLayer(HLOD.HLODLayerStr) == -1)
            {
                TagUtils.AddLayer(HLOD.HLODLayerStr);
                Tools.lockedLayers |= 1 << LayerMask.NameToLayer(HLOD.HLODLayerStr);
            }
        }

        Label TreeDepthLabel;
        Label TreeDepthLevelWarning;
        Label SubTreeLabel;
        Label MissingSpaceSplittersLabel;
        Label MissingSimplifiersLabel;
        Label MissingStreamingProvidersLabel;
        Label MissingUserDataSerializersLabel;

        public VisualElement Properties;
        public Foldout Common;
        public Foldout SpaceSplitter;
        public Foldout Simplifier;
        public Foldout Streaming;
        public Foldout UserDataSerializer;

        public void SetDirtyAndApply(SerializedObject serializedObject, HLODBase hlod)
        {
            EditorUtility.SetDirty(hlod);
            serializedObject.ApplyModifiedProperties();
        }

        public HLODBaseEditor(HLODBase hlod, SerializedObject serializedObject)
        {
            m_SpaceSplitterTypes = SpaceManager.SpaceSplitterTypes.GetTypes();
            m_SpaceSplitterNames = m_SpaceSplitterTypes.Select(t => t.Name).ToList();

            m_SimplifierTypes = HLODSystem.Simplifier.SimplifierTypes.GetTypes();
            m_SimplifierNames = m_SimplifierTypes.Select(t => t.Name).ToList();
            m_SimplifierNames = new List<string>(m_SimplifierTypes.Length);
            for (var i = 0; i < m_SimplifierTypes.Length; ++i)
                m_SimplifierNames.Add(m_SimplifierTypes[i].Name);

            m_StreamingTypes = HLODSystem.Streaming.StreamingBuilderTypes.GetTypes();
            m_StreamingNames = m_StreamingTypes.Select(t => t.Name).ToList();
            m_StreamingNames = new List<string>(m_StreamingTypes.Length);
            for (var i = 0; i < m_StreamingTypes.Length; ++i)
                m_StreamingNames.Add(m_StreamingTypes[i].Name);

            m_UserDataSerializerTypes = Serializer.UserDataSerializerTypes.GetTypes();
            m_UserDataSerializerNames = m_UserDataSerializerTypes.Select(t => t.Name).ToList();
            m_UserDataSerializerNames = new List<string>(m_UserDataSerializerTypes.Length);
            for (var i = 0; i < m_UserDataSerializerTypes.Length; ++i)
                m_UserDataSerializerNames.Add(m_UserDataSerializerTypes[i].Name);

            m_ChunkSizeProperty = serializedObject.FindProperty("m_ChunkSize");
            m_LODDistanceProperty = serializedObject.FindProperty("m_LODScreenRatioThreshold");
            m_CullDistanceProperty = serializedObject.FindProperty("m_CullScreenRatioThreshold");

            Properties = new VisualElement();
            Add(Properties);

            {
                Common = new Foldout();
                Common.text = "Common";
                Common.name = Common.text;
                Properties.Add(Common);

                var chunkSize = new PropertyField(m_ChunkSizeProperty);
                Common.Add(chunkSize);
                chunkSize.RegisterValueChangeCallback((e) => { HLODUtils.GetChunkSizePropertyValue(m_ChunkSizeProperty.floatValue);});

                TreeDepthLabel = new Label();
                TreeDepthLabel.style.color = Styles.BlueTextColor;
                TreeDepthLabel.style.flexWrap = new StyleEnum<Wrap>(Wrap.Wrap);
                Common.Add(TreeDepthLabel);

                TreeDepthLevelWarning = new Label() { text = $"Warning: A Node Level Count greater than 5 may cause the editor to freeze." };
                TreeDepthLevelWarning.style.color = Styles.RedTextColor;
                TreeDepthLevelWarning.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
                Common.Add(TreeDepthLevelWarning);
                TreeDepthLevelWarning.style.display = DisplayStyle.None;

                SubTreeLabel = new Label() { name = "SubTreeLabel" };
                SubTreeLabel.style.color = Styles.BlueTextColor;
                SubTreeLabel.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
                Common.Add(SubTreeLabel);

                m_LODSlider = new LODSlider(true, "Cull");
                m_LODSlider.InsertRange("High", m_LODDistanceProperty);
                m_LODSlider.InsertRange("Low", m_CullDistanceProperty);
                Common.Add(m_LODSlider);
            }

            {
                SpaceSplitter = new Foldout();
                SpaceSplitter.text = "Space Splitter";
                SpaceSplitter.name = SpaceSplitter.text;
                Properties.Add(SpaceSplitter);

                MissingSpaceSplittersLabel = new Label() { text = "Cannot find Space Splitters." };
                MissingSpaceSplittersLabel.name = "MissingSpaceSplittersLabel";
                SpaceSplitter.Add(MissingSpaceSplittersLabel);

                var spaceSplitterDropdown = new DropdownField();
                spaceSplitterDropdown.label = SpaceSplitter.text;
                SpaceSplitter.Add(spaceSplitterDropdown);
                spaceSplitterDropdown.choices = m_SpaceSplitterNames;
                spaceSplitterDropdown.value = hlod.SpaceSplitterType.Name;
                spaceSplitterDropdown.RegisterValueChangedCallback((e) =>
                {
                    var spaceSplitterIndex = m_SpaceSplitterNames.IndexOf(e.newValue);
                    hlod.SpaceSplitterType = m_SpaceSplitterTypes[spaceSplitterIndex];

                    m_splitter = SpaceSplitterTypes.CreateInstance(hlod);

                    var bounds = hlod.GetBounds();
                    int depth = m_splitter.CalculateTreeDepth(bounds, m_ChunkSizeProperty.floatValue);
                    TreeDepthLabel.text = ($"The HLOD tree will be created with {depth} levels.");

                    TreeDepthLevelWarning.style.display = depth > 5 ? DisplayStyle.Flex : DisplayStyle.None;

                    if (m_splitter != null)
                    {
                        var info = hlod.SpaceSplitterType.GetMethod("CreateGUI");
                        if (info != null)
                        {
                            if (info.IsStatic == true)
                            {
                                SpaceSplitter.Add(info.Invoke(null, new object[] { hlod.SpaceSplitterOptions }) as VisualElement);
                            }
                        }

                        int subTreeCount = m_splitter.CalculateSubTreeCount(hlod.GetBounds());
                        SubTreeLabel.text = $"The HLOD tree will be created with {subTreeCount} sub trees.";
                    }

                    SetDirtyAndApply(serializedObject, hlod);
                });


                if (m_SpaceSplitterTypes.Length == 0)
                {
                    spaceSplitterDropdown.enabledSelf = false;
                    MissingSpaceSplittersLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    MissingSpaceSplittersLabel.style.display = DisplayStyle.None;
                }
            }

            {
                Simplifier = new Foldout();
                Simplifier.text = "Simplifier";
                Simplifier.name = Simplifier.text;
                Properties.Add(Simplifier);

                MissingSimplifiersLabel = new Label() { text = "Cannot find Simplifiers." };
                MissingSimplifiersLabel.name = "MissingSimplifiersLabel";
                Simplifier.Add(MissingSimplifiersLabel);

                var simplifierDropdown = new DropdownField();
                simplifierDropdown.label = Simplifier.text;
                Simplifier.Add(simplifierDropdown);
                simplifierDropdown.choices = m_SimplifierNames;
                simplifierDropdown.value = hlod.SimplifierType.Name;
                simplifierDropdown.RegisterValueChangedCallback((e) =>
                {
                    var simplifierIndex = m_SimplifierNames.IndexOf(e.newValue);
                    hlod.SimplifierType = m_SimplifierTypes[simplifierIndex];

                    var info = hlod.SpaceSplitterType.GetMethod("CreateGUI");
                    if (info != null)
                    {
                        if (info.IsStatic == true)
                        {
                            var settingsUI = info.Invoke(null, new object[] { hlod.SimplifierOptions }) as VisualElement;
                            settingsUI.style.marginLeft = 5;
                            Simplifier.Add(settingsUI);
                        }
                    }
                    SetDirtyAndApply(serializedObject, hlod);
                });

                if (m_SimplifierTypes.Length == 0)
                {
                    simplifierDropdown.enabledSelf = false;
                    MissingSimplifiersLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    MissingSimplifiersLabel.style.display = DisplayStyle.None;
                }
            }

            {
                Streaming = new Foldout();
                Streaming.text = "Streaming";
                Streaming.name = Streaming.text;
                Properties.Add(Streaming);

                MissingStreamingProvidersLabel = new Label() { text = "Cannot find streaming providers." };
                MissingStreamingProvidersLabel.name = "MissingStreamingProvidersLabel";
                Streaming.Add(MissingStreamingProvidersLabel);

                var streamingDropdown = new DropdownField();
                streamingDropdown.label = Streaming.text;
                Streaming.Add(streamingDropdown);
                streamingDropdown.choices = m_StreamingNames;
                streamingDropdown.value = hlod.StreamingType.Name;
                streamingDropdown.RegisterValueChangedCallback((e) =>
                {
                    var streamingIndex = m_StreamingNames.IndexOf(e.newValue);
                    hlod.StreamingType = m_StreamingTypes[streamingIndex];

                    var info = m_StreamingTypes[streamingIndex].GetMethod("CreateGUI");
                    if (info != null)
                    {
                        if (info.IsStatic == true)
                        {
                            Streaming.Add(info.Invoke(null, new object[] { hlod.StreamingOptions }) as VisualElement);
                        }
                    }
                    SetDirtyAndApply(serializedObject, hlod);
                });

                if (m_StreamingTypes.Length == 0)
                {
                    streamingDropdown.enabledSelf = false;
                    MissingStreamingProvidersLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    MissingStreamingProvidersLabel.style.display = DisplayStyle.None;
                }
            }
            
            {
                UserDataSerializer = new Foldout();
                UserDataSerializer.text = "UserDataSerializer";
                UserDataSerializer.name = UserDataSerializer.text;
                Properties.Add(UserDataSerializer);

                MissingUserDataSerializersLabel = new Label() { text = "Cannot find UserDataSerializers." };
                MissingUserDataSerializersLabel.name = "MissingUserDataSerializersLabel";
                UserDataSerializer.Add(MissingUserDataSerializersLabel);

                var userDataSerializersDropdown = new DropdownField();
                userDataSerializersDropdown.label = UserDataSerializer.text;
                UserDataSerializer.Add(userDataSerializersDropdown);
                userDataSerializersDropdown.choices = m_UserDataSerializerNames;
                userDataSerializersDropdown.value = hlod.UserDataSerializerType.Name;
                userDataSerializersDropdown.RegisterValueChangedCallback((e) =>
                {
                    var serializerIndex = m_UserDataSerializerNames.IndexOf(e.newValue);
                    hlod.UserDataSerializerType = m_UserDataSerializerTypes[serializerIndex];
                    SetDirtyAndApply(serializedObject, hlod);
                });

                if (m_UserDataSerializerTypes.Length == 0)
                {
                    userDataSerializersDropdown.enabledSelf = false;
                    MissingUserDataSerializersLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    MissingUserDataSerializersLabel.style.display = DisplayStyle.None;
                }
            }


            //TODO: to add the functionality back in to disable the destroy button when there's no data to destroy, we need either a callback (bleh) or a way to poll at the right moment what the state should be.
            //GUIContent generateButtonStyle = Styles.GenerateButtonEnable;
            //GUIContent destroyButtonStyle = Styles.DestroyButtonNotExists;

            //if (hlod.GeneratedObjects.Count > 0 )
            //{
            //    generateButtonStyle = Styles.RegenerateButtonEnable;
            //    destroyButtonStyle = Styles.DestroyButtonEnable;
            //}

            var generateButton = new Button() { name = "Generate" };
            generateButton.text = generateButton.name;
            generateButton.clicked += () =>
            {
                if (hlod.GeneratedObjects.Count > 0)
                {
                    if (hlod is HLOD)
                    {
                        CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod as HLOD));
                    }
                    else if (hlod is TerrainHLOD)
                    {
                        CoroutineRunner.RunCoroutine(TerrainHLODCreator.Destroy(hlod as TerrainHLOD));
                    }

                    if (hlod is HLOD)
                    {
                        CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod as HLOD));
                    }
                    else if (hlod is TerrainHLOD)
                    {
                        CoroutineRunner.RunCoroutine(TerrainHLODCreator.Create(hlod as TerrainHLOD));
                    }
                }
            };
            Add(generateButton);

            var destroyButton = new Button() { name = "Destroy" };
            destroyButton.text = destroyButton.name;
            destroyButton.clicked += () =>
            {
                if (hlod is HLOD)
                {
                    CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod as HLOD));
                }
                else if (hlod is TerrainHLOD)
                {
                    CoroutineRunner.RunCoroutine(TerrainHLODCreator.Destroy(hlod as TerrainHLOD));
                }
            };
            Add(destroyButton);

            //if (EditorGUI.EndChangeCheck())
            //{
            //    EditorUtility.SetDirty(hlod);
            //}
        }

    }

}