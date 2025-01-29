using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

namespace Unity.HLODSystem
{
    [CreateAssetMenu(fileName = "MaterialMapping", menuName = "HLOD/Material Mapping")]
    public class MaterialMapping : ScriptableObject
    {
        public Shader Shader;
        [SerializeField] 
        private string ShaderGUID = "";
        public bool EnableTintColor = true;
        public string TintColorName = "_Color";
        public List<TextureInfo> TextureInfoList = new (){ };
        
        [NonSerialized]
        private string[] inputTexturePropertyNames = null;
        [NonSerialized]
        private string[] outputTexturePropertyNames = null;
        [NonSerialized]
        HLOD lastHLODReferenced = null;

        static string[] GetTexturePropertyNames(Shader shader)
        {
            var mat = new Material(shader);
            return mat.GetTexturePropertyNames();
        }

        static string[] GetAllMaterialTextureProperties(GameObject root)
        {
            var meshRenderers = root.GetComponentsInChildren<MeshRenderer>();
            HashSet<string> uniquePropertyNames = new HashSet<string>();
            for (int m = 0; m < meshRenderers.Length; ++m)
            {
                var mesh = meshRenderers[m];
                foreach (Material material in mesh.sharedMaterials)
                {
                    if (material == null)
                        continue;

                    var names = material.GetTexturePropertyNames();
                    for (int n = 0; n < names.Length; ++n)
                    {
                        uniquePropertyNames.Add(names[n]);
                    }    
                }
                
            }

            var propertyNames = new string[uniquePropertyNames.Count];
            var i = 0;
            foreach (var name in uniquePropertyNames)
            {
                propertyNames[i] = (name);
                i++;
            }

            return propertyNames;
        }
        
        public void DrawGUI(HLOD hlod, ref bool textureSlotFoldout)
        {
            Shader = (Shader)EditorGUILayout.ObjectField(new GUIContent("Shader", "A value of null refers to the current render pipeline's default shader."), Shader, typeof(Shader), false);
                
            var resolvedShader = Shader != null ? Shader : Utils.GraphicsUtils.GetDefaultShader();

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(resolvedShader, out string shaderGUID,
                    out long localShaderID) && ShaderGUID != shaderGUID)
            {
                ShaderGUID = shaderGUID;
                outputTexturePropertyNames = GetTexturePropertyNames(resolvedShader);
            }

            if (hlod != lastHLODReferenced)
            {
                inputTexturePropertyNames = null;
                lastHLODReferenced = hlod;
            }

            if (inputTexturePropertyNames == null && hlod != null)
            {
                inputTexturePropertyNames = GetAllMaterialTextureProperties(hlod.gameObject);
            }
            if (outputTexturePropertyNames == null)
            {
                outputTexturePropertyNames = GetTexturePropertyNames(resolvedShader);
            }
            
            //apply tint color
            EnableTintColor =
                EditorGUILayout.Toggle("Enable tint color", EnableTintColor);
            if (EnableTintColor == true)
            {
                EditorGUI.indentLevel += 1;
                
                List<string> colorPropertyNames = new List<string>();
                int propertyCount = ShaderUtil.GetPropertyCount(resolvedShader);
                for (int i = 0; i < propertyCount; ++i)
                {
                    string name = ShaderUtil.GetPropertyName(resolvedShader, i);
                    if (ShaderUtil.GetPropertyType(resolvedShader, i) == ShaderUtil.ShaderPropertyType.Color)
                    {
                        colorPropertyNames.Add(name);
                    }
                }

                int index = colorPropertyNames.IndexOf(TintColorName);
                index = EditorGUILayout.Popup("Tint color property", index, colorPropertyNames.ToArray());
                if (index >= 0)
                {
                    TintColorName = colorPropertyNames[index];
                }
                else
                {
                    TintColorName = "";
                }
                
                EditorGUI.indentLevel -= 1;
            }
            
            EditorGUILayout.Space();
            textureSlotFoldout = EditorGUILayout.Foldout(textureSlotFoldout, "Textures");
            if (textureSlotFoldout)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.SelectableLabel("Output");
                EditorGUILayout.SelectableLabel("Inputs");
                EditorGUILayout.SelectableLabel("Default Color");
                EditorGUILayout.EndHorizontal();

                for (int infoIdx = 0; infoIdx < TextureInfoList.Count; ++infoIdx)
                {
                    TextureInfo info = TextureInfoList[infoIdx];

                    EditorGUILayout.BeginVertical();

                    EditorGUILayout.BeginHorizontal();
                    info.OutputName = GUIUtils.StringPopup(info.OutputName, outputTexturePropertyNames);

                    info.Type = (PackingType)EditorGUILayout.EnumPopup(info.Type);

                    GUILayout.Space(20);
                    if (GUILayout.Button("x", GUILayout.Width(20)) == true)
                    {
                        TextureInfoList.RemoveAt(infoIdx);
                        infoIdx -= 1;
                    }

                    EditorGUILayout.EndHorizontal();

                    for (var inputIdx = 0; inputIdx < info.InputNames.Count; ++inputIdx)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel(" ");
                        if (inputTexturePropertyNames == null)
                        {
                            info.InputNames[inputIdx] = EditorGUILayout.TextField(info.InputNames[inputIdx]);
                        }
                        else
                        {
                            info.InputNames[inputIdx] = GUIUtils.StringPopup(info.InputNames[inputIdx], inputTexturePropertyNames);
                        }

                        if (info.InputNames.Count <= 1)
                            GUI.enabled = false;
                        if (GUILayout.Button("x", GUILayout.Width(20)) == true)
                        {
                            info.InputNames.RemoveAt(inputIdx);
                            inputIdx -= 1;
                        }
                        GUI.enabled = true;

                        if (inputIdx != info.InputNames.Count - 1)
                            GUI.enabled = false;
                        if (GUILayout.Button("+", GUILayout.Width(20)) == true)
                        {
                            var defaultName = inputTexturePropertyNames != null ? inputTexturePropertyNames[0] : "";
                            info.InputNames.Add(defaultName);
                        }
                        GUI.enabled = true;

                        EditorGUILayout.EndHorizontal();
                    }

                    GUIUtils.DrawHorizontalGUILine(60);
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(" ");
                if (GUILayout.Button("Add new texture property") == true)
                {
                    TextureInfoList.Add(new TextureInfo());
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(" ");
                if (GUILayout.Button("Update texture properties"))
                {
                    //TODO: Need update automatically
                    inputTexturePropertyNames = null;
                    outputTexturePropertyNames = null;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel -= 1;
            }
        }
    }
    
    [Serializable]
    public class TextureInfo
    {
        public List<string> InputNames = new List<string>() { "_InputProperty" };
        public string OutputName = "_OutputProperty";
        public PackingType Type = PackingType.White;
    }
    
    public enum PackingType
    {
        White,
        Black,
        Normal,
    }
}
