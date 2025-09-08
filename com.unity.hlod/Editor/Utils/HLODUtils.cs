using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.HLODSystem.Utils
{
    public static class HLODUtils
    {
        public static float GetChunkSizePropertyValue(float value)
        {
            if (value < 0.05f)
            {
                return 0.05f;
            }
            return value;
        }

        public static void DestroyHLOD(HLODBase hlod)
        {
            if(hlod is HLOD)
            {
                CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod as HLOD));
            }
            else if(hlod is TerrainHLOD)
            {
                CoroutineRunner.RunCoroutine(TerrainHLODCreator.Destroy(hlod as TerrainHLOD));
            }
        }
    }
}