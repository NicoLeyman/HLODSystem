using System;
using System.Collections.Generic;
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
            public static GUIContent OnlyIncludeHierarchy = new GUIContent("Only Include Hierarchy", "If true the HLOD baker will only include GameObjects nested under this GameObject. Otherwise it will include all GameObjects in the scene.");
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
    }

}
