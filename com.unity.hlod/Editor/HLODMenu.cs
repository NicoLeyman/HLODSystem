using Unity.HLODSystem;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;

public class HLODMenu
{
    [MenuItem("HLOD/Regenerate Scene HLODs")]
    public static void RegenerateSceneHLODs()
    {
            var hlods = GameObject.FindObjectsByType<HLOD>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);

            foreach(var hlod in hlods)
            {
                CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
            }
    }

    [MenuItem("HLOD/Destroy Scene HLODs")]
    public static void DestroySceneHLODs()
    {
        var hlods = GameObject.FindObjectsByType<HLOD>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        foreach (var hlod in hlods)
        {
            CoroutineRunner.RunCoroutine(HLODCreator.Destroy(hlod));
        }
    }
}
