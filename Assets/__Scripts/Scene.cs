using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
public class Scene : MonoBehaviour
{
    public List<GameObject> SceneTaskObjects = new List<GameObject>();
    string sceneName = "";

    // when RemoveTaskObject() called, this will force a check of sceneTaskObjects
    // and invoke onTasksCompleted if all tasks are complete/sceneTaskObjects is empty
    public bool alwaysActOnTasksComplete = false;

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

    private void UpdateGameStateProgressionForCompletedTask(string taskName)
    {
        // Find sceneProgressionInfo for current scene and add this task to the list of completed objectives
        if (GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(sceneName))
        {
            if (!GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Contains(taskName))
            {
                GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Add(taskName);
                Debug.Log("Added " + taskName + " to completed objectives for scene: " + sceneName);
            }
            else
            {
                Debug.LogWarning("Objective " + taskName + " already marked as completed for scene: " + sceneName);
            }
        }
        else
        {
            Debug.LogError("No sceneProgressionInfo found for scene: " + sceneName);
        }
    }

    public void RemoveTaskObject(GameObject go)
    {
        if (alwaysActOnTasksComplete)
        {
            RemoveTaskObjectAndActOnComplete(go);
            return;
        }
        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (SceneTaskObjects.Count - 1));
        // Update game state progression for this completed task
        UpdateGameStateProgressionForCompletedTask(go.name);
        SceneTaskObjects.Remove(go);
    }

    public void RemoveTaskObjectAndActOnComplete(GameObject go)
    {
        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (SceneTaskObjects.Count - 1));
        // Update game state progression for this completed task
        UpdateGameStateProgressionForCompletedTask(go.name);
        
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
        // Update game state progression for all remaining tasks
        foreach (var go in SceneTaskObjects)
        {
            UpdateGameStateProgressionForCompletedTask(go.name);
        }
        SceneTaskObjects.Clear();
        if (actOnComplete)
        {
            onTasksCompleted.Invoke();
        }
    }

    public void ClearAllTasks()
    {
        Debug.Log("Clearing all tasks for scene (with NO progression updates): " + sceneName);
        SceneTaskObjects.Clear();
    }

    public void CompleteNonTaskObject(GameObject go)
    {
        Debug.Log("Completing non-task object: " + go.name + " for scene: " + sceneName);
        UpdateGameStateProgressionForCompletedTask(go.name);
    }

    public void ActOnGivenTasksComplete(List<GameObject> taskObjects)
    {
        // Check if all given task objects are in the gameState's sceneProgressionInfo for this scene, and if so, remove them and act on completion. If any are not, log a warning and return without doing anything
        if (!GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(sceneName))
        {
            Debug.LogWarning("Cannot act on tasks complete for scene: " + sceneName + " because no progression info found for this scene in game state.");
            return;
        }
        foreach (var go in taskObjects)
        {
            if (!GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Contains(go.name))
            {
                Debug.LogWarning("Task " + go.name + " is not marked as completed for scene: " + sceneName + " in game state progression info.");
                return;
            }
        }
        // If we made it here, all tasks are completed, so we can act on completion
        onTasksCompleted.Invoke();
    }
    public void ActOnGivenTaskComplete(GameObject go)
    {
        // Check if this task object is in the gameState's sceneProgressionInfo for this scene, and if so, remove it and act on completion. If not, log a warning and return without doing anything
        if (!GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(sceneName))
        {
            Debug.LogWarning("Cannot act on task complete for scene: " + sceneName + " because no progression info found for this scene in game state.");
            return;
        }
        if (!GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Contains(go.name))
        {
            Debug.LogWarning("Task " + go.name + " is not marked as completed for scene: " + sceneName + " in game state progression info.");
            return;
        }
        // If we made it here, the task is completed, so we can act on completion
        onTasksCompleted.Invoke();
    }
}
