using UnityEngine;

public class NpcFollow : MonoBehaviour
{
    private bool playerInRange;

    [SerializeField] private GameObject prompt;

    Follow com;

    //[SerializeField] private GameObject player;

    //[SerializeField] private float speed = 1.5f;

    private void Start()
    {
        com = GetComponent<Follow>();
    }

    void Update()
    {
        if (playerInRange)
        {

            if (Input.GetKeyDown(KeyCode.E))
            {
                Destroy(prompt);
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
            prompt.gameObject.SetActive(true);
            playerInRange = true;

        }


    }

    private void OnTriggerExit(Collider other)
    {
        prompt.gameObject.SetActive(false);

        if (other.gameObject.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }
}
