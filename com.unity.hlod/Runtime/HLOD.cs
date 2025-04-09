using System;
using UnityEngine;

namespace Unity.HLODSystem
{
    public class HLOD : HLODBase
    {
        public const string HLODLayerStr = "HLOD";

        [SerializeField]
        private float m_MinObjectSize = 0.0f;

        [SerializeField]
        private SerializableDynamicObject m_BatcherOptions = new SerializableDynamicObject();

        private Type m_BatcherType;

        [SerializeField]
        private string m_BatcherTypeStr;

        public Type BatcherType
        {
            set { m_BatcherType = value; }
            get { return m_BatcherType; }
        }

        public SerializableDynamicObject BatcherOptions
        {
            get { return m_BatcherOptions; }
        }

        public float MinObjectSize
        {
            set { m_MinObjectSize = value; }
            get { return m_MinObjectSize; }
        }

        public override Bounds GetBounds()
        {
            Bounds ret = new Bounds();
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                ret.center = Vector3.zero;
                ret.size = Vector3.zero;
                return ret;
            }

            Bounds bounds = Utils.BoundsUtils.CalcLocalBounds(renderers[0], transform);
            for (int i = 1; i < renderers.Length; ++i)
            {
                bounds.Encapsulate(Utils.BoundsUtils.CalcLocalBounds(renderers[i], transform));
            }

            ret.center = bounds.center;
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            ret.size = new Vector3(max, max, max);  

            return ret;
        }    

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            if ( m_BatcherType != null )
                m_BatcherTypeStr = m_BatcherType.AssemblyQualifiedName;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            if (string.IsNullOrEmpty(m_BatcherTypeStr))
            {
                m_BatcherType = null;
            }
            else
            {
                m_BatcherType = Type.GetType(m_BatcherTypeStr);
            }       
        }
    }

}