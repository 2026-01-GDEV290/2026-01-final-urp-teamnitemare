using System;
using UnityEngine;
using TMPro;

[Serializable]
public enum InteractableType
{
    None,
    Object,
    NPC,
    Other
}

[Serializable]
public abstract class InteractableBase : MonoBehaviour
{
    protected InteractableType interactableType = InteractableType.None;
    public string interactText = "Interact";
    public string interactResponseText = "You interacted with the object!";
    
    public bool isInteractable = true;
    public bool isOneTimeUse = false;
    public int interactionCount = 0;

    [SerializeField] GameObject billBoardObject = null;
    [SerializeField] Vector3 billBoardOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] TMP_Text billBoardTextObject = null;
    [SerializeField] Vector3 billBoardTextOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] string billBoardInteractText = "Interact";
    [SerializeField] bool showBillboardOnStart = true;

    protected virtual void Awake()
    {
        if (billBoardObject != null)
        {
            // Check if billBoardObject is actually a billBoardText object
            if (billBoardObject.GetComponent<TMP_Text>() != null)
            {
                billBoardTextObject = billBoardObject.GetComponent<TMP_Text>();
                // don't need this anymore
                billBoardObject = null;
            }
            // else it's possible to have BOTH a billboard (say, image) and billboardtext object
        }
    }
    protected virtual void Start()
    {
        if (billBoardObject != null)
        {
            billBoardObject.transform.position = transform.position + billBoardOffset;
        }
        if (billBoardTextObject != null)
        {

            billBoardTextObject.text = billBoardInteractText;
            billBoardTextObject.transform.position = transform.position + billBoardTextOffset;
        }
        if (!showBillboardOnStart)
        {
            SetBillboardVisibility(false);
        }
    }

    public abstract bool CanInteract();

    public abstract void SetIsInteractable(bool value);

    public abstract void Interact();

    public void SetBillboardText(string text)
    {
        if (billBoardTextObject != null)
        {
            billBoardTextObject.text = text;
        }
    }

    public void SetBillboardVisibility(bool visible)
    {
        if (billBoardObject != null)
        {
            billBoardObject.SetActive(visible);
        }
        if (billBoardTextObject != null)
        {
            billBoardTextObject.gameObject.SetActive(visible);
        }
    }

}

