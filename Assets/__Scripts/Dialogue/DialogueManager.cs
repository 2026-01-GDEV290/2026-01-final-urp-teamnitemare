using UnityEngine;
using TMPro;
using Ink.Runtime;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]

    [SerializeField] private GameObject dialoguePanel;

    [SerializeField] private TextMeshProUGUI dialogueText;

    private Story currentStory;

    public bool dialogueIsPlaying { get; private set; }

    private static DialogueManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Found more than one Dialogue Manager in the Scene");
        }
        instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        //return right away if dialogue isn't playing
        if (!dialogueIsPlaying)
        {
            return;
        }

        //handle continuing to the next line in the dialog when interact is pressed
        if (Input.GetKeyUp(KeyCode.E))
        {
            ContinueStory();
        }
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        dialoguePanel.SetActive(true);

        ContinueStory();
    }

    private void ExitDialogueMode()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);
        dialogueText.text = "";
    }

    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            dialogueText.text = currentStory.Continue();
        }
        else
        {
            ExitDialogueMode();
        }
    }
}
