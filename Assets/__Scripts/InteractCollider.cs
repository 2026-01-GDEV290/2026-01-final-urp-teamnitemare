using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class InteractCollider : MonoBehaviour
{
    // delegate + event to notify parent InteractableObject of trigger enter
    public delegate void PlayerEnterTriggerHandler(InteractableObject interactable);
    public event PlayerEnterTriggerHandler OnPlayerHitInteractable;
    public event PlayerEnterTriggerHandler OnPlayerLeaveInteractable;

    InteractableObject interactableObject = null;

    [SerializeField] private TMP_Text interactText;

    void Awake()
    {
        if (interactText == null)
        {
            interactText = GetComponentInChildren<TMP_Text>(true);            
        }
        SetInteractText("");
    }

    public void SetInteractText(string text)
    {
        if (interactText != null)
        {
            interactText.text = text;
        }
    }

    void FixedUpdate()
    {
        if (interactableObject != null)
        {
            // if object is disabled or non-interactable, treat as if player left the trigger
            if (!interactableObject.gameObject.activeInHierarchy || !interactableObject.isInteractable)
            {
                //Debug.Log($"InteractCollider treating {interactableObject.gameObject.name} as left trigger because it is no longer active or interactable");
                OnPlayerLeaveInteractable?.Invoke(interactableObject);
                interactableObject = null;
                SetInteractText("");
            }

        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log($"InteractCollider entered trigger: {other.gameObject.name}");
        if (other.TryGetComponent(out InteractableObject interactable))
        {
            interactableObject = interactable;
            OnPlayerHitInteractable?.Invoke(interactable);
            Debug.Log($"InteractCollider found InteractableObject: {interactable.gameObject.name}");
            Debug.Log($"Interactable text: {interactable.interactText}");
        }
    }
    void OnTriggerExit(Collider other)
    {
        //Debug.Log($"InteractCollider exited trigger: {other.gameObject.name}");
        if (interactableObject != null)
        {
            OnPlayerLeaveInteractable?.Invoke(interactableObject);
            interactableObject = null;
        }
    }
}
