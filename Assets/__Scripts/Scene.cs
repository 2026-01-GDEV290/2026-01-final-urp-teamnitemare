using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
public class Scene : MonoBehaviour
{
    public List<GameObject> SceneTaskObjects = new List<GameObject>();
    string sceneName = "";

    public UnityEngine.Events.UnityEvent onTasksCompleted;
    
    void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;
        Debug.Log("Scene->Awake: " + SceneTaskObjects.Count + " tasks");
        GameManager.Instance.SceneAwake(this);
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
    public void RemoveTaskObjectAndActOnComplete(GameObject go)
    {
        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (SceneTaskObjects.Count - 1));
        // Find sceneProgressionInfo for current scene and add this task to the list of completed objectives
        if (GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(sceneName))
        {
            if (!GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Contains(go.name))
            {
                GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Add(go.name);
                Debug.Log("Added " + go.name + " to completed objectives for scene: " + sceneName);
            }
            else
            {
                Debug.LogWarning("Objective " + go.name + " already marked as completed for scene: " + sceneName);
            }
        }
        else
        {
            Debug.LogError("No sceneProgressionInfo found for scene: " + sceneName);
        }
        
        SceneTaskObjects.Remove(go);

        ActOnAllTasksComplete();

    }

    public void ActOnAllTasksComplete()
    {
        if (SceneTaskObjects.Count == 0)
        {
            Debug.Log("All tasks completed for scene: " + sceneName);
            onTasksCompleted.Invoke();
        }
        else
        {
            Debug.Log("Tasks remaining for scene: " + sceneName + ": " + SceneTaskObjects.Count);
        }
    }

    public void ForceCompleteAllTasks(bool actOnComplete = true)
    {
        Debug.Log("Force completing all tasks for scene: " + sceneName);
        SceneTaskObjects.Clear();
        if (actOnComplete)
        {
            onTasksCompleted.Invoke();
        }
    }

    public void ClearAllTasks()
    {
        Debug.Log("Clearing all tasks for scene: " + sceneName);
        SceneTaskObjects.Clear();
    }
}
