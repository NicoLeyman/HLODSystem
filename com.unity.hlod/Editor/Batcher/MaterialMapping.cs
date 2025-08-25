using System.Collections.Generic;
using UnityEngine;
using System;

namespace Unity.HLODSystem
{
    [CreateAssetMenu(fileName = "MaterialMapping", menuName = "HLOD/Material Mapping")]
    public class MaterialMapping : ScriptableObject
    {
        public Shader Shader;
        public string ShaderGUID = "";
        public string OutputTexturePropertyToTint = "_Color";
        public List<TextureInfo> TextureInfoList = new (){ };
    }
    
    [Serializable]
    public class TextureInfo
    {
        public List<string> InputTexturePropertyNames = new List<string>() { };
        public List<string> InputColorPropertyNames = new List<string>() { };
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
