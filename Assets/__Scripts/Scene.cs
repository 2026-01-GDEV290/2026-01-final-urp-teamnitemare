using UnityEngine;

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
public class Scene : MonoBehaviour
{
    void Awake()
    {
        GameManager.Instance.SceneAwake();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.SceneStart();
    }

    // Update is called once per frame
    //void Update() { }

    void OnDestroy()
    {
        GameManager.Instance.SceneDestroyed();
    }
}
