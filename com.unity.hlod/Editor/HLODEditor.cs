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
    [CustomEditor(typeof(HLOD))]
    public class HLODEditor : Editor
    {
        public static class Styles
        {
            public static GUIContent GenerateButtonEnable = new GUIContent("Generate", "Generate HLOD mesh.");
            public static GUIContent RegenerateButtonEnable = new GUIContent("Regenerate", "Regenerate HLOD mesh.");
            public static GUIContent DestroyButtonEnable = new GUIContent("Destroy", "Destroy HLOD mesh.");
            public static GUIContent DestroyButtonNotExists = new GUIContent("Destroy", "HLOD must be created before the destroying.");

            public static GUIStyle RedTextColor = new GUIStyle();
            public static GUIStyle BlueTextColor = new GUIStyle();

            static Styles()
            {
                RedTextColor.normal.textColor = Color.red;
                BlueTextColor.normal.textColor = new Color(0.4f, 0.5f, 1.0f);
            }

        }        

        private SerializedProperty m_MinObjectSizeProperty;

        private Type[] m_BatcherTypes;
        private List<string> m_BatcherNames;

        Label MissingBatchersLabel;

        Foldout BatcherFoldout;
        VisualElement BatcherOptions;

        [InitializeOnLoadMethod]
        static void InitTagTagUtils()
        {
            if (LayerMask.NameToLayer(HLOD.HLODLayerStr) == -1)
            {
                TagUtils.AddLayer(HLOD.HLODLayerStr);
                Tools.lockedLayers |= 1 << LayerMask.NameToLayer(HLOD.HLODLayerStr);
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var hlod = target as HLOD;
            var hlodEditor = new HLODBaseEditor(hlod, serializedObject);

            m_BatcherTypes = BatcherTypes.GetTypes();
            m_BatcherNames = new List<string>(m_BatcherTypes.Length);
            for (var i = 0; i < m_BatcherTypes.Length; ++i)
                m_BatcherNames.Add(m_BatcherTypes[i].Name);

            m_MinObjectSizeProperty = serializedObject.FindProperty("m_MinObjectSize");

            hlodEditor.Common.Add(new PropertyField(m_MinObjectSizeProperty));

            {
                BatcherFoldout = new Foldout();
                BatcherFoldout.text = "Batcher";
                BatcherFoldout.name = BatcherFoldout.text;
                hlodEditor.Properties.Add(BatcherFoldout);

                MissingBatchersLabel = new Label() { text = "Cannot find Batchers." };
                MissingBatchersLabel.name = "MissingBatchersLabel";
                MissingBatchersLabel.visible = false;
                BatcherFoldout.Add(MissingBatchersLabel);

                var batcherDropdown = new DropdownField();
                batcherDropdown.label = BatcherFoldout.text;
                BatcherFoldout.Add(batcherDropdown);
                batcherDropdown.choices = m_BatcherNames;
                batcherDropdown.value = hlod.BatcherType.Name;
                batcherDropdown.RegisterValueChangedCallback((e) =>
                {
                    AddBatcherOptions(hlod, e.newValue);
                    hlodEditor.SetDirtyAndApply(serializedObject, hlod);
                });

                if (m_BatcherTypes.Length == 0)
                {
                    batcherDropdown.enabledSelf = false;
                    MissingBatchersLabel.visible = true;
                }
                else
                {
                    MissingBatchersLabel.visible = false;
                    AddBatcherOptions(hlod, batcherDropdown.value);
                }
            }

            return hlodEditor;
        }

        void AddBatcherOptions(HLOD hlod, string batcherName)
        {
            if(BatcherOptions != null)
            {
                BatcherFoldout.Remove(BatcherOptions);
            }

            var batcherIndex = m_BatcherNames.IndexOf(batcherName);
            hlod.BatcherType = m_BatcherTypes[batcherIndex];

            var info = m_BatcherTypes[batcherIndex].GetMethod("CreateGUI");
            if (info != null)
            {
                if (info.IsStatic == true)
                {
                    BatcherOptions = info.Invoke(null, new object[] { hlod }) as VisualElement;
                    BatcherOptions.style.marginLeft = 5;
                    BatcherFoldout.Add(BatcherOptions);
                }
            }
        }

        //public override void OnInspectorGUI()
        //{
        //    serializedObject.Update();
        //    EditorGUI.BeginChangeCheck();

        //    HLOD hlod = target as HLOD;
        //    if (hlod == null)
        //    {
        //        EditorGUILayout.LabelField("HLOD is null.");
        //        return;
        //    }
        //    if (m_splitter == null)
        //    {
        //        m_splitter = SpaceSplitterTypes.CreateInstance(hlod);
        //    }

        //    isShowCommon = EditorGUILayout.BeginFoldoutHeaderGroup(isShowCommon, "Common");
        //    if (isShowCommon == true)
        //    {
        //        EditorGUILayout.PropertyField(m_ChunkSizeProperty);

        //        m_ChunkSizeProperty.floatValue = HLODUtils.GetChunkSizePropertyValue(m_ChunkSizeProperty.floatValue);

        //        if (m_splitter != null)
        //        {
        //            var bounds = hlod.GetBounds();
        //            int depth = m_splitter.CalculateTreeDepth(bounds, m_ChunkSizeProperty.floatValue);

        //            EditorGUILayout.LabelField($"The HLOD tree will be created with {depth} levels.", Styles.BlueTextColor);
        //            if (depth > 5)
        //            {
        //                EditorGUILayout.LabelField($"Node Level Count greater than 5 may cause a frozen Editor.",
        //                    Styles.RedTextColor);
        //                EditorGUILayout.LabelField($"I recommend keeping the level under 5.", Styles.RedTextColor);

        //            }
        //        }

        //        //m_LODSlider.Draw();
        //        EditorGUILayout.PropertyField(m_MinObjectSizeProperty);
        //    }
        //    EditorGUILayout.EndFoldoutHeaderGroup();

        //    isShowSpaceSplitter = EditorGUILayout.BeginFoldoutHeaderGroup(isShowSpaceSplitter, "SpaceSplitter");
        //    if (isShowSpaceSplitter)
        //    {
        //        EditorGUI.indentLevel += 1;
        //        if (m_SpaceSplitterTypes.Length > 0)
        //        {
        //            EditorGUI.BeginChangeCheck();
                    
        //            int spaceSplitterIndex = Math.Max(Array.IndexOf(m_SpaceSplitterTypes, hlod.SpaceSplitterType), 0);
        //            spaceSplitterIndex = EditorGUILayout.Popup("SpaceSplitter", spaceSplitterIndex, m_SpaceSplitterNames);
        //            hlod.SpaceSplitterType = m_SpaceSplitterTypes[spaceSplitterIndex];

        //            var info = m_SpaceSplitterTypes[spaceSplitterIndex].GetMethod("OnGUI");
        //            if (info != null)
        //            {
        //                if ( info.IsStatic == true )
        //                {
        //                    info.Invoke(null, new object[] { hlod.SpaceSplitterOptions });
        //                }
        //            }

        //            if (EditorGUI.EndChangeCheck())
        //            {
        //                m_splitter = SpaceSplitterTypes.CreateInstance(hlod);
        //            }

        //            if (m_splitter != null)
        //            {
        //                int subTreeCount = m_splitter.CalculateSubTreeCount(hlod.GetBounds());
        //                EditorGUILayout.LabelField($"The HLOD tree will be created with {subTreeCount} sub trees.",
        //                    Styles.BlueTextColor);
        //            }

        //        }
        //        else
        //        {
        //            EditorGUILayout.LabelField("Cannot find SpaceSplitters.");
        //        }
        //        EditorGUI.indentLevel -= 1;
        //    }
        //    EditorGUILayout.EndFoldoutHeaderGroup();

        //    isShowSimplifier = EditorGUILayout.BeginFoldoutHeaderGroup(isShowSimplifier, "Simplifier");
        //    if (isShowSimplifier == true)
        //    {
        //        EditorGUI.indentLevel += 1;
        //        if (m_SimplifierTypes.Length > 0)
        //        {
        //            int simplifierIndex = Math.Max(Array.IndexOf(m_SimplifierTypes, hlod.SimplifierType), 0);
        //            simplifierIndex = EditorGUILayout.Popup("Simplifier", simplifierIndex, m_SimplifierNames);
        //            hlod.SimplifierType = m_SimplifierTypes[simplifierIndex];

        //            var info = m_SimplifierTypes[simplifierIndex].GetMethod("OnGUI");
        //            if (info != null)
        //            {
        //                if (info.IsStatic == true)
        //                {
        //                    info.Invoke(null, new object[] {hlod.SimplifierOptions});
        //                }
        //            }
        //        }
        //        else
        //        {
        //            EditorGUILayout.LabelField("Cannot find Simplifiers.");
        //        }
        //        EditorGUI.indentLevel -= 1;
        //    }
        //    EditorGUILayout.EndFoldoutHeaderGroup();

        //    isShowBatcher = EditorGUILayout.BeginFoldoutHeaderGroup(isShowBatcher, "Batcher");
        //    if (isShowBatcher == true)
        //    {
        //        EditorGUI.indentLevel += 1;
        //        if (m_BatcherTypes.Length > 0)
        //        {
        //            int batcherIndex = Math.Max(Array.IndexOf(m_BatcherTypes, hlod.BatcherType), 0);
        //            batcherIndex = EditorGUILayout.Popup("Batcher", batcherIndex, m_BatcherNames);
        //            hlod.BatcherType = m_BatcherTypes[batcherIndex];

        //            var info = m_BatcherTypes[batcherIndex].GetMethod("OnGUI");
        //            if (info != null)
        //            {
        //                if (info.IsStatic == true)
        //                {
        //                    info.Invoke(null, new object[] {hlod, isFirstOnGUI });
        //                }
        //            }
        //        }
        //        else
        //        {
        //            EditorGUILayout.LabelField("Cannot find Batchers.");
        //        }
        //        EditorGUI.indentLevel -= 1;
        //    }
        //    EditorGUILayout.EndFoldoutHeaderGroup();
            

        //    isShowStreaming = EditorGUILayout.BeginFoldoutHeaderGroup(isShowStreaming, "Streaming");
        //    if (isShowStreaming == true)
        //    {
        //        EditorGUI.indentLevel += 1;
        //        if (m_StreamingTypes.Length > 0)
        //        {
        //            int streamingIndex = Math.Max(Array.IndexOf(m_StreamingTypes, hlod.StreamingType), 0);
        //            streamingIndex = EditorGUILayout.Popup("Streaming", streamingIndex, m_StreamingNames);
        //            hlod.StreamingType = m_StreamingTypes[streamingIndex];

        //            var info = m_StreamingTypes[streamingIndex].GetMethod("OnGUI");
        //            if (info != null)
        //            {
        //                if (info.IsStatic == true)
        //                {
        //                    info.Invoke(null, new object[] { hlod.StreamingOptions });
        //                }
        //            }
        //        }
        //        else
        //        {
        //            EditorGUILayout.LabelField("Cannot find StreamingSetters.");
        //        }
        //        EditorGUI.indentLevel -= 1;
        //    }
        //    EditorGUILayout.EndFoldoutHeaderGroup();
            
            
        //    isShowUserDataSerializer =
        //        EditorGUILayout.BeginFoldoutHeaderGroup(isShowUserDataSerializer, "UserData serializer");
        //    if (isShowUserDataSerializer)
        //    {
        //        EditorGUI.indentLevel += 1;
        //        if (m_UserDataSerializerTypes.Length > 0)
        //        {
        //            int serializerIndex =
        //                Math.Max(Array.IndexOf(m_UserDataSerializerTypes, hlod.UserDataSerializerType), 0);
        //            serializerIndex =
        //                EditorGUILayout.Popup("UserDataSerializer", serializerIndex, m_UserDataSerializerNames);
        //            hlod.UserDataSerializerType = m_UserDataSerializerTypes[serializerIndex];
        //        }
        //        else
        //        {
        //            EditorGUILayout.LabelField("Cannot find UserDataSerializer.");
        //        }
        //        EditorGUI.indentLevel -= 1;
        //    }
        //    EditorGUILayout.EndFoldoutHeaderGroup();


        //    GUIContent generateButton = Styles.GenerateButtonEnable;
        //    GUIContent destroyButton = Styles.DestroyButtonNotExists;

        //    if (hlod.GeneratedObjects.Count > 0 )
        //    {
        //        generateButton = Styles.RegenerateButtonEnable;
        //        destroyButton = Styles.DestroyButtonEnable;
        //    }

        //    EditorGUILayout.Space();

        //    if (generateButton == Styles.GenerateButtonEnable)
        //    {
        //        if (GUILayout.Button(generateButton))
        //        {
        //            CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
        //        }
        //    }
        //    else
        //    {
        //        if (generateButton == Styles.RegenerateButtonEnable)
        //        {
        //            if (GUILayout.Button(generateButton))
        //            {
        //                CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
        //                CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
        //            }
        //        }
        //    }

        //    GUI.enabled = destroyButton == Styles.DestroyButtonEnable;
        //    if (GUILayout.Button(destroyButton))
        //    {
        //        CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
        //    }
            
        //    if (EditorGUI.EndChangeCheck())
        //    {
        //        EditorUtility.SetDirty(hlod);
        //    }

        //    GUI.enabled = true;

            
        //    serializedObject.ApplyModifiedProperties();
        //    isFirstOnGUI = false;
        //}

    }

}