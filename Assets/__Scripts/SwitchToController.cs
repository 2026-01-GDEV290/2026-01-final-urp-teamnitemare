using UnityEngine;

public class SwitchToController : MonoBehaviour
{

    [SerializeField] private GameObject autoDialogue;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject cutsceneCamera;
    [SerializeField] private Animator birdFlee;
    [SerializeField] private GameObject bird;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!autoDialogue.activeInHierarchy)
        {
            Invoke(nameof(BirdFlyingDelay), 3f); 

            if (bird == null)
            {
            Invoke(nameof(SwitchToPlayer), 3f);
            }
        }

    }

    void SwitchToPlayer()
    {
        player.SetActive(true);
        cutsceneCamera.SetActive(false);

    }

    void BirdFlyingDelay()
    {
        birdFlee.SetBool("flying", true);
    }

}
