using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Triggerable : MonoBehaviour
{
    public bool isActive = true;
    public UnityEngine.Events.UnityEvent onTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            onTrigger.Invoke();
        }
    }
}
