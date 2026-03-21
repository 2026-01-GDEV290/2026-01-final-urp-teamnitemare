using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Triggerable : MonoBehaviour
{
    public bool isActive = true;
    public GameObject playerOrNullForAll = null;
    public UnityEngine.Events.UnityEvent onTrigger;

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
            }
        }
    }
}
