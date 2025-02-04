using Unity.HLODSystem;
using Unity.HLODSystem.Utils;
using UnityEditor;
using UnityEngine;

public class HLODMenu
{
    [MenuItem("HLOD/Regenerate All HLODs")]
    public static void RegenerateAllHLODs()
    {
            var hlods = GameObject.FindObjectsByType<HLOD>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);

            foreach(var hlod in hlods)
            {
                CoroutineRunner.RunCoroutine(HLODCreator.Create(hlod));
            }
    }
}
