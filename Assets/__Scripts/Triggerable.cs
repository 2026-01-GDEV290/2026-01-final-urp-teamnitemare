using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Triggerable : MonoBehaviour, ISaveable
{
    public bool isActive = true;
    public GameObject playerOrNullForAll = null;
    public UnityEngine.Events.UnityEvent onTrigger;
    public UnityEngine.Events.UnityEvent onTriggerExit;
    public int triggeredCount = 0;
    bool isActiveInScene = false;
    bool bDataRestored = false;
    GameObject objectTriggeredBy = null;

    void Awake()
    {
        isActiveInScene = gameObject.activeInHierarchy;
    }
    void Start()
    {
        if (bDataRestored)
        {
            gameObject.SetActive(isActiveInScene);
        }
    }

    public void SetIsActive(bool active)
    {
        isActive = active;
    }

    public void AddTriggerListener(UnityEngine.Events.UnityAction action)
    {
        onTrigger.AddListener(action);
    }
    public void RemoveTriggerListener(UnityEngine.Events.UnityAction action)
    {
        onTrigger.RemoveListener(action);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            if (playerOrNullForAll == null || other.gameObject == playerOrNullForAll)  
            {
                objectTriggeredBy = other.gameObject;
                onTrigger.Invoke();
                triggeredCount++;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (isActive && objectTriggeredBy == other.gameObject)
        {
            if (playerOrNullForAll == null || other.gameObject == playerOrNullForAll)
            {
                onTriggerExit.Invoke();
            }
        }
    }
#region ISaveable implementation
    private class TriggerableData
    {
        public bool isActive;
        public bool isActiveInScene;
        public int triggeredCount;
    }

    public object CaptureState()
    {
        var data = new TriggerableData
        {
            isActive = this.isActive,
            isActiveInScene = this.isActiveInScene,
            triggeredCount = this.triggeredCount
        };
        return data;
    }
    public void RestoreState(object state)
    {
        if (state is TriggerableData data)
        {
            this.isActive = data.isActive;
            this.isActiveInScene = data.isActiveInScene;
            this.triggeredCount = data.triggeredCount;
            // not running onTrigger here since it is location-based
        }
        //gameObject.SetActive(isActiveInScene);
    }
#endregion ISaveable implementation
}
