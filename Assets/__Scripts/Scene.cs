using System.Collections.Generic;
using UnityEngine;

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
public class Scene : MonoBehaviour
{
    public List<GameObject> SceneTaskObjects;
    
    void Awake()
    {
        Debug.Log("Scene->Awake: " + SceneTaskObjects.Count + " tasks");
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

    // For use with Events in the inspector:
    public void LoadNextScene()
    {
        GameManager.Instance.LoadNextScene();
    }
    public void LoadScene(Scenes scene)
    {
        GameManager.Instance.LoadScene(scene);
    }

    public void AddTaskObject(GameObject go)
    {
        SceneTaskObjects.Add(go);
    }
    public void RemoveTaskObject(GameObject go)
    {
        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (SceneTaskObjects.Count - 1));
        SceneTaskObjects.Remove(go);
    }
    public void CompleteLevelOnTasksCompleted()
    {
        if (SceneTaskObjects.Count == 0)
        {
            Debug.Log("All tasks completed. Loading next scene.");
            LoadNextScene();
        }
    }

    public void EnableObjectOnTasksCompleted(GameObject go)
    {
        if (SceneTaskObjects.Count == 0)
        {
            Debug.Log("All tasks completed. Enabling object: " + go.name);
            go.SetActive(true);
        }
    }
}
