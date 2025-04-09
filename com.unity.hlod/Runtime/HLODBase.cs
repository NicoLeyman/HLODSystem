using Unity.HLODSystem.Serializer;
using Unity.HLODSystem.Streaming;
using Unity.HLODSystem;
using UnityEngine;
using System.Collections.Generic;
using System;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public abstract class HLODBase : MonoBehaviour, ISerializationCallbackReceiver, IGeneratedResourceManager
    {
        [SerializeField]
        protected float m_ChunkSize = 30.0f;
        [SerializeField]
        protected float m_LODScreenRatioThreshold = 0.3f;
        [SerializeField]
        protected float m_CullScreenRatioThreshold = 0.01f;

        [SerializeField]
        protected SerializableDynamicObject m_SpaceSplitterOptions = new SerializableDynamicObject();
        [SerializeField]
        protected SerializableDynamicObject m_SimplifierOptions = new SerializableDynamicObject();
        [SerializeField]
        protected SerializableDynamicObject m_StreamingOptions = new SerializableDynamicObject();

        [SerializeField]
        protected List<Object> m_generatedObjects = new List<Object>();
        [SerializeField]
        protected List<GameObject> m_convertedPrefabObjects = new List<GameObject>();

        private Type m_SpaceSplitterType;
        private Type m_SimplifierType;
        private Type m_StreamingType;
        private Type m_UserDataSerializerType;

        // Unity serializer does not support serialization with System.Type
        // So, we should convert to string to store value.
        [SerializeField]
        private string m_SpaceSplitterTypeStr;
        [SerializeField]
        protected string m_SimplifierTypeStr;
        [SerializeField]
        protected string m_StreamingTypeStr;
        [SerializeField]
        protected string m_UserDataSerializerTypeStr;

        public float ChunkSize
        {
            get { return m_ChunkSize; }
        }

        public float LODScreenRatioTreshold
        {
            get { return m_LODScreenRatioThreshold; }
            set { 
                m_LODScreenRatioThreshold = value;
#if UNITY_EDITOR
                if(TryGetComponent(out HLODControllerBase controller))
                {
                    controller.LODScreenRatioTreshold = m_LODScreenRatioThreshold;
                }
#endif
            }
        }

        public float CullScreenRatioTreshold
        {
            get { return m_CullScreenRatioThreshold; }
            set {
                m_CullScreenRatioThreshold = value;
#if UNITY_EDITOR
                if (TryGetComponent(out HLODControllerBase controller))
                {
                    controller.CullScreenRatioTreshold = m_LODScreenRatioThreshold;
                }
#endif
            }
        }

        public Type SpaceSplitterType
        {
            set { m_SpaceSplitterType = value; }
            get { return m_SpaceSplitterType; }
        }

        public Type SimplifierType
        {
            set { m_SimplifierType = value; }
            get { return m_SimplifierType; }
        }

        public Type StreamingType
        {
            set { m_StreamingType = value; }
            get { return m_StreamingType; }
        }

        public Type UserDataSerializerType
        {
            set { m_UserDataSerializerType = value; }
            get { return m_UserDataSerializerType; }
        }

        public SerializableDynamicObject SpaceSplitterOptions
        {
            get { return m_SpaceSplitterOptions; }
        }

        public SerializableDynamicObject StreamingOptions
        {
            get { return m_StreamingOptions; }
        }

        public SerializableDynamicObject SimplifierOptions
        {
            get { return m_SimplifierOptions; }
        }

#if UNITY_EDITOR
        public List<Object> GeneratedObjects
        {
            get { return m_generatedObjects; }
        }

        public List<GameObject> ConvertedPrefabObjects
        {
            get { return m_convertedPrefabObjects; }
        }

        public virtual void OnBeforeSerialize()
        {
            if (m_SpaceSplitterType != null)
                m_SpaceSplitterTypeStr = m_SpaceSplitterType.AssemblyQualifiedName;
            if (m_SimplifierType != null)
                m_SimplifierTypeStr = m_SimplifierType.AssemblyQualifiedName;
            if (m_StreamingType != null)
                m_StreamingTypeStr = m_StreamingType.AssemblyQualifiedName;
            if (m_UserDataSerializerType != null)
                m_UserDataSerializerTypeStr = m_UserDataSerializerType.AssemblyQualifiedName;
        }
        public virtual void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(m_SpaceSplitterTypeStr))
            {
                m_SpaceSplitterType = null;
            }
            else
            {
                m_SpaceSplitterType = Type.GetType(m_SpaceSplitterTypeStr);
            }

            if (string.IsNullOrEmpty(m_SimplifierTypeStr))
            {
                m_SimplifierType = null;
            }
            else
            {
                m_SimplifierType = Type.GetType(m_SimplifierTypeStr);
            }

            if (string.IsNullOrEmpty(m_StreamingTypeStr))
            {
                m_StreamingType = null;
            }
            else
            {
                m_StreamingType = Type.GetType(m_StreamingTypeStr);
            }

            if (string.IsNullOrEmpty(m_UserDataSerializerTypeStr))
            {
                m_UserDataSerializerType = null;
            }
            else
            {
                m_UserDataSerializerType = Type.GetType(m_UserDataSerializerTypeStr);
            }
        }
        public void AddGeneratedResource(Object obj)
        {
            m_generatedObjects.Add(obj);
        }

        public bool IsGeneratedResource(Object obj)
        {
            return m_generatedObjects.Contains(obj);
        }

        public void AddConvertedPrefabResource(GameObject obj)
        {
            m_convertedPrefabObjects.Add(obj);
        }

        public abstract Bounds GetBounds();

        public List<HLODControllerBase> GetHLODControllerBases()
        {
            List<HLODControllerBase> controllerBases = new List<HLODControllerBase>();

            foreach (Object obj in m_generatedObjects)
            {
                var controllerBase = obj as HLODControllerBase;
                if (controllerBase != null)
                    controllerBases.Add(controllerBase);
            }

            //if controller base doesn't exists in the generated objects, it was created from old version.
            //so adding controller base manually.
            if (controllerBases.Count == 0)
            {
                var controller = GetComponent<Streaming.HLODControllerBase>();
                if (controller != null)
                {
                    controllerBases.Add(controller);
                }
            }
            return controllerBases;
        }

        public void TryGatheringGeneratedObjects()
        {
            // Make a last ditch effort to clean up scene objects if there was some issue with keeping track of GeneratedObjects.
            if (GeneratedObjects.Count == 0)
            {
                var children = GetComponentsInChildren<Transform>();
                for (var c = 0; c < children.Length; c++)
                {
                    var childTransform = children[c];
                    if (childTransform.name == "HLODRoot")
                    {
                        var generatedTransforms = childTransform.GetComponentsInChildren<Transform>();
                        foreach (var generatedTransform in generatedTransforms)
                        {
                            GeneratedObjects.Add(generatedTransform.gameObject);
                        }
                        break;
                    }
                }

                var controller = GetComponent<HLODControllerBase>();
                if (controller != null)
                {
                    GeneratedObjects.Add(controller);
                }

                var userData = GetComponent<UserDataSerializerBase>();
                if (userData != null)
                {
                    GeneratedObjects.Add(userData);
                }
            }
        }
#endif

    }
}
