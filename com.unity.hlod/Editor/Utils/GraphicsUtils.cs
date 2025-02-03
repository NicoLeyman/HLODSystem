
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.HLODSystem.Utils
{
    public static class GraphicsUtils
    {
        [Flags] // Bitflags in case we ever need it in context of cross pipeline development.
        public enum RenderPipeline
        {
            None = 0,
            BuiltIn = 1,
            URP = 2,
            HDRP = 4,
            Other = 8
        }

        private static RenderPipeline _activeRenderPipeline = RenderPipeline.None;
        public static RenderPipeline ActiveRenderPipeline
        {
            get 
            { 
                if (_activeRenderPipeline == RenderPipeline.None)
                {
                    if (GraphicsSettings.currentRenderPipeline)
                    {
                        var activePipelineName = GraphicsSettings.currentRenderPipeline.GetType().ToString();
                        if (activePipelineName.Contains("HighDefinition"))
                        {
                            _activeRenderPipeline = RenderPipeline.HDRP;
                        }
                        else if(activePipelineName.Contains("Universal"))
                        {
                            _activeRenderPipeline = RenderPipeline.URP;
                        }
                        else
                        {
                            _activeRenderPipeline = RenderPipeline.Other;
                        }
                    }
                    else
                    {
                        _activeRenderPipeline = RenderPipeline.BuiltIn;
                    }
                }
                return _activeRenderPipeline;
            }
            private set { }
        }

        public static Shader GetDefaultShader()
        {
            string shaderName = null;
            
            var defaultShader = HLODEditorSettings.Instance.DefaultShader;
            if (defaultShader != null)
                return defaultShader;

            switch(ActiveRenderPipeline)
            {
                case RenderPipeline.BuiltIn:
                    shaderName = "Standard";
                    break;
                case RenderPipeline.URP:
                    shaderName = "Universal Render Pipeline/Lit";
                    break;
                case RenderPipeline.HDRP:
                    shaderName = "HDRenderPipeline/Lit";
                    break;
            }

            return shaderName == null ? null : Shader.Find(shaderName);
        }
    }
}