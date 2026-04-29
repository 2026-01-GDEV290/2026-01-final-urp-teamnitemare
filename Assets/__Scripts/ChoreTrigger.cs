using UnityEngine;
using TMPro;

public class ChoreTrigger : MonoBehaviour
{

    private bool playerInRange;
   
    [SerializeField] private GameObject prompt;

    [SerializeField] private GameObject choreObject;

    [SerializeField] private GameObject choreSound;

    [SerializeField] private GameObject fade;

    void Update()
    {
        if (playerInRange)
        {

            if (Input.GetKeyDown(KeyCode.E))
            {
                //question.gameObject.SetActive(true);
                prompt.gameObject.SetActive(false);
                fade.gameObject.SetActive(true);
                choreSound.gameObject.SetActive(true);
                Invoke(nameof(ClearChore), 1f);
            }
        }
        else
        {
            //question.gameObject.SetActive(false);
        }
    }

    void ClearChore()
    {
        choreObject.gameObject.SetActive(false);

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            prompt.gameObject.SetActive(true);
            fade.gameObject.SetActive(false);
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
