using UnityEngine;

//!! Important: Incorporating this as a data member requires referencing a component
// Also, this script is paired with:
// Editor/UniqueIDValidator to ensure every ISaveable has a UniqueID, and that all UniqueIDs are unique
// Editor/UniqueIDInspector to display the UniqueID in the Inspector (read-only) for debugging purposes
[DefaultExecutionOrder(-250)]
public class UniqueID : MonoBehaviour
{
    [SerializeField, HideInInspector]
    private string id;

    public string ID => id;

    private void Awake()
    {
        // Runtime fallback: generate ID if missing
        if (string.IsNullOrEmpty(id))
        {
            id = System.Guid.NewGuid().ToString();
        }

        // Register if saveable
        if (TryGetComponent<ISaveable>(out var saveable))
        {
            SaveManager.Instance.Register(id, saveable);
        }
    }

#if UNITY_EDITOR
    public void OnValidate()
    {
        // Only generate IDs in the editor, never at runtime
        if (!Application.isPlaying && string.IsNullOrEmpty(id))
        {
            id = UnityEditor.GUID.Generate().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}

