using System;
using System.Collections;
using System.Collections.Generic;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem.Simplifier
{
    public abstract class SimplifierBase : ISimplifier
    {
        private dynamic m_options;
        public SimplifierBase(SerializableDynamicObject simplifierOptions)
        {
            m_options = simplifierOptions;
        }
        public IEnumerator Simplify(HLODBuildInfo buildInfo)
        {
            for (int i = 0; i < buildInfo.WorkingObjects.Count; ++i)
            {
                Utils.WorkingMesh mesh = buildInfo.WorkingObjects[i].Mesh;

                int triangleCount = mesh.triangles.Length / 3;
                float maxQuality = Mathf.Min((float)m_options.SimplifyMaxPolygonCount / (float)triangleCount, (float)m_options.SimplifyPolygonRatio);
                float minQuality = Mathf.Max((float)m_options.SimplifyMinPolygonCount / (float)triangleCount, 0.0f);

                var ratio = maxQuality * Mathf.Pow((float)m_options.SimplifyPolygonRatio, buildInfo.Distances[i]);
                ratio = Mathf.Max(ratio, minQuality);

                
//                while (Cache.SimplifiedCache.IsGenerating(GetType(), mesh, ratio) == true)
//                {
//                    yield return null;
//                }
//                Mesh simplifiedMesh = Cache.SimplifiedCache.Get(GetType(), mesh, ratio);
//                if (simplifiedMesh == null)
//                {
//                    Cache.SimplifiedCache.MarkGenerating(GetType(), mesh, ratio);
                    yield return GetSimplifiedMesh(mesh, ratio, (m) =>
                    {
                        buildInfo.WorkingObjects[i].SetMesh(m);
                    });
//                    Cache.SimplifiedCache.Update(GetType(), mesh, simplifiedMesh, ratio);
                    
//                }

            }            
        }

        public void SimplifyImmidiate(HLODBuildInfo buildInfo)
        {
            
            IEnumerator routine = Simplify(buildInfo);
            CustomCoroutine coroutine = new CustomCoroutine(routine);
            while (coroutine.MoveNext())
            {
                
            }
            
        }

        public static void InitializeOptions(dynamic options)
        {
            if (options.SimplifyPolygonRatio == null)
                options.SimplifyPolygonRatio = 0.8f;
            if (options.SimplifyMinPolygonCount == null)
                options.SimplifyMinPolygonCount = 10;
            if (options.SimplifyMaxPolygonCount == null)
                options.SimplifyMaxPolygonCount = 500;
        }

        protected abstract IEnumerator GetSimplifiedMesh(Utils.WorkingMesh origin, float quality, Action<Utils.WorkingMesh> resultCallback);

        protected static VisualElement CreateGUIBase(HLODBase hlod)
        {
            dynamic options = hlod.SimplifierOptions;

            InitializeOptions(options);

            var gui = new VisualElement() { name = nameof(SimplifierBase) };

            var polygonRatio = new Slider("Polygon Ratio", 0.0f, 1.0f);
            polygonRatio.value = options.SimplifyPolygonRatio;
            polygonRatio.RegisterValueChangedCallback((e) => options.polygonRatio = e.newValue);
            gui.Add(polygonRatio);

            var triangleRange = new MinMaxSlider("Triangle Range", options.SimplifyMinPolygonCount, options.SimplifyMaxPolygonCount, 100, 5000);
            triangleRange.RegisterValueChangedCallback((e) =>
            {
                options.SimplifyMinPolygonCount = e.newValue.x;
                options.SimplifyMaxPolygonCount = e.newValue.y;
            });
            gui.Add(triangleRange);

            return gui;
        }
        
    }
}
