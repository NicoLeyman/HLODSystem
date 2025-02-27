using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.HLODSystem
{
    public class TerrainHLOD : HLODBase
    {
        [SerializeField] private TerrainData m_TerrainData;
        [SerializeField] private bool m_DestroyTerrain = true;
        [SerializeField] private int m_BorderVertexCount = 256;

        [SerializeField] private string m_materialGUID = "";
        [SerializeField] private string m_materialLowGUID = "";
        [SerializeField] private int m_textureSize = 64;

        [SerializeField] private bool m_useNormal = false;
        [SerializeField] private bool m_useMask = false;

        [SerializeField] private string m_albedoPropertyName = "";
        [SerializeField] private string m_normalPropertyName = "";
        [SerializeField] private string m_maskPropertyName = "";

        public TerrainData TerrainData
        {
            set { m_TerrainData = value;}
            get { return m_TerrainData; }
        }
        public bool DestroyTerrain
        {
            set { m_DestroyTerrain = value; }
            get { return m_DestroyTerrain; }
        }

        public int BorderVertexCount
        {
            get { return m_BorderVertexCount; }
            set { m_BorderVertexCount = value; }
        }

        public int TextureSize
        {
            set { m_textureSize = value; }
            get { return m_textureSize; }
        }

        public string MaterialGUID
        {
            set { m_materialGUID = value; }
            get { return m_materialGUID; }
        }

        public string MaterialLowGUID
        {
            set { m_materialLowGUID = value; }
            get { return m_materialLowGUID; }
        }

        public bool UseNormal
        {
            set { m_useNormal = value; }
            get { return m_useNormal; }
        }

        public bool UseMask
        {
            set { m_useMask = value; }
            get { return m_useMask; }
        }

        public string AlbedoPropertyName
        {
            set { m_albedoPropertyName = value; }
            get { return m_albedoPropertyName; }
        }

        public string NormalPropertyName
        {
            set { m_normalPropertyName = value; }
            get { return m_normalPropertyName; }
        }

        public string MaskPropertyName
        {
            set { m_maskPropertyName = value; }
            get { return m_maskPropertyName; }
        }
        
        public Bounds GetBounds()
        {
            if ( m_TerrainData == null )
                return new Bounds();
            return new Bounds(m_TerrainData.size * 0.5f, m_TerrainData.size);
        }

    }
}