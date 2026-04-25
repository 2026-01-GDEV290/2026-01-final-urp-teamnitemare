using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Ink.Runtime;
using TMPro;
using UnityEngine.EventSystems;

// special Inkle-related UnityEvents that take strings as parameters

// string tagName
public class StringEvent : UnityEvent<string> { }
public class StringListEvent : UnityEvent<List<string>> { }

// string variableName, string triggerValue
public class TwoStringEvent : UnityEvent<string, string> { }

[Serializable]
public struct InkleVariableWatch
{
    public string variableName;
    public string triggerValueOrAll;
    public TwoStringEvent onVariableMatch;
}
[Serializable]
public struct InkleTagWatch
{
    public string tagName;
    public StringEvent onTagFound;
}

[Serializable]
public struct InkleDialogueData
{
    public string text;
    public string[] options;
}

public class InkleDialogue : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject speakerPanel;
    [SerializeField] private float xOffsetSpeakerPanelLeft = -1319;
    [SerializeField] private float xOffsetSpeakerPanelRight = 0;
    [SerializeField] private float xOffsetSpeakerPanelCenter = -652f;
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private GameObject[] choices;
    [SerializeField] private TextMeshProUGUI[] choicesText;
    [SerializeField] string speakerTagPrefix = "speaker:";
    [SerializeField] string speakerLocationTagPrefix = "speakerLocation:";

    [SerializeField] List<InkleVariableWatch> variableWatches = new List<InkleVariableWatch>();
    [SerializeField] List<InkleTagWatch> tagWatches = new List<InkleTagWatch>();
    [SerializeField] StringListEvent onTagsFound = new StringListEvent();

    [SerializeField] UnityEvent onDialogueStarted;
    [SerializeField] UnityEvent onDialogueEnded;
    [SerializeField] UnityEvent onChoicesPresented;
    [SerializeField] UnityEvent onChoiceSelection;

    [SerializeField] bool playerMovementDisabledDuringDialogue = true;
    [SerializeField] bool playerMovementStopsDialogue = false;    

    //[SerializeField] private TextAsset inkJSON;

    // public get, private set
    public bool DialogueIsPlaying { get; private set; } = false;
    public bool DialoguePanelIsActive { get; private set; } = false;

    private Story currentStory = null;
    private string currentText = "";
    private List<Choice> currentChoices = null;
    private List<string> currentTags = new List<string>();
    bool speakerActive = false;

    private InputSystem_Actions inputActions;
    private Coroutine restoreControlsAfterDialogueCoroutine;


    void Awake()
    {
        inputActions = new InputSystem_Actions();

        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError("InkleDialogue: One or more UI components not assigned in inspector.");
        }
        if (choices == null || choicesText == null)
        {
            Debug.LogWarning("InkleDialogue: Choices or choices text not assigned in inspector. Choices will end dialogue.");
        }
        if (speakerPanel == null || displayNameText == null)
        {
            Debug.LogWarning("InkleDialogue: Speaker panel or display name text not assigned in inspector. Speaker panel will not be used.");
        }
        if (choices.Length != choicesText.Length)
        {
            Debug.LogError("InkleDialogue: Choices and choices text arrays must be the same length.");
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
        
        if (DialogueIsPlaying)
        {
            inputActions.Player.Interact.performed += OnInteractPerformed;
            inputActions.Player.Attack.performed += OnClickAnywhereToContinue;
        }
    }

    void OnDisable()
    {
        HideDialogueInterface();
        ResetState();
        inputActions.Player.Attack.performed -= OnClickAnywhereToContinue;
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        inputActions.Disable();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Start() {}

    // Update is called once per frame
    // void Update() {}

    public void StartDialogue(TextAsset inkStoryJSON)
    {
        StartDialogue(inkStoryJSON.text);
    }
    public void StartDialogue(string inkStoryJSON)
    {
        ResetState();
        DialogueIsPlaying = true;
        if (playerMovementDisabledDuringDialogue)
        {
            // disable player movement 
            // TODO: need to check this state in playerController scripts!
            GameManager.Instance.DisableAllControls();
        }
        currentStory = new Story(inkStoryJSON);
        SetupVariableListeners();
        ShowDialogueInterface();

        GameManager.Instance.ModalDialogueSetIsOpen();

        inputActions.Player.Attack.performed += OnClickAnywhereToContinue;
        inputActions.Player.Interact.performed += OnInteractPerformed;
        
        onDialogueStarted.Invoke();
        ContinueStory();        
    }

    public void EndDialogue()
    {
        if (!DialogueIsPlaying)
        {
            return;
        }
        inputActions.Player.Attack.performed -= OnClickAnywhereToContinue;
        inputActions.Player.Interact.performed -= OnInteractPerformed;
        DisableVariableListeners();
        currentStory = null;        
        onDialogueEnded.Invoke();
        HideDialogueInterface();
        DialogueIsPlaying = false;
        ResetState();
        GameManager.Instance.ModalDialogueSetIsClosed();
        if (playerMovementDisabledDuringDialogue) 
        {
            if (restoreControlsAfterDialogueCoroutine != null)
            {
                StopCoroutine(restoreControlsAfterDialogueCoroutine);
            }
            restoreControlsAfterDialogueCoroutine = StartCoroutine(RestoreControlsAfterDialogueInputRelease());
        }
    }

    private IEnumerator RestoreControlsAfterDialogueInputRelease()
    {
        yield return null;

        while (inputActions != null && (inputActions.Player.Attack.IsPressed() || inputActions.Player.Interact.IsPressed()))
        {
            yield return null;
        }

        GameManager.Instance.EnableAllControls();
        restoreControlsAfterDialogueCoroutine = null;
    }

    void ContinueStory()
    {
        // if "canContinue" it means there is more text and no choices
        if (currentStory.canContinue)
        {
            currentText = currentStory.Continue();
            dialogueText.text = currentText;
            InternalCheckTagsAndHandleSpecialTags();
            CheckTagsAndInvokeTagEvents();
            
            currentChoices = currentStory.currentChoices;
            // ShowChoices():
            if (currentChoices != null && currentChoices.Count > 0)
            {
                ShowChoiceUI(currentChoices.Count);
                for (int i = 0; i < currentChoices.Count; i++)
                {
                    choicesText[i].text = currentChoices[i].text;
                }
                onChoicesPresented.Invoke();
                // Select 1st choice by default.
                // this is done in ShowChoiceUI:
                //choices[0].GetComponent<UnityEngine.UI.Button>().Select();
                // optional, required in some versions of Unity?:
                //StartCoroutine(SelectFirstChoice());
            }
            else
            {
                HideChoiceUI();
            }
        }
        else
        {
            EndDialogue();
        }
    }

/*
    private IEnumerator SelectFirstChoice()
    {
        //Event System requires we clear it first, then wait
        //for at least one frame before we set the current selected object
        EventSystem.current.SetSelectedGameObject(null);
        yield return new WaitForEndOfFrame();
        EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
    }
*/

    public void MakeChoice(int choiceIndex)
    {
        if (currentChoices == null || choiceIndex < 0 || choiceIndex >= currentChoices.Count)
        {
            Debug.LogError("InkleDialogue: Invalid choice index: " + choiceIndex);
            return;
        }
        onChoiceSelection.Invoke();
        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }

    void TrySubmitSelectedChoice()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("InkleDialogue: No EventSystem found in scene. Cannot submit selected choice.");
            return;
        }
        GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
        if (selectedObject == null)
        {
            Debug.Log("InkleDialogue: No selected object. Cannot submit selected choice.");
            return;
        }
        for (int i = 0; i < choices.Length; i++)
        {
            if (selectedObject == choices[i])
            {
                MakeChoice(i);
                return;
            }
        }
    }

    void InternalCheckTagsAndHandleSpecialTags()
    {
        bool speakerFound = false;
        // handle special tags that have built-in functionality in this script, such as showing the speaker panel or setting the speaker location
        foreach (var tag in currentStory.currentTags)
        {
            Debug.Log("InkleDialogue: Checking tag: " + tag);

            if (tag.Trim().StartsWith(speakerTagPrefix))
            {
                string speakerName = tag.Substring(speakerTagPrefix.Length);
                if (string.IsNullOrEmpty(speakerName) || speakerName == "OFF")
                {
                    // if speaker name is empty or "OFF", hide speaker panel
                    speakerPanel.SetActive(false);
                    speakerActive = false;
                    continue;
                }
                else
                {
                    speakerPanel.SetActive(true);
                    speakerActive = true;
                    displayNameText.text = speakerName;
                    speakerFound = true;
                }
            }
            else if (tag.Trim().StartsWith(speakerLocationTagPrefix))
            {
                string location = tag.Substring(speakerLocationTagPrefix.Length);
                // set speaker panel location based on location string, e.g. "left", "right", "center"
                switch (location)
                {
                    case "left":
                        speakerPanel.transform.localPosition = new Vector3(xOffsetSpeakerPanelLeft, speakerPanel.transform.localPosition.y, speakerPanel.transform.localPosition.z);
                        break;
                    case "right":
                        speakerPanel.transform.localPosition = new Vector3(xOffsetSpeakerPanelRight, speakerPanel.transform.localPosition.y, speakerPanel.transform.localPosition.z);
                        break;
                    case "center":
                        speakerPanel.transform.localPosition = new Vector3(xOffsetSpeakerPanelCenter, speakerPanel.transform.localPosition.y, speakerPanel.transform.localPosition.z);
                        break;
                    default:
                        Debug.LogWarning("InkleDialogue: Unrecognized speaker location tag: " + tag);
                        break;
                }
            }
        }
        if (!speakerFound)
        {
            if (!speakerActive)
            {
                speakerPanel.SetActive(false);
            }
        }
    }


    public void ResetState()
    {
        DialogueIsPlaying = false;
        DialoguePanelIsActive = false;
        currentStory = null;
        currentChoices = null;
        currentText = "";
        currentTags.Clear();
        speakerActive = false;
    }


    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }

        if (!DialogueIsPlaying || currentStory == null)
        {
            //Debug.Log("Interact performed, but no dialogue is currently playing.");
            return;
        }

        if (GetCurrentChoicesCount() == 0)
        {
            Debug.Log("Continuing story with no choices available.");
            ContinueStory();
            return;
        }
        //Debug.Log("Attempting to submit selected choice on interact.");

        TrySubmitSelectedChoice();
    }
    private void OnClickAnywhereToContinue(InputAction.CallbackContext context)
    {
        if (GameManager.Instance.gameState.currentGameState == GameStates.Paused)
        {
            return;
        }
        if (!DialogueIsPlaying)
        {
            return;
        }
        // clicking outside of choices only continues story if
        // there are not choices
        if (GetCurrentChoicesCount() > 0)
        {
            return;
        }

        ContinueStory();
    }

#region UI Methods
    public void ShowDialogueInterface()
    {
        if (DialoguePanelIsActive)
        {
            return;
        }
        DialoguePanelIsActive = true;
        // enable interface
        dialoguePanel.SetActive(true);
        speakerPanel.SetActive(false);
        HideChoiceUI();
    }
    void ShowChoiceUI(int numChoices)
    {
        //onChoicesPresented.Invoke();
        for (int i = 0; i < choices.Length; i++)
        {
            if (i < numChoices)
            {
                choices[i].SetActive(true);
            }
            else
            {
                choices[i].SetActive(false);
            }
        }
        // select first choice by default
        if (numChoices > 0)
        {
            choices[0].GetComponent<UnityEngine.UI.Button>().Select();
            // The following might not work so a coroutine might be needed (see SelectFirstChoice coroutine)
            // EventSystem.current.SetSelectedGameObject(null);
            // EventSystem.current.SetSelectedGameObject(choices[0].gameObject);
        }
    }
    void HideChoiceUI()
    {
        foreach (var choice in choices)
        {
            choice.SetActive(false);
        }
    }
    public void HideDialogueInterface()
    {
        if (!DialogueIsPlaying || !DialoguePanelIsActive)
        {
            return;
        }
        dialoguePanel.SetActive(false);
        speakerPanel.SetActive(false);
        HideChoiceUI();
        DialoguePanelIsActive = false;
        //DialogueManager.GetInstance().HideDialogueInterface();
    }
