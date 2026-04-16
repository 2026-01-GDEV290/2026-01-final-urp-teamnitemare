using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Unity.VisualScripting;

// TODO: 'hubconnectedscene' minigames (going IN to a minigame scene, returing BACK to this scene)
// TODO: Restart should reset tasks/quests, which means using visit # in GameState
// (or just have 1 visit)


[Serializable]
class SceneVisitTasks
{
    public int visitCount = 0;
    [SerializeField] public UnityEvent onSceneAwake;
    [SerializeField] public UnityEvent onSceneStart;
    [SerializeField] public bool useIfVisitCountExceeded = false; // if true, will use this SceneVisitTasks if visitCount exceeds the specified visitCount (instead of doing nothing)
}

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
[DefaultExecutionOrder(-100)]
public class Scene : MonoBehaviour
{
    [Header("Scene Visit Groups")]
    [SerializeField] List<SceneVisitTasks> sceneVisitTasks = new List<SceneVisitTasks>()
    {
        new SceneVisitTasks { visitCount = 1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() }
    };
    [SerializeField] SceneVisitTasks onHubReturnTasks = new SceneVisitTasks() { visitCount = -1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() };

    [SerializeField] SceneVisitTasks onReloadTasks = new SceneVisitTasks() { visitCount = -1, onSceneAwake = new UnityEvent(), onSceneStart = new UnityEvent() };

    [SerializeField] public Transform hubReturnPosition = null;
    [SerializeField] public GameObject hubReturnPlayerObject = null;

    string sceneName = "";  // set at Awake from SceneManager.GetActiveScene().name

    private UnityEvent onSceneAwake;
    private UnityEvent onSceneStart;

    private QuestManager questManager;

    private int visitCount;

    public int VisitCount => visitCount;

    private bool sceneWasReloaded = false;
    public bool SceneWasReloaded => sceneWasReloaded;
    bool sceneWasRestarted = false;
    public bool SceneWasRestarted => sceneWasRestarted;
    bool returnedToHubFromScene = false;
    public bool ReturnedToHubFromScene => returnedToHubFromScene;

    
    void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;

        // save reload/restart/hub-return state from GameManager (it resets them in SceneAwake, and adjusts visit count)
        sceneWasReloaded = GameManager.Instance.reloadCurrentSceneCalled;
        sceneWasRestarted = GameManager.Instance.restartCurrentSceneCalled;
        returnedToHubFromScene = GameManager.Instance.hubSubSceneVisited;

        GameManager.Instance.SceneAwake(this);

        visitCount = GameManager.Instance.gameState.GetSceneVisitCount(sceneName);
        Debug.Log("Scene->Awake: Scene (after GM->SceneAwake): " + sceneName + ", Visit Count: " + visitCount);

        questManager = FindFirstObjectByType<QuestManager>();
        if (questManager == null)
        {
            GameObject questManagerGO = new GameObject("QuestManager");
            questManager = questManagerGO.AddComponent<QuestManager>();
        }
        questManager.Initialize(this);

        if (returnedToHubFromScene)
        {
            onSceneAwake = onHubReturnTasks.onSceneAwake;
            onSceneStart = onHubReturnTasks.onSceneStart;
            Debug.Log("Scene->Awake: Hub return visit to Scene: " + sceneName + ", using onHubReturnTasks");
            if (hubReturnPosition != null && hubReturnPlayerObject != null)
            {
                hubReturnPlayerObject.transform.position = hubReturnPosition.position;
                Debug.Log("Scene->Awake: Moved player to hub return position: " + hubReturnPosition.position);
            }
            else
            {
                Debug.LogWarning("Scene->Awake: Hub return position or player object not set for Scene: " + sceneName);
            }
        }
        else    // non-hub visit
        {
            // Restart means resetting all progress in the scene but not incrementing visit count
            if (sceneWasRestarted)
            {
                questManager.ClearTasksAndQuestsForScene();
                // No that will stil be a problem 
                //GameManager.Instance.gameState.ResetSceneProgress(sceneName);
                //Debug.Log("Scene->Awake: Restart visit to Scene: " + sceneName + ", Restoring scene state");
            }
            else if (sceneWasReloaded)
            {
                // Just restore scene state, don't reset progress or increment visit count
                //SaveManager.Instance.RestoreTemporarySceneState();
                //Debug.Log("Scene->Awake: Reload visit to Scene: " + sceneName + ", Restoring scene state");
            }
            

            SceneVisitTasks visitTasks = sceneVisitTasks.Find(s => s.visitCount == visitCount);
            if (visitTasks != null)
            {
                Debug.Log("Scene->Awake: Found SceneVisitTasks for Scene: " + sceneName + ", Visit Count: " + visitCount);
                onSceneAwake = visitTasks.onSceneAwake;
                onSceneStart = visitTasks.onSceneStart;
            }
            else
            {
                // check for 'useIfVisitCountExceeded' tasks moving from last to first in list
                visitTasks = sceneVisitTasks.FindLast(s => s.useIfVisitCountExceeded && s.visitCount <= visitCount);
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
            }
        }
 

        onSceneAwake.Invoke();
        if (SceneWasReloaded)
        {
            onReloadTasks.onSceneAwake.Invoke();
        }
    }

    void OnEnable()
    {
        Debug.Log("Scene->OnEnable: Scene: " + sceneName);
        if (sceneWasReloaded || returnedToHubFromScene)
        {
            SaveManager.Instance.RestoreTemporarySceneState();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.SceneStart();

        onSceneStart.Invoke();
        if (SceneWasReloaded)
        {
            onReloadTasks.onSceneStart.Invoke();
        }
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
    // public void LoadHubConnectedScene(Scenes hubConnectedScene)
    // {
    //     // Set up logic - could have GM.minigameLoaded field
    //     GameManager.Instance.LoadHubConnectedScene(hubConnectedScene);
    // }

    // increments visit count, and restores scene state
    public void ReloadScene()
    {
        SaveManager.Instance.CaptureTemporarySceneState();
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
