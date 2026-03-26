using UnityEngine;

public class NpcFollow : MonoBehaviour
{
    private bool playerInRange;

    [SerializeField] private GameObject prompt;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject visualCue;
    [SerializeField] private Animator animator;
    Follow com;

    private void Start()
    {
        com = GetComponent<Follow>();
        animator = GetComponent<Animator>();
        transform.LookAt(target);

    }

    void Update()
    {
        if (playerInRange)
        {
            transform.LookAt(target);
            if (Input.GetKeyDown(KeyCode.E))
            {
                Destroy(prompt);
                Destroy(visualCue);
                prompt.gameObject.SetActive(false);
                com.enabled = true;
            }
        }
        else
        {
            
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            animator.SetBool("Bounce", true);
            visualCue.SetActive(false);
            prompt.gameObject.SetActive(true);
            playerInRange = true;
        }

    }

    private void OnTriggerExit(Collider other)
    {
        prompt.gameObject.SetActive(false);
        visualCue.SetActive(true);
        animator.SetBool("Bounce", false);

        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
