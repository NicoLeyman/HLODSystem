using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using static Unity.HLODSystem.GUIUtils;

namespace Unity.HLODSystem
{
    public static class GUIUtils
    {
        public static void DrawHorizontalGUILine(int offsetLeft = 0, int height = 1)
        {
            GUILayout.Space(4);

            Rect rect = GUILayoutUtility.GetRect(10, height, GUILayout.ExpandWidth(true));
            rect.height = height;
            rect.xMin = offsetLeft;
            rect.xMax = EditorGUIUtility.currentViewWidth;

            Color lineColor = new Color(0.10196f, 0.10196f, 0.10196f, 1);
            EditorGUI.DrawRect(rect, lineColor);
            GUILayout.Space(4);
        }

        public static string StringPopup(string select, string[] options)
        {
            if (options == null || options.Length == 0)
            {
                EditorGUILayout.Popup(0, new string[] { select });
                return select;
            }

            int index = Array.IndexOf(options, select);
            if (index < 0)
                index = 0;

            int selected = EditorGUILayout.Popup(index, options);
            return options[selected];
        }

        public static string DynamicAssetPropertyGUI<T>(string label, string serializedAssetGUID, T defaultValue) where T : UnityEngine.Object
        {
            string path = "";
            T asset = null;
            if (string.IsNullOrEmpty(serializedAssetGUID) == false)
            {
                path = AssetDatabase.GUIDToAssetPath(serializedAssetGUID);
                asset = AssetDatabase.LoadAssetAtPath<T>(path);
            }
            asset = EditorGUILayout.ObjectField(label, asset, typeof(T), false) as T;
            if (asset == null)
                asset = defaultValue;

            path = AssetDatabase.GetAssetPath(asset);
            return AssetDatabase.AssetPathToGUID(path);
        }

        public class OverridablePropertyElement : VisualElement
        {
            PropertyField OverrideField;
            PropertyField ValueField;
            Label ResolvedValueLabel;
            Func<bool, string> ResolvedValueLabelOnOverrideChange;

            public OverridablePropertyElement(SerializedObject serializedObject, string valueName, Func<bool, string> resolvedLabelOnOverrideChange, string label, string tooltip)
            {
                this.tooltip = tooltip;
                style.flexDirection = FlexDirection.Row;
                Add(new Label() { text = valueName });
                // Create drawer UI using C#.
                var overrideProperty = serializedObject.FindProperty($"Override{valueName}");
                OverrideField = new PropertyField(overrideProperty, "");
                Add(OverrideField);
                ValueField = new PropertyField(serializedObject.FindProperty(valueName), "");
                Add(ValueField);

                ResolvedValueLabel = new Label();
                Add(ResolvedValueLabel);
                ResolvedValueLabelOnOverrideChange = resolvedLabelOnOverrideChange;

                OverrideField.RegisterValueChangeCallback((e) =>
                {
                    OnOverrideToggled(e.changedProperty.boolValue);
                });

                OnOverrideToggled(overrideProperty.boolValue);
            }

            void OnOverrideToggled(bool newValue)
            {
                ValueField.enabledSelf = newValue;
                ResolvedValueLabel.visible = !newValue;

                if (ResolvedValueLabelOnOverrideChange != null)
                {
                    ResolvedValueLabel.text = ResolvedValueLabelOnOverrideChange.Invoke(newValue);
                }
            }
        }
        
       
    }
}
