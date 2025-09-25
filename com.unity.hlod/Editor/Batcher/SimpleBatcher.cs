using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.HLODSystem.Utils;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

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

        public static void InitializeOptions(dynamic options)
        {
            if (options.textureSlotFoldout == null)
                options.textureSlotFoldout = false;

            if (options.PackTextureSize == null)
                options.PackTextureSize = 1024;
            if (options.LimitTextureSize == null)
                options.LimitTextureSize = 128;
            if (options.MaterialGUID == null)
                options.MaterialGUID = "";
            if (options.AllowAlphaClipping == null)
                options.AllowAlphaClipping = true;
        }

        public void Batch(Transform rootTransform, DisposableList<HLODBuildInfo> targets, Action<float> onProgress)
        {
            InitializeOptions(m_batcherOptions);

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
                
            private string m_outputTextureToTintName;
            private bool m_shouldUseTransparency;

            public bool ShouldUseTransparency => m_shouldUseTransparency;
            
            public MaterialTextureCache(MaterialMapping mapping)
            {
                m_defaultTextures = CreateDefaultTextures();
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
                    foreach (var inputName in m_textureInfoList[0].InputTexturePropertyNames)
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

                    for (var inputIdx = 0; inputIdx < textureInfo.InputTexturePropertyNames.Count; ++inputIdx)
                    {
                        tex = material.GetTexture(textureInfo.InputTexturePropertyNames[inputIdx]);

                        if (tex != null)
                            break;
                    }

                    if (tex == null)
                    {
                        tex = m_defaultTextures[textureInfo.Type];
                    }

                    bool takeTextureOwnership = false;
                    for (var inputIdx = 0; inputIdx < textureInfo.InputColorPropertyNames.Count; ++inputIdx)
                    {
                        var colorName = textureInfo.InputColorPropertyNames[inputIdx];
                        if (material.HasColor(colorName))
                        {
                            var tintColor = material.GetColor(colorName);
                            tex = tex.Clone();
                            ApplyTintColor(tex, tintColor);
                            takeTextureOwnership = true;
                            break;
                        }
                    }

                    materialTexture.Add(tex, takeTextureOwnership);
                }

                m_textureCache.Add(material.Guid, materialTexture);

                m_shouldUseTransparency |= material.EnableAlphaClipping;
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
            string materialMappingGUID = options.MaterialMappingGUID;
            MaterialMapping materialMapping;

            if (string.IsNullOrEmpty(materialMappingGUID))
            {
                materialMapping = HLODEditorSettings.Instance.DefaultMaterialMapping;
            }
            else
            {
                var materialMappingPath = AssetDatabase.GUIDToAssetPath(materialMappingGUID);
                materialMapping = AssetDatabase.LoadAssetAtPath<MaterialMapping>(materialMappingPath);
            }

            bool sourceMaterialsUseTransparency = false;

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

                sourceMaterialsUseTransparency |= cache.ShouldUseTransparency;
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

                Material mat = null;

                string matGUID = options.MaterialGUID;
                string path = "";
                if (string.IsNullOrEmpty(matGUID) == false)
                {
                    path = AssetDatabase.GUIDToAssetPath(matGUID);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                else
                {
                    var shader = materialMapping.Shader;
                    if (shader == null)
                        shader = GraphicsUtils.GetDefaultShader();

                    mat = new Material(shader);
                }

                WorkingMaterial wm = CreateMaterial(options.MaterialGUID, textures, (bool)options.AllowAlphaClipping && sourceMaterialsUseTransparency, mat);
                wm.Name = "CombinedMaterial " + index;
                m_createdMaterials.Add(atlas, wm);
                index += 1;
            }
        }

        static WorkingMaterial CreateMaterial(string guidstr, Dictionary<string, WorkingTexture> textures, bool enableAlphaClipping, Material mat)
        {
            WorkingMaterial material = null;
            string path = AssetDatabase.GUIDToAssetPath(guidstr);
            if (string.IsNullOrEmpty(path) == false)
            {
                mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    material = new WorkingMaterial(Allocator.Invalid, mat.GetInstanceID(), mat.name);
                }
            }

            if (material == null)
            {
                material = new WorkingMaterial(Allocator.Persistent, mat);
            }
            
            foreach (var texture in textures)
            {
                material.AddTexture(texture.Key, texture.Value.Clone());
            }

            material.EnableAlphaClipping = enableAlphaClipping;
            
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
                materialMapping = HLODEditorSettings.Instance.DefaultMaterialMapping;
            }

            List<MeshCombiner.CombineInfo> combineInfos = new List<MeshCombiner.CombineInfo>();
            var hlodWorldToLocal = rootTransform.worldToLocalMatrix;

            for (int i = 0; i < info.WorkingObjects.Count; ++i)
            {
                var obj = info.WorkingObjects[i];
                if (obj.Mesh == null)
                    continue;

                ConvertMesh(obj.Mesh, obj.Materials, atlas, materialMapping.TextureInfoList[0].InputTexturePropertyNames);

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
            if (updated.Length > 0)
            {
                // Some meshes have submeshes that either aren't expected to render or are missing a material, so go ahead and skip
                int subMeshCount = Mathf.Min(mesh.subMeshCount, materials.Count);
                for (int mi = 0; mi < subMeshCount; ++mi)
                {
                    int[] indices = mesh.GetTriangles(mi);
                    foreach (var i in indices)
                    {
                        if (updated[i] == false)
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
                            if (uv2.Length > 0)
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
                256, 512, 1024, 2048, 4096, 8192, 16384
            };
            public static List<string> PackTextureSizeNames;

            public static int[] LimitTextureSizes = new int[]
            {
                32, 64, 128, 256, 512, 1024
            };
            public static List<string> LimitTextureSizeNames;

            static Styles()
            {
                PackTextureSizeNames = new List<string>(PackTextureSizes.Length);
                for (int i = 0; i < PackTextureSizes.Length; ++i)
                {
                    PackTextureSizeNames.Add(PackTextureSizes[i].ToString());
                }

                LimitTextureSizeNames = new List<string>(LimitTextureSizes.Length);
                for (int i = 0; i < LimitTextureSizes.Length; ++i)
                {
                    LimitTextureSizeNames.Add(LimitTextureSizes[i].ToString());
                }
            }
        }
        
        public static VisualElement CreateGUI(HLOD hlod)
        {
            var root = new VisualElement();

            dynamic batcherOptions = hlod.BatcherOptions;

            SimpleBatcher.InitializeOptions(batcherOptions);

            var packTextureSize = new DropdownField() { label = "Pack texture size" };
            packTextureSize.choices = Styles.PackTextureSizeNames;
            packTextureSize.value = batcherOptions.PackTextureSize.ToString();
            packTextureSize.RegisterValueChangedCallback((e) =>
            {
                batcherOptions.PackTextureSize = Styles.PackTextureSizes[Styles.PackTextureSizeNames.IndexOf(e.newValue)];
            });
            root.Add(packTextureSize);

            var limitTextureSize = new DropdownField() { label = "Texture size limit" };
            limitTextureSize.choices = Styles.LimitTextureSizeNames;
            limitTextureSize.value = batcherOptions.LimitTextureSize.ToString();
            limitTextureSize.RegisterValueChangedCallback((e) =>
            {
                batcherOptions.LimitTextureSize = Styles.LimitTextureSizes[Styles.LimitTextureSizeNames.IndexOf(e.newValue)];
            });
            root.Add(limitTextureSize);

            var materialElement = new ObjectField() { label = "Material", objectType = typeof(Material) };
            root.Add(materialElement);

            {
                Material mat = null;

                string matGUID = batcherOptions.MaterialGUID;
                string path = "";
                if (string.IsNullOrEmpty(matGUID) == false)
                {
                    path = AssetDatabase.GUIDToAssetPath(matGUID);
                    mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
                materialElement.SetValueWithoutNotify(mat);
            }
            materialElement.RegisterValueChangedCallback((e) =>
            {
                var mat = e.newValue;
                if (mat == null)
                    mat = new Material(GraphicsUtils.GetDefaultShader());

                var path = AssetDatabase.GetAssetPath(mat);
                var matGUID = AssetDatabase.AssetPathToGUID(path);

                batcherOptions.MaterialGUID = matGUID;
            });

            var allowAlphaClippingElement = new Toggle() { 
                label = "Allow Alpha Clipping", 
                tooltip = "Allows the batcher to enable Alpha Clipping on the HLOD material if any of the source materials use transparancy.",
                value = (bool)batcherOptions.AllowAlphaClipping
            };
            root.Add(allowAlphaClippingElement);

            allowAlphaClippingElement.RegisterValueChangedCallback((e) => { batcherOptions.AllowAlphaClipping = (bool)e.newValue; });

            {
                MaterialMapping materialMapping = batcherOptions.MaterialMapping;

                var mappingLabel = new Label() { text = "Both this component's Material Mapping and the default are set to null.\nPlease assign a Material Mapping object to either this component or Preferences/HLOD/Default Material Mapping" };
                mappingLabel.style.whiteSpace = WhiteSpace.Normal;
                mappingLabel.style.display = DisplayStyle.None;

                MaterialMappingElement materialMappingElement = null;

                var materialMappingAssetElement = new DynamicAssetPropertyElement<MaterialMapping>("Material Mapping Asset", (string)batcherOptions.MaterialMappingGUID, null, (newValue, guid) =>
                {
                    // Resolve material mapping
                    var mapping = newValue;
                    if (mapping == null)
                    {
                        mapping = HLODEditorSettings.Instance.DefaultMaterialMapping;
                    }

                    batcherOptions.MaterialMapping = mapping;
                    batcherOptions.MaterialMappingGUID = guid;

                    if (mapping == null)
                    {
                        mappingLabel.style.display = DisplayStyle.Flex;;
                    }
                    else
                    {
                        materialMappingElement.Bind(hlod, mapping);
                        mappingLabel.style.display = DisplayStyle.None;
                    }
                });

                materialMappingElement = new MaterialMappingElement(() => {
                    var mapping = (MaterialMapping)batcherOptions.MaterialMapping;
                    if(mapping == null)
                    {
                        mapping = HLODEditorSettings.Instance.DefaultMaterialMapping;
                    }
                    var serializedObject = new SerializedObject(mapping);
                    EditorUtility.SetDirty(mapping);
                    serializedObject.ApplyModifiedProperties();
                });

                root.Add(materialMappingAssetElement);
                materialMappingAssetElement.value = materialMapping;

                var materialMappingFoldout = new Foldout() { text = "Material Mapping" };
                root.Add(materialMappingFoldout);

                materialMappingFoldout.Add(mappingLabel);
                materialMappingFoldout.Add(materialMappingElement);

                var resolvedMapping = materialMapping;
                if (resolvedMapping == null)
                {
                    resolvedMapping = HLODEditorSettings.Instance.DefaultMaterialMapping;
                }
                materialMappingElement.Bind(hlod, resolvedMapping);
            }

            return root;
        }
    }

}