#endregion UI Methods

#region Listeners for Ink story state changes
    void SetupVariableListeners()
    {
        currentStory.variablesState.variableChangedEvent += VariableChanged;
        // foreach (var variableWatch in variableWatches)
        // {
        //     currentStory.ObserveVariable(variableWatch.variableName, (variableName, newValue) =>
        //     {
        //         if (variableWatch.triggerValueOrAll == "all" || variableWatch.triggerValueOrAll == newValue.ToString())
        //         {
        //             variableWatch.onVariableMatch.Invoke(variableName, newValue.ToString());
        //         }
        //     });
        // }
    }
    void VariableChanged(string variableName, Ink.Runtime.Object newValue)
    {
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && (variableWatch.triggerValueOrAll == "all" || variableWatch.triggerValueOrAll == newValue.ToString()))
            {
                variableWatch.onVariableMatch.Invoke(variableName, newValue.ToString());
            }
        }
    }
    void DisableVariableListeners()
    {
        currentStory.variablesState.variableChangedEvent -= VariableChanged;
        // foreach (var variableWatch in variableWatches)
        // {
        //     currentStory.ObserveVariable(variableWatch.variableName, null);
        // }
    }
    void CheckTagsAndInvokeTagEvents()
    {
        currentTags = currentStory.currentTags;
        onTagsFound.Invoke(currentTags);
        foreach (var tag in currentTags)
        {
            foreach (var tagWatch in tagWatches)
            {
                if (tagWatch.tagName == tag)
                {
                    tagWatch.onTagFound.Invoke(tag);
                }
            }
        }
    }
#endregion Listeners for Ink story state changes

