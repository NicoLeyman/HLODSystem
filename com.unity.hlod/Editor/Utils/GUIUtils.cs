using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

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
    }
}
