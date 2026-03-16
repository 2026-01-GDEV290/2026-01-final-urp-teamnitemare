using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public string interactText = "Interact";
    public string interactResponseText = "You interacted with the object!";
    
    public bool isInteractable = true;
    public bool isOneTimeUse = false;
    public int interactionCount = 0;

    public UnityEngine.Events.UnityEvent onInteract;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{ }

    // Update is called once per frame
    //void Update()
    //{}

    public void Interact()
    {
        if (!isInteractable)
        {
            return;
        }

        onInteract.Invoke();
        interactionCount++;

        if (isOneTimeUse)
        {
            isInteractable = false;
        }
    }
}
