using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Serialization;

// TODO: Visit-specific events (onSceneAwake, onSceneStart), which can also
// enable/disable SceneTaskGroups for a given visit and set up visit-specific scene info

[Serializable]
class SceneVisitTasks
{
    public int visitCount = 0;
    [SerializeField] public UnityEvent onSceneAwake;
    [SerializeField] public UnityEvent onSceneStart;
}

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
[Serializable]
public class Scene : MonoBehaviour
{
    [Header("Scene Visit Groups")]
    [SerializeField] List<SceneVisitTasks> sceneVisitTasks = new List<SceneVisitTasks>()
    {
        new SceneVisitTasks { visitCount = 1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() }
    };

    string sceneName = "";  // set at Awake from SceneManager.GetActiveScene().name

    private UnityEvent onSceneAwake;
    private UnityEvent onSceneStart;

    
    void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;

        GameManager.Instance.SceneAwake(this);

        int visitCount = GameManager.Instance.gameState.GetSceneVisitCount(sceneName);
        Debug.Log("Scene->Awake: Scene (after GM->SceneAwake): " + sceneName + ", Visit Count: " + visitCount + ", #Awake Events: " + onSceneAwake.GetPersistentEventCount() + ", #Start Events: " + onSceneStart.GetPersistentEventCount());

        SceneVisitTasks visitTasks = sceneVisitTasks.Find(s => s.visitCount == visitCount);
        if (visitTasks != null)
        {
            onSceneAwake = visitTasks.onSceneAwake;
            onSceneStart = visitTasks.onSceneStart;
        }
        else
        {
            Debug.LogWarning("Scene->Awake: No SceneVisitTasks found for Scene: " + sceneName + ", Visit Count: " + visitCount);
            onSceneAwake = new UnityEvent();
            onSceneStart = new UnityEvent();
        }

        onSceneAwake.Invoke();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.SceneStart();

        onSceneStart.Invoke();
    }

    // Update is called once per frame
    //void Update() { }

    void OnDestroy()
    {
        GameManager.Instance.SceneDestroyed();
    }

#region Scene Progression
    // For use with Events in the inspector:
    public void LoadNextScene()
    {
        GameManager.Instance.LoadNextScene();
    }
    public void LoadScene(Scenes scene)
    {
        GameManager.Instance.LoadScene(scene);
    }
    // TODO: minigames
    // public void LoadMinigameScene(Scenes minigameScene)
    // {
    //     // Set up logic - could have GM.minigameLoaded field
    //     GameManager.Instance.LoadMinigameScene(minigameScene);
    // }

    public void ReloadScene()
    {
        GameManager.Instance.ReloadCurrentScene();
    }

    // Restart doesn't increment visit counter, but does reset scene progress
    public void RestartScene()
    {
        GameManager.Instance.RestartCurrentScene();
    }

#endregion Scene Progression


// TODO: Remove these? (just drag object into events and set directly in inspector)
#region Interaction Helpers

    public void TriggerableSetIsActive(GameObject go, bool active)
    {
        Triggerable triggerable = go.GetComponent<Triggerable>();
        if (triggerable == null)
        {
            Debug.LogError("TriggerableSetIsActive: GameObject: " + go.name + " does not have a Triggerable component.");
            return;
        }
        else
        {
            triggerable.SetIsActive(active);
        }
    }
    public void TriggerableSetIsActive(GameObject go)
    {
        TriggerableSetIsActive(go, true);
    }
    public void TriggerableSetIsInactive(GameObject go)
    {
        TriggerableSetIsActive(go, false);
    }
    public void InteractableSetIsInteractable(GameObject go, bool isInteractable)
    {
        InteractableBase interactable = go.GetComponent<InteractableBase>();
        if (interactable == null)
        {
            Debug.LogError("InteractableSetIsInteractable: GameObject: " + go.name + " does not have an InteractableBase component.");
            return;
        }
        else
        {
            interactable.SetIsInteractable(isInteractable);
        }
    }
    public void InteractableSetIsInteractable(GameObject go)
    {
        InteractableSetIsInteractable(go, true);
    }
    public void InteractableSetIsNonInteractable(GameObject go)
    {
        InteractableSetIsInteractable(go, false);
    }

    public void TimerSetIsEnabled(GameObject go, bool enabled)
    {
        TimerObject timerObject = go.GetComponent<TimerObject>();
        if (timerObject == null)
        {
            Debug.LogError("TimerSetIsEnabled: GameObject: " + go.name + " does not have a TimerObject component.");
            return;
        }
        else
        {
            timerObject.SetIsEnabled(enabled);
        }
    }
    public void TimerSetIsEnabled(GameObject go)
    {
        TimerSetIsEnabled(go, true);
    }
    public void TimerSetIsDisabled(GameObject go)
    {
        TimerSetIsEnabled(go, false);
    }
    public void TimerReset(GameObject go)
    {
        TimerObject timerObject = go.GetComponent<TimerObject>();
        if (timerObject == null)
        {
            Debug.LogError("TimerReset: GameObject: " + go.name + " does not have a TimerObject component.");
            return;
        }
        else
        {
            timerObject.ResetTimer();
        }
    }
    // Can't expose in Unity (2 parameters) - in this case, call Timer object's SetDuration directly
    //public void TimerSetDuration(GameObject go, float duration)

#endregion Interaction Helpers

}
