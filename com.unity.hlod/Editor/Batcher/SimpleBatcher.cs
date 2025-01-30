using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem
{
    public class SimpleBatcher : IBatcher
    {

        [InitializeOnLoadMethod]
        static void RegisterType()
        {
            BatcherTypes.RegisterBatcherType(typeof(SimpleBatcher));
        }

        private DisposableDictionary<TexturePacker.TextureAtlas, WorkingMaterial> m_createdMaterials = new DisposableDictionary<TexturePacker.TextureAtlas, WorkingMaterial>();
        private SerializableDynamicObject m_batcherOptions;

        public SimpleBatcher(SerializableDynamicObject batcherOptions)
        {
            m_batcherOptions = batcherOptions;
        }

        public void Dispose()
        {
            m_createdMaterials.Dispose();
        }

        public void Batch(Transform rootTransform, DisposableList<HLODBuildInfo> targets, Action<float> onProgress)
        {
            dynamic options = m_batcherOptions;
            if (onProgress != null)
                onProgress(0.0f);

            using (TexturePacker packer = new TexturePacker())
            {
                PackingTexture(packer, targets, options, onProgress);

                for (int i = 0; i < targets.Count; ++i)
                {
                    Combine(rootTransform, packer, targets[i], options);
                    if (onProgress != null)
                        onProgress(0.5f + ((float)i / (float)targets.Count) * 0.5f);
                }
            }

        }


        class MaterialTextureCache : IDisposable
        {
            private NativeArray<int> m_detector = new NativeArray<int>(1, Allocator.Persistent);
            
            private List<TextureInfo> m_textureInfoList;
            private DisposableDictionary<string, TexturePacker.MaterialTexture> m_textureCache;
            private DisposableDictionary<PackingType, WorkingTexture> m_defaultTextures;
                
            private bool m_enableTintColor;
            private string m_tintColorName;
            private string m_outputTextureToTintName;
            
            public MaterialTextureCache(MaterialMapping mapping)
            {
                m_defaultTextures = CreateDefaultTextures();
                m_enableTintColor = mapping.EnableTintColor;
                m_tintColorName = mapping.TintColorName;
                m_outputTextureToTintName = mapping.OutputTexturePropertyToTint;
                m_textureInfoList = mapping.TextureInfoList;
                m_textureCache = new DisposableDictionary<string, TexturePacker.MaterialTexture>();
            }
            public TexturePacker.MaterialTexture GetMaterialTextures(WorkingMaterial material)
            {
                if (m_textureCache.ContainsKey(material.Guid) == false)
                {
                    AddToCache(material);
                }

                if (m_textureCache.TryGetValue(material.Guid, out var textures))
                {
                    foreach (var inputName in m_textureInfoList[0].InputNames)
                    {
                        material.SetTexture(inputName, textures[0].Clone());
                    }
                }

                return textures;
            }

            public void Dispose()
            {
                m_textureCache.Dispose();
                m_defaultTextures.Dispose();
                m_detector.Dispose();
                
            }

            private void AddToCache(WorkingMaterial material)
            {
                if (m_textureInfoList.Count == 0)
                    return;
                
                TexturePacker.MaterialTexture materialTexture = new TexturePacker.MaterialTexture();
                

                for (int ti = 0; ti < m_textureInfoList.Count; ++ti)
                {
                    var textureInfo = m_textureInfoList[ti];

                    WorkingTexture tex = null;

                    for (var inputIdx = 0; inputIdx < textureInfo.InputNames.Count; ++inputIdx)
                    {
                        tex = material.GetTexture(textureInfo.InputNames[inputIdx]);

                        if (tex != null)
                            break;
                    }

                    if (tex == null)
                    {
                        tex = m_defaultTextures[textureInfo.Type];
                    }

                    // TODO: Allow color tint per layer input.
                    if (m_enableTintColor && textureInfo.OutputName == m_outputTextureToTintName)
                    {
                        Color tintColor = material.GetColor(m_tintColorName);

                        tex = tex.Clone();
                        ApplyTintColor(tex, tintColor);
                        materialTexture.Add(tex, true);
                    }
                    else
                    {
                        materialTexture.Add(tex);
                    }
                }

                m_textureCache.Add(material.Guid, materialTexture);
            }
            private void ApplyTintColor(WorkingTexture texture, Color tintColor)
            {
                for (int ty = 0; ty < texture.Height; ++ty)
                {
                    for (int tx = 0; tx < texture.Width; ++tx)
                    {
                        Color c = texture.GetPixel(tx, ty);
                    
                        c.r = c.r * tintColor.r;
                        c.g = c.g * tintColor.g;
                        c.b = c.b * tintColor.b;
                        c.a = c.a * tintColor.a;
                    
                        texture.SetPixel(tx, ty, c);
                    }
                }
            }

            private static DisposableDictionary<PackingType, WorkingTexture> CreateDefaultTextures()
            {
                DisposableDictionary<PackingType, WorkingTexture> textures = new DisposableDictionary<PackingType, WorkingTexture>();

                textures.Add(PackingType.White, CreateEmptyTexture(4, 4, Color.white, false));
                textures.Add(PackingType.Black, CreateEmptyTexture(4, 4, Color.black, false));
                textures.Add(PackingType.Normal, CreateEmptyTexture(4, 4, new Color(0.5f, 0.5f, 1.0f), true, true));

                return textures;
            }

        }

        private void PackingTexture(TexturePacker packer, DisposableList<HLODBuildInfo> targets, dynamic options, Action<float> onProgress)
        { 
            MaterialMapping materialMapping = options.MaterialMapping;
            // Resolve material mapping
            if (materialMapping == null)
            {
                materialMapping = HLODEditorSettings.DefaultMaterialMapping;
            }
            List<TextureInfo> textureInfoList = materialMapping.TextureInfoList;
            using (MaterialTextureCache cache = new MaterialTextureCache(materialMapping))
            {
                for (int i = 0; i < targets.Count; ++i)
                {
                    var workingObjects = targets[i].WorkingObjects;
                    Dictionary<Guid, TexturePacker.MaterialTexture> textures =
                        new Dictionary<Guid, TexturePacker.MaterialTexture>();

                    for (int oi = 0; oi < workingObjects.Count; ++oi)
                    {
                        var materials = workingObjects[oi].Materials;

                        for (int m = 0; m < materials.Count; ++m)
                        {
                            var materialTextures = cache.GetMaterialTextures(materials[m]);
                            if (materialTextures == null)
                                continue;

                            if (textures.ContainsKey(materialTextures[0].GetGUID()) == true)
                                continue;

                            textures.Add(materialTextures[0].GetGUID(), materialTextures);
                        }
                    }


                    packer.AddTextureGroup(targets[i], textures.Values.ToList());


                    if (onProgress != null)
                        onProgress(((float) i / targets.Count) * 0.1f);
                }
            }

            packer.Pack(TextureFormat.RGBA32, options.PackTextureSize, options.LimitTextureSize, false);
            if ( onProgress != null) onProgress(0.3f);

            int index = 1;
            var atlases = packer.GetAllAtlases();
            foreach (var atlas in atlases)
            {
                Dictionary<string, WorkingTexture> textures = new Dictionary<string, WorkingTexture>();
                for (int i = 0; i < atlas.Textures.Count; ++i)
                {
                    WorkingTexture wt = atlas.Textures[i];
                    wt.Name = "CombinedTexture " + index + "_" + i;
                    if (textureInfoList[i].Type == PackingType.Normal)
                    {
                        wt.Linear = true;
                        wt.IsNormal = true;
                    }

                    if(!textures.TryAdd(textureInfoList[i].OutputName, wt))
                    {
                        Debug.Log(textureInfoList[i].OutputName);
                    }
                }

                WorkingMaterial mat = CreateMaterial(options.MaterialGUID, textures);
                mat.Name = "CombinedMaterial " + index;
                m_createdMaterials.Add(atlas, mat);
                index += 1;
            }
        }

        static WorkingMaterial CreateMaterial(string guidstr, Dictionary<string, WorkingTexture> textures)
        {
            WorkingMaterial material = null;
            string path = AssetDatabase.GUIDToAssetPath(guidstr);
            if (string.IsNullOrEmpty(path) == false)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    material = new WorkingMaterial(Allocator.Invalid, mat.GetInstanceID(), mat.name);
                }
            }

            if (material == null)
            {
                material = new WorkingMaterial(Allocator.Persistent, new Material(GraphicsUtils.GetDefaultShader()));
            }
            
            foreach (var texture in textures)
            {
                material.AddTexture(texture.Key, texture.Value.Clone());
            }
            
            return material;
        }

        private void Combine(Transform rootTransform, TexturePacker packer, HLODBuildInfo info, dynamic options)
        {
            var atlas = packer.GetAtlas(info);
            if (atlas == null)
                return;

            MaterialMapping materialMapping = options.MaterialMapping;
            // Resolve material mapping
            if (materialMapping == null)
            {
                materialMapping = HLODEditorSettings.DefaultMaterialMapping;
            }

            List<MeshCombiner.CombineInfo> combineInfos = new List<MeshCombiner.CombineInfo>();
            var hlodWorldToLocal = rootTransform.worldToLocalMatrix;

            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                var obj = info.WorkingObjects[i];
                if (obj.Mesh == null)
                    continue;

                ConvertMesh(obj.Mesh, obj.Materials, atlas, materialMapping.TextureInfoList[0].InputNames);

                for (int si = 0; si < obj.Mesh.subMeshCount; ++si)
                {
                    var ci = new MeshCombiner.CombineInfo();
                    var colliderLocalToWorld = obj.LocalToWorld;
                    var matrix = hlodWorldToLocal * colliderLocalToWorld;
                    
                    ci.Mesh = obj.Mesh;
                    ci.MeshIndex = si;
                    
                    ci.Transform = matrix;

                    if (ci.Mesh == null)
                        continue;
                    
                    combineInfos.Add(ci);
                }
            }
            
            MeshCombiner combiner = new MeshCombiner();
            WorkingMesh combinedMesh = combiner.CombineMesh(Allocator.Persistent, combineInfos);

            WorkingObject newObj = new WorkingObject(Allocator.Persistent);
            WorkingMaterial newMat = m_createdMaterials[atlas].Clone();

            combinedMesh.name = info.Name + "_Mesh";
            newObj.Name = info.Name;
            newObj.SetMesh(combinedMesh);
            newObj.Materials.Add(newMat);

            info.WorkingObjects.Dispose();
            info.WorkingObjects = new DisposableList<WorkingObject>();
            info.WorkingObjects.Add(newObj);
        }


        private void ConvertMesh(WorkingMesh mesh, DisposableList<WorkingMaterial> materials, TexturePacker.TextureAtlas atlas, IList<string> inputTexturePropertyNames)
        {
            var uv1 = mesh.uv1;
            var uv2 = mesh.uv2;
            var uv3 = mesh.uv3;
            var uv4 = mesh.uv4;

            var updated = new bool[uv1.Length];
            // Some meshes have submeshes that either aren't expected to render or are missing a material, so go ahead and skip
            int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Count);
            for (int mi = 0; mi < subMeshCount; ++mi)
            {
                int[] indices = mesh.GetTriangles(mi);
                foreach (var i in indices)
                {
                    if ( updated[i] == false )
                    {
                        var uvCoord1 = uv1[i];
                        var uvCoord2 = uv2.Length > 0 ? uv2[i] : Vector2.zero;
                        var uvCoord3 = uv3.Length > 0 ? uv3[i] : Vector2.zero;
                        var uvCoord4 = uv4.Length > 0 ? uv4[i] : Vector2.zero;
                        WorkingTexture texture = null;

                        foreach (var texturePropertyName in inputTexturePropertyNames)
                        {
                            texture = materials[mi].GetTexture(texturePropertyName);
                            if (texture != null)
                                break;
                        }
                        
                        if (texture == null || texture.GetGUID() == Guid.Empty)
                        {
                            // Sample at center of white texture to avoid sampling edge colors incorrectly
                            uvCoord1.x = 0.5f;
                            uvCoord1.y = 0.5f;
                            uvCoord2 = uvCoord1;
                            uvCoord3 = uvCoord1;
                            uvCoord4 = uvCoord1;
                        }
                        else
                        {
                            var uvOffset = atlas.GetUV(texture.GetGUID());
                            
                            // TODO: for tiling textures (UVs outside the 0-1 range):
                            // - Split the geometry into chunks with normalized UV coordinates before combining/atlassing the geometry.
                            // -----> Likely to increase geometry density which we want to avoid.
                            // - Normalize the UVs for all meshes sharing the same atlas space based on the one with the largest UV requirements.
                            // - Bake the tiling into the atlas space.
                            // -----> Reduces texel density. The quality loss may not be too noticeable in most cases and comes at no perf cost.
                            // - Reserve multiple atlas items to either componsate for the texel density loss or to accommodate the UV requirements
                            // -----> Unlikely to be able to meet all UV requirements.
                            // -----> Uses a lot of atlas space to compensate.
                            // -----> Please don't make me figure out how to play the atlas item tetris game. Q.Q
                            uvCoord1.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord1.x % 1);
                            uvCoord1.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord1.y % 1);

                            uvCoord2.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord2.x % 1);
                            uvCoord2.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord2.y % 1);

                            uvCoord3.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord3.x % 1);
                            uvCoord3.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord3.y % 1);

                            uvCoord4.x = Mathf.Lerp(uvOffset.xMin, uvOffset.xMax, uvCoord4.x % 1);
                            uvCoord4.y = Mathf.Lerp(uvOffset.yMin, uvOffset.yMax, uvCoord4.y % 1);
                        }
                        
                        uv1[i] = uvCoord1;
                        if(uv2.Length > 0)
                            uv2[i] = uvCoord2;
                        if (uv3.Length > 0)
                            uv3[i] = uvCoord3;
                        if (uv4.Length > 0)
                            uv4[i] = uvCoord4;

                        updated[i] = true;
                    }
                }
                
            }

            mesh.uv1 = uv1;
            mesh.uv2 = uv2;
            mesh.uv3 = uv3;
            mesh.uv4 = uv4;
        }

        static private WorkingTexture CreateEmptyTexture(int width, int height, Color color, bool linear, bool isNormal = false)
        {
            WorkingTexture texture = new WorkingTexture(Allocator.Persistent, TextureFormat.RGB24, width, height, linear);
            texture.IsNormal = isNormal;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            return texture;
        }
        
        static class Styles
        {
            public static int[] PackTextureSizes = new int[]
            {
                256, 512, 1024, 2048, 4096
            };
            public static string[] PackTextureSizeNames;

            public static int[] LimitTextureSizes = new int[]
            {
                32, 64, 128, 256, 512, 1024
            };
            public static string[] LimitTextureSizeNames;

            static Styles()
            {
                PackTextureSizeNames = new string[PackTextureSizes.Length];
                for (int i = 0; i < PackTextureSizes.Length; ++i)
                {
                    PackTextureSizeNames[i] = PackTextureSizes[i].ToString();
                }

                LimitTextureSizeNames = new string[LimitTextureSizes.Length];
                for (int i = 0; i < LimitTextureSizes.Length; ++i)
                {
                    LimitTextureSizeNames[i] = LimitTextureSizes[i].ToString();
                }
            }
        }
        
        private static TextureInfo addingTextureInfo = new TextureInfo();
        public static void OnGUI(HLOD hlod, bool isFirst)
        {
            EditorGUI.indentLevel += 1;
            dynamic batcherOptions = hlod.BatcherOptions;

            // UI only
            if (batcherOptions.textureSlotFoldout == null)
                batcherOptions.textureSlotFoldout = false;

            if (batcherOptions.PackTextureSize == null)
                batcherOptions.PackTextureSize = 1024;
            if (batcherOptions.LimitTextureSize == null)
                batcherOptions.LimitTextureSize = 128;
            if (batcherOptions.MaterialGUID == null)
                batcherOptions.MaterialGUID = "";
            //if (batcherOptions.TextureInfoList == null)
            //{
            //    batcherOptions.TextureInfoList = new List<TextureInfo>(){
            //        new TextureInfo()
            //    {
            //        InputNames = { "_MainTex" },
            //        OutputName = "_MainTex",
            //        Type = PackingType.White
            //    },
            //    new TextureInfo()
            //    {
            //        InputNames = { "_BumpMap", "_NormalMap" },
            //        OutputName = "_NormalMap",
            //        Type = PackingType.Normal
            //    },
            //    new TextureInfo()
            //    {
            //        InputNames = { "_MaskMap"},
            //        OutputName = "_MaskMap",
            //        Type = PackingType.Black
            //    } };
            //}

            batcherOptions.PackTextureSize = EditorGUILayout.IntPopup("Pack texture size", batcherOptions.PackTextureSize, Styles.PackTextureSizeNames, Styles.PackTextureSizes);
            batcherOptions.LimitTextureSize = EditorGUILayout.IntPopup("Limit texture size", batcherOptions.LimitTextureSize, Styles.LimitTextureSizeNames, Styles.LimitTextureSizes);

            Material mat = null;

            string matGUID = batcherOptions.MaterialGUID;
            string path = "";
            if (string.IsNullOrEmpty(matGUID) == false)
            {
                path = AssetDatabase.GUIDToAssetPath(matGUID);
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            mat = EditorGUILayout.ObjectField("Material", mat, typeof(Material), false) as Material;
            if( mat == null)
                mat = new Material(GraphicsUtils.GetDefaultShader());
            
            path = AssetDatabase.GetAssetPath(mat);
            matGUID = AssetDatabase.AssetPathToGUID(path);


            EditorGUILayout.BeginHorizontal();
            MaterialMapping materialMapping = batcherOptions.MaterialMapping;

            if (batcherOptions.FoldoutMapping == null)
                batcherOptions.FoldoutMapping = false;

            batcherOptions.FoldoutMapping = EditorGUILayout.Foldout((bool)batcherOptions.FoldoutMapping, "Material Mapping");
            batcherOptions.MaterialMappingGUID = GUIUtils.DynamicAssetPropertyGUI<MaterialMapping>(null, batcherOptions.MaterialMappingGUID, null);
            batcherOptions.MaterialMapping = materialMapping;
            
            // Resolve material mapping
            if (materialMapping == null)
            {
                materialMapping = HLODEditorSettings.DefaultMaterialMapping;
            }

            EditorGUILayout.EndHorizontal();

            if (batcherOptions.FoldoutMapping != false)
            {
                EditorGUILayout.Space(2.5f);
                if (materialMapping == null)
                {
                    EditorGUILayout.HelpBox("Both this component's Material Mapping and the default are set to null.\nPlease assign a Material Mapping object to either this component or Preferences/HLOD/Default Material Mapping", MessageType.Error);
                }
                else
                {
                    bool textureSlotFoldout = batcherOptions.textureSlotFoldout;
                    materialMapping.DrawGUI(hlod, ref textureSlotFoldout);
                    batcherOptions.textureSlotFoldout = textureSlotFoldout;
                }
            }

            EditorGUI.indentLevel -= 1;
            EditorGUI.indentLevel -= 1;
        }
    }

}
