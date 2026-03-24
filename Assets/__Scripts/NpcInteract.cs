using UnityEngine;

public class NpcInteract : MonoBehaviour
{
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        if (DialogueManager.GetInstance().dialogueIsPlaying)
        {
            animator.SetBool("Bounce", true);
        }
        else
        {
            animator.SetBool("Bounce", false);
        }
    }
}

