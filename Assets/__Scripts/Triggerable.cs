using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Triggerable : MonoBehaviour, ISaveable
{
    public bool isActive = true;
    public GameObject playerOrNullForAll = null;
    public UnityEngine.Events.UnityEvent onTrigger;
    public int triggeredCount = 0;
    bool isActiveInScene = false;
    bool bDataRestored = false;

    void Awake()
    {
        isActiveInScene = gameObject.activeInHierarchy;
    }
    void Star()
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

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            if (playerOrNullForAll == null || other.gameObject == playerOrNullForAll)  
            {
                onTrigger.Invoke();
                triggeredCount++;
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
