using UnityEngine;
using TMPro;
using Ink.Runtime;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]

    [SerializeField] private GameObject dialoguePanel;

    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Choices UI")]

    [SerializeField] private GameObject[] choices;

    private TextMeshProUGUI[] choicesText; 

    private Story currentStory;
    private InputSystem_Actions inputActions;
    private bool consumeNextInteract;

    public bool dialogueIsPlaying { get; private set; }
    public event Action<bool> DialogueStateChanged;

    private static DialogueManager instance;

    private DialogueVariables dialogueVariables;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the Scene");
        }
        instance = this;

        dialogueVariables = new DialogueVariables();
        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputSystem_Actions();
        }

        inputActions.Enable();
        inputActions.Player.Interact.started += OnInteractPerformed;
    }

    private void OnDisable()
    {
        if (inputActions == null)
        {
            return;
        }

        inputActions.Player.Interact.started -= OnInteractPerformed;
        inputActions.Disable();
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        SetDialogueState(false);
        dialoguePanel.SetActive(false);

        //get all of the choices text
        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>(true);
            index++;
        }

    }

    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (consumeNextInteract)
        {
            consumeNextInteract = false;
            return;
        }

        if (!dialogueIsPlaying || currentStory == null)
        {
            return;
        }

        if (currentStory.currentChoices.Count == 0)
        {
            ContinueStory();
            return;
        }

        TrySubmitSelectedChoice();
    }

    public void EnterDialogueMode(TextAsset inkJSON, bool consumeCurrentInteract = false)
    {
        if (inkJSON == null)
        {
            Debug.LogError("Cannot enter dialogue mode with a null Ink JSON asset.");
            return;
        }

        currentStory = new Story(inkJSON.text);
        consumeNextInteract = consumeCurrentInteract;
        SetDialogueState(true);
        dialoguePanel.SetActive(true);

        dialogueVariables.StartListening(currentStory);

        ContinueStory();
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(0.2f);

        if (currentStory != null)
        {
            dialogueVariables.StopListening(currentStory);
        }

        currentStory = null;
        SetDialogueState(false);
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            //set text for the current dialogue line
            dialogueText.text = currentStory.Continue();
            // display choices, if any, for this dialogue line
            DisplayChoices();
        }
        else
        {
            StartCoroutine(ExitDialogueMode());
        }
    }

    private void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;
        
        if (currentChoices.Count > choices.Length)
        {
            Debug.LogError("More choices were given than the UI can support. Number of choices given: " + currentChoices.Count);
        }

        int index = 0;
        //enable and initialize the choices up to the amount of choices for this line of dialogue
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }
        //go through the remaining choices the UI supports and make sure they're hidden
        for (int i = index; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }

        if (currentChoices.Count > 0)
        {
            StartCoroutine(SelectFirstChoice());
        }

    }
    private IEnumerator SelectFirstChoice()
    {
        //Event System requires we clear it first, then wait
        //for at least one frame before we set the current selected object
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }

    public void MakeChoice(int choiceIndex)
    {
        if (currentStory == null)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogWarning($"Choice index {choiceIndex} is out of range.");
            return;
        }

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    private void TrySubmitSelectedChoice()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            return;
        }

        for (int i = 0; i < choices.Length; i++)
        {
            if (choices[i] == selectedObject || selectedObject.transform.IsChildOf(choices[i].transform))
            {
                MakeChoice(i);
                return;
            }
        }
    }

    private void SetDialogueState(bool isPlaying)
    {
        if (dialogueIsPlaying == isPlaying)
        {
            return;
        }

        dialogueIsPlaying = isPlaying;
        DialogueStateChanged?.Invoke(dialogueIsPlaying);
    }

}

