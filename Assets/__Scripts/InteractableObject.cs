using UnityEngine;

public class InteractableObject : InteractableBase, ISaveable
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
