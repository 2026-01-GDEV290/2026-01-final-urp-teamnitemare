using UnityEngine;

public class Follow : MonoBehaviour
{
    [SerializeField] private float speed = 1.5f;
    public AudioClip getSound;
    public float volume = 1.0f;

    [SerializeField] private GameObject followBox;

    [SerializeField] private Animator animator;


    private void Awake()
    {
        animator = GetComponent<Animator>();

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        followBox = GameObject.FindGameObjectWithTag("FollowBox");
        AudioSource.PlayClipAtPoint(getSound, transform.position, volume);
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, followBox.transform.position, speed * Time.deltaTime);
        // Animaton
        animator.SetBool("Fly", true);
        animator.SetBool("Idle_A", false);

    }
}
