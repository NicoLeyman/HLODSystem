using UnityEngine;

#if UNITY_EDITOR
[Tooltip("Tag MonoBehaviour used to indicate to the HLOD builders that they should ignore this GameObject.")]
public class ExcludeFromHLOD : MonoBehaviour
{
    public bool ExcludeChildren = true;
}
#endif