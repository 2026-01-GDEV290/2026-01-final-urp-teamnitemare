using UnityEngine;

public class InteractableObject : InteractableBase
{
    public UnityEngine.Events.UnityEvent onInteract;

    InteractableObject() : base()
    {
        interactableType = InteractableType.Object;
    }
    protected override void Awake()
    {
        base.Awake();
        // Additional initialization if needed
    }
    protected override void Start()
    {
        base.Start();
        // Additional initialization if needed
    }

    public override bool CanInteract()
    {
        return isInteractable;
    }

    public override void SetIsInteractable(bool value)
    {
        isInteractable = value;
        if (value && interactionCount > 0 && isOneTimeUse)
        {
            interactionCount = 0;
        }
    }

    public override void Interact(bool forceOverride = false)
    {
        if (!isInteractable && !forceOverride)
        {
            return;
        }
        Debug.Log("IBO->Interacted with object of type: " + interactableType + " with interactText: " + interactText);
        // default?
        //SetBillboardVisibility(false);

        onInteract.Invoke();
        interactionCount++;

        if (isOneTimeUse)
        {
            isInteractable = false;
        }
    }
}