#region Player Movement Options
    public bool IsPlayerMovementDisabledDuringDialogue()
    {
        return playerMovementDisabledDuringDialogue;
    }
    public bool DoesPlayerMovementStopDialogue()
    {
        return playerMovementStopsDialogue;
    }
    public void DisablePlayerMovementDuringDialogue()
    {
        playerMovementDisabledDuringDialogue = true;
        if (DialogueIsPlaying)
        {
            GameManager.Instance.DisableAllControls();
        }
    }
    public void EnablePlayerMovementDuringDialogue()
    {
        if (DialogueIsPlaying)
        {
            GameManager.Instance.EnableAllControls();
        }
        playerMovementDisabledDuringDialogue = false;
    }
    public void SetPlayerMovementStopsDialogue(bool stops)
    {
        playerMovementStopsDialogue = stops;
    }
#endregion Player Movement Options

#region Listeners for dialogue events

    public void AddTagListener(UnityAction<string> action, string tagName)
    {
        foreach (var tagWatch in tagWatches)
        {
            if (tagWatch.tagName == tagName)
            {
                tagWatch.onTagFound.AddListener(action);
                return;
            }
        }
        // no current tag watch for this tag, so add one
        var newTagWatch = new InkleTagWatch
        {
            tagName = tagName,
            onTagFound = new StringEvent()
        };
        tagWatches.Add(newTagWatch);
        newTagWatch.onTagFound.AddListener(action);
    }
    public void RemoveTagListener(UnityAction<string> action, string tagName)
    {
        foreach (var tagWatch in tagWatches)
        {
            if (tagWatch.tagName == tagName)
            {
                tagWatch.onTagFound.RemoveListener(action);
                return;
            }
        }
    }
    public void AddTagsListener(UnityAction<List<string>> action)
    {
        onTagsFound.AddListener(action);
    }
    public void RemoveTagsListener(UnityAction<List<string>> action)
    {
        onTagsFound.RemoveListener(action);
    }

    public void AddVariableListener(UnityAction<string, string> action, string variableName, string triggerValueOrAll = "all")
    {
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && variableWatch.triggerValueOrAll == triggerValueOrAll)
            {
                variableWatch.onVariableMatch.AddListener(action);
                return;
            }
        }
        // no current variable watch for this variable and trigger value, so add one
        var newVariableWatch = new InkleVariableWatch
        {
            variableName = variableName,
            triggerValueOrAll = triggerValueOrAll,
            onVariableMatch = new TwoStringEvent()
        };
        variableWatches.Add(newVariableWatch);
        newVariableWatch.onVariableMatch.AddListener(action);
    }
    public void RemoveVariableListener(UnityAction<string, string> action, string variableName, string triggerValueOrAll = "all")
    {
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && variableWatch.triggerValueOrAll == triggerValueOrAll)
            {
                variableWatch.onVariableMatch.RemoveListener(action);
                return;
            }
        }
    }
    public void AddDialogueStartedListener(UnityAction action)
    {
        onDialogueStarted.AddListener(action);
    }
    public void RemoveDialogueStartedListener(UnityAction action)
    {
        onDialogueStarted.RemoveListener(action);
    }
    public void AddDialogueEndedListener(UnityAction action)
    {
        onDialogueEnded.AddListener(action);
    }
    public void RemoveDialogueEndedListener(UnityAction action)
    {
        onDialogueEnded.RemoveListener(action);
    }
    public void AddChoiceSelectionListener(UnityAction action)
    {
        onChoiceSelection.AddListener(action);
    }
    public void RemoveChoiceSelectionListener(UnityAction action)
    {
        onChoiceSelection.RemoveListener(action);
    }
#endregion Listeners for dialogue events

#region Queries for current dialogue state
    List<string> GetCurrentTags()
    {
        return currentTags;
    }
    string GetCurrentText()
    {
        return currentText;
    }
    List<Choice> GetCurrentChoices()
    {
        return currentChoices;
    }
    int GetCurrentChoicesCount()
    {
        if (currentChoices == null)
        {
            return 0;
        }
        return currentChoices.Count;
    }
    string[] GetCurrentChoicesAsStrings()
    {
        if (currentChoices == null)
        {
            return new string[0];
        }
        string[] choiceStrings = new string[currentChoices.Count];
        for (int i = 0; i < currentChoices.Count; i++)
        {
            choiceStrings[i] = currentChoices[i].text;
        }
        return choiceStrings;
    }
    object GetVariableValue(string variableName)
    {
        if (currentStory == null)
        {
            return "";
        }
        return currentStory.variablesState[variableName];
    }
#endregion Queries for current dialogue state

#region Set variable value
    void SetVariableValue(string variableName, object value)
    {
        if (currentStory == null)
        {
            return;
        }
        currentStory.variablesState[variableName] = value;
        // check if any variable watches should be triggered by this variable change
        foreach (var variableWatch in variableWatches)
        {
            if (variableWatch.variableName == variableName && (variableWatch.triggerValueOrAll == "all" || variableWatch.triggerValueOrAll == value.ToString()))
            {
                variableWatch.onVariableMatch.Invoke(variableName, value.ToString());
            }
        }
    }
#endregion Set variable value


}
