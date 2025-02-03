using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.HLODSystem.Utils;

namespace Unity.HLODSystem
{
    public static partial class CreateUtils
    {
        private class MeshRendererCalculator
        {
            private bool m_isCalculated = false;
            private List<HLODMeshSetter> m_meshSetters = new List<HLODMeshSetter>();
            private Dictionary<MeshRenderer, MeshFilter> m_meshRenderers = new Dictionary<MeshRenderer, MeshFilter>();
            private List<LODGroup> m_lodGroups = new List<LODGroup>();

            private List<(MeshRenderer, MeshFilter)> m_resultMeshRenderers = new List<(MeshRenderer, MeshFilter)>();

            public List<(MeshRenderer, MeshFilter)> ResultMeshRenderers
            {
                get { return m_resultMeshRenderers; }
            }
            
            public MeshRendererCalculator(List<GameObject> targetGameObjects)
            {
                var renderers = new List<MeshRenderer>();

                for (int oi = 0; oi < targetGameObjects.Count; ++oi)
                {
                    var target = targetGameObjects[oi];
                    
                    m_meshSetters.AddRange(target.GetComponentsInChildren<HLODMeshSetter>());

                    target.GetComponentsInChildren(true, renderers);

                    foreach(var renderer in renderers)
                    {
                        var mf = renderer.GetComponent<MeshFilter>();
                        if(mf != null && mf.sharedMesh != null)
                        {
                            m_meshRenderers.Add(renderer, mf);
                        }
                    }

                    m_lodGroups.AddRange(target.GetComponentsInChildren<LODGroup>());
                }

                RemoveDisabled(m_meshSetters);
                RemoveDisabled(m_lodGroups);
                RemoveDisabled(m_meshRenderers);
            }

            public void Calculate(float minObjectSize, int level)
            {
                if (m_isCalculated == true)
                    return;

                for (int si = 0; si < m_meshSetters.Count; ++si)
                {
                    AddResultFromMeshSetters(m_meshSetters[si], minObjectSize, level);
                }

                for (int gi = 0; gi < m_lodGroups.Count; ++gi)
                {
                    LODGroup lodGroup = m_lodGroups[gi];
                    LOD[] lods = lodGroup.GetLODs();
                    for (int li = 0; li < lods.Length; ++li)
                    {
                        Renderer[] lodRenderers = lods[li].renderers;

                        //Remove every mesh renderer which is registered to the LODGroup.
                        for (int ri = 0; ri < lodRenderers.Length; ++ri)
                        {
                            MeshRenderer mr = lodRenderers[ri] as MeshRenderer;
                            if (mr == null)
                                continue;

                            m_meshRenderers.Remove(mr);
                        }
                    }

                    AddResultFromLODGroup(lodGroup, minObjectSize);
                }

                foreach((MeshRenderer mr, MeshFilter mf) in m_meshRenderers)
                {
                    float max = Mathf.Max(mr.bounds.size.x, mr.bounds.size.y, mr.bounds.size.z);
                    if (max < minObjectSize)
                        continue;

                    if (mr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                        continue;

                    m_resultMeshRenderers.Add((mr, mf));
                }

                m_isCalculated = true;
            }


            private void AddResultFromMeshSetters(HLODMeshSetter setter, float minObjectSize, int level)
            {
                var group = setter.FindGroup(level);
                
                //If group is null, there is no MeshSetting for current level.
                if (group == null)
                    return;

                var renderers = group.MeshRenderers;
                m_resultMeshRenderers.Capacity += renderers.Count;
                foreach (var renderer in renderers)
                {
                    var mf = renderer.GetComponent<MeshFilter>();
                    if(mf == null) 
                        continue;

                    if (renderer.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                        continue;

                    m_resultMeshRenderers.Add((renderer, mf));
                }
                RemoveUnderMeshSetters(setter);
            }

            private void AddResultFromLODGroup(LODGroup lodGroup, float minObjectSize)
            {
                LOD[] lods = lodGroup.GetLODs();
                Renderer[] renderers = lods.Last().renderers;
                for (int ri = 0; ri < renderers.Length; ++ri)
                {
                    MeshRenderer mr = renderers[ri] as MeshRenderer;

                    if (mr == null)
                        continue;

                    if (mr.gameObject.activeInHierarchy == false || mr.enabled == false)
                        continue;

                    if (mr.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)
                        continue;

                    float max = Mathf.Max(mr.bounds.size.x, mr.bounds.size.y, mr.bounds.size.z);
                    if (max < minObjectSize)
                        continue;

                    var mf = mr.GetComponent<MeshFilter>();

                    if (mf == null)
                        continue;

                    m_resultMeshRenderers.Add((mr, mf));
                }
            }

            private void RemoveUnderMeshSetters(HLODMeshSetter setter)
            {
                m_lodGroups.RemoveAll(setter.GetComponentsInChildren<LODGroup>());
                var renderers = setter.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    m_meshRenderers.Remove(renderer);
                }
            }

            
            //enabled is not Component variable.
            //So we can not make this to Generic function.
            private void RemoveDisabled(List<HLODMeshSetter> componentList)
            {
                for (int i = 0; i < componentList.Count; ++i)
                {
                    if (componentList[i].enabled == true && componentList[i].gameObject.activeInHierarchy == true)
                    {
                        continue;
                    }

                    int backIndex = componentList.Count - 1;
                    componentList[i] = componentList[backIndex];
                    componentList.RemoveAt(backIndex);
                    i -= 1;
                }
            }
            private void RemoveDisabled(List<LODGroup> componentList)
            {
                for (int i = 0; i < componentList.Count; ++i)
                {
                    if (componentList[i].enabled == true && componentList[i].gameObject.activeInHierarchy == true)
                    {
                        continue;
                    }

                    int backIndex = componentList.Count - 1;
                    componentList[i] = componentList[backIndex];
                    componentList.RemoveAt(backIndex);
                    i -= 1;
                }
            }
            private void RemoveDisabled(Dictionary<MeshRenderer, MeshFilter> meshRenderers)
            {
                var componentList = meshRenderers.Keys.ToArray();
                for (int i = 0; i < componentList.Length; ++i)
                {
                    var renderer = componentList[i];
                    if (renderer.enabled == true && renderer.gameObject.activeInHierarchy == true)
                    {
                        continue;
                    }

                    meshRenderers.Remove(renderer);
                }
            }
        }
        public static List<(MeshRenderer, MeshFilter)> GetMeshRenderers(List<GameObject> gameObjects, float minObjectSize, int level)
        {
            MeshRendererCalculator calculator = new MeshRendererCalculator(gameObjects);
            calculator.Calculate(minObjectSize, level);
            return calculator.ResultMeshRenderers;
        }
        
        public static List<(MeshRenderer, MeshFilter)> GetMeshRenderers(GameObject gameObject, float minObjectSize, int level)
        {
            List<GameObject> tmpList = new List<GameObject>();
            tmpList.Add(gameObject);
            
            MeshRendererCalculator calculator = new MeshRendererCalculator(tmpList);
            calculator.Calculate(minObjectSize, level);
            return calculator.ResultMeshRenderers;
        }

    }

}