using System;
using UnityEngine;
using TMPro;

[Serializable]
enum InteractableType
{
    None,
    Object,
    NPC,
    Other
}

[Serializable]
public class InteractableBase : MonoBehaviour
{
    InteractableType interactableType = InteractableType.None;
    public string interactText = "Interact";
    public string interactResponseText = "You interacted with the object!";
    
    public bool isInteractable = true;
    public bool isOneTimeUse = false;
    public int interactionCount = 0;

    [SerializeField] GameObject billBoardObject;
    [SerializeField] Vector3 billBoardOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] TMP_Text billBoardText;
    [SerializeField] Vector3 billBoardTextOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] string billBoardInteractMessage = "Interact";
    [SerializeField] bool showBillboard = true;

    public UnityEngine.Events.UnityEvent onInteract;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{ }

    // Update is called once per frame
    //void Update()
    //{}

    public bool CanInteract()
    {
        return isInteractable;
    }

    public void SetIsInteractable(bool value)
    {
        isInteractable = value;
        if (value && interactionCount > 0 && isOneTimeUse)
        {
            interactionCount = 0;
        }
    }

    public virtual void Interact()
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

