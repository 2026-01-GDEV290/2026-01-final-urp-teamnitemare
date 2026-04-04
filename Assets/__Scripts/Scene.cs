using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

// TODO: TaskGroups -> SceneTaskGroups object which this can interact with
// TODO: Visit-specific events (onSceneAwake, onSceneStart), which can also
// enable/disable SceneTaskGroups for a given visit and set up visit-specific scene info

[Serializable]
public class TaskGroup
{
    public string taskGroupName = "";
    public bool completePriorGroupFirst = false;
    public List<GameObject> taskObjects = new List<GameObject>();
    public bool autoActOnComplete = false;
    public UnityEngine.Events.UnityEvent onTasksCompleted;
    //[HideInInspector] // no, still want it to show just not be editable (requires custom Editor code)
    public bool actedOnComplete = false;
}

// This script is mandatory to attach to each Scene. Easiest is to attach it to the Main Camera
// It communicates with the GameManager currently
[Serializable]
public class Scene : MonoBehaviour
{
    [SerializeField] UnityEvent onSceneAwake;
    [SerializeField] UnityEvent onSceneStart;
    //public List<GameObject> SceneTaskObjects = new List<GameObject>();
    [SerializeField] private List<TaskGroup> taskGroups = new List<TaskGroup>();
    int taskGroupsCompleted = 0;
    string sceneName = "";

    // when RemoveTaskObject() called, this will force a check of sceneTaskObjects
    // and invoke onTasksCompleted if all tasks are complete/sceneTaskObjects is empty
    //public bool alwaysActOnAllTasksComplete = false;

    //public UnityEngine.Events.UnityEvent onTasksCompleted;

    int GetTotalTasksInAllGroups()
    {
        int total = 0;
        foreach (var group in taskGroups)
        {
            total += group.taskObjects.Count;
        }
        return total;
    }
    
    void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;
        Debug.Log("Scene->Awake: " + taskGroups.Count + " task groups with " + GetTotalTasksInAllGroups() + " total tasks");
        GameManager.Instance.SceneAwake(this);
        // TODO: This will have to become visit-specific
        onSceneAwake.Invoke();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.Instance.SceneStart();
        // TODO: This will have to become visit-specific
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

    public void ReloadScene(bool incrementVisitCounter = true)
    {
        // GameManager.Instance.gameState.sceneVisitCounts[sceneName] += incrementVisitCounter ? 1 : 0;
        GameManager.Instance.ReloadCurrentScene();
    }
#endregion Scene Progression

#region Task-Group Manage

    // Value changed in Inspector
    // Since inspector duplicates last element, we'll undo this
    void OnValidate()
    {
        if (taskGroups == null || taskGroups.Count == 0)
            return;
        if (taskGroups.Count == 1)
        {
            if (string.IsNullOrEmpty(taskGroups[0].taskGroupName))
            {
                taskGroups[0].taskGroupName = "TaskGroup1";
            }
            return;
        }
        // more than 1 task group. Just check for new addition with duplicate name
        TaskGroup lastGroup = taskGroups[taskGroups.Count - 2];
        TaskGroup newGroup = taskGroups[taskGroups.Count - 1];
        if (newGroup.taskGroupName == lastGroup.taskGroupName)
        {
            // rename with TaskGroup + index
            newGroup.taskGroupName = "TaskGroup" + taskGroups.Count;
            // check if all the tasks are the same
            if (newGroup.taskObjects.Count == lastGroup.taskObjects.Count)
            {
                bool allTasksSame = true;
                for (int i = 0; i < newGroup.taskObjects.Count; i++)
                {
                    if (newGroup.taskObjects[i] != lastGroup.taskObjects[i])
                    {
                        allTasksSame = false;
                        break;
                    }
                }
                if (allTasksSame)
                {
                    // delete all tasks from new group
                    newGroup.taskObjects.Clear();
                    // and set defaults for other fields
                    newGroup.completePriorGroupFirst = false;
                    newGroup.autoActOnComplete = false;
                    newGroup.onTasksCompleted = new UnityEvent();
                }
            }
        }
    }

    public void AddTaskObject(string taskObjectGroupName, GameObject go)
    {
        TaskGroup group = taskGroups.Find(g => g.taskGroupName == taskObjectGroupName);
        if (group == null)
        {
            Debug.LogError("AddTaskObject: No TaskGroup found with name: " + taskObjectGroupName);
            return;
        }
        group.taskObjects.Add(go);
    }

    public void AddTaskObject(int groupIndex, GameObject go)
    {
        if (groupIndex < 0 || groupIndex >= taskGroups.Count)
        {
            Debug.LogError("AddTaskObject: Invalid group index: " + groupIndex);
            return;
        }
        taskGroups[groupIndex].taskObjects.Add(go);
    }
    public int AddTaskGroup(string name, UnityAction onCompleted, bool completePriorGroupFirst = false, bool autoActOnComplete = false)
    {
        if (taskGroups.Exists(g => g.taskGroupName == name))
        {
            Debug.LogWarning("AddTaskGroup: TaskGroup with name: " + name + " already exists.");
            // return index of existing group with this name
            int existingGroupIndex = taskGroups.FindIndex(g => g.taskGroupName == name);
            return existingGroupIndex;
        }
        taskGroups.Add(new TaskGroup {
            taskGroupName = name, completePriorGroupFirst = completePriorGroupFirst, autoActOnComplete = autoActOnComplete,
            onTasksCompleted = new UnityEvent()
        });
        taskGroups[taskGroups.Count - 1].onTasksCompleted.AddListener(onCompleted);
        return taskGroups.Count - 1;
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

    private int FindTaskGroupIndexForTaskObject(GameObject go)
    {
        int groupIndex = taskGroups.FindIndex(g => g.taskObjects.Contains(go));
        if (groupIndex == -1)
        {
            //Debug.LogError("No TaskGroup found containing task object: " + go.name);
            return -1;
        }
        return groupIndex;
    }

    // INTERNAL: index must be valid
    private bool IsItOkayToActOnComplete(int groupIndex)
    {
        Debug.Log("Checking if it's okay to act on complete for group: " + taskGroups[groupIndex].taskGroupName + " in scene: " + sceneName);
        Debug.Log("Group has " + taskGroups[groupIndex].taskObjects.Count + " tasks remaining.");

        // still tasks left?
        if (taskGroups[groupIndex].taskObjects.Count > 0)
            return false;
        
        // no tasks left..

        // acted already?
        if (taskGroups[groupIndex].actedOnComplete)
            return false;

        if (groupIndex == 0)    // no need to check prior groups
            return true;

        // index 1+, complete prior groups? if not, we are okay
        if (!taskGroups[groupIndex].completePriorGroupFirst)
        {
            return true;
        }

        // else: check previous groups
        for (int i = groupIndex - 1; i >= 0; i--)
        {
            if (taskGroups[i].taskObjects.Count > 0)
            {
                Debug.Log("Previous group: " + taskGroups[i].taskGroupName + " still has tasks remaining.");
                return false;
            }
            // if prior group has 0 tasks left, and doesn't require
            // previous groups to be completed, we can return true here
            if (!taskGroups[i].completePriorGroupFirst)
                return true;
        }
        // if we made it here, all prior groups have 0 tasks left, so we can return true
        return true;
    }

    public void RemoveTaskObject(GameObject go)
    {
        // Get group index
        int groupIndex = FindTaskGroupIndexForTaskObject(go);
        if (groupIndex == -1)
        {
            Debug.LogError("RemoveTaskObject: No TaskGroup found containing task object: " + go.name);
            return;
        }

        TaskGroup group = taskGroups[groupIndex];

        if (group.autoActOnComplete)
        {
            RemoveTaskObjectAndActOnComplete(groupIndex,go);
            return;
        }
        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (group.taskObjects.Count - 1));
        // Update game state progression for this completed task
        UpdateGameStateProgressionForCompletedTask(go.name);
        // if (group.taskObjects.Count == 0)
        // {
        //     UpdateTaskGroupCompletion(groupIndex);
        // }
        group.taskObjects.Remove(go);

        // autoActOnComplete above calls RemoveTaskObjectAndActOnComplete
    }

    // INTERNAL - most checks should be completed
    private void RemoveTaskObjectAndActOnComplete(int groupIndex, GameObject go)
    {
        if (groupIndex < 0 || groupIndex >= taskGroups.Count)
        {
            Debug.LogError("RemoveTaskObjectAndActOnComplete: Invalid group index: " + groupIndex);
            return;
        }
        TaskGroup group = taskGroups[groupIndex];

        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (group.taskObjects.Count - 1));
        // Update game state progression for this completed task
        UpdateGameStateProgressionForCompletedTask(go.name);
        // if (group.taskObjects.Count == 0)
        // {
        //     UpdateTaskGroupCompletion(groupIndex);
        // }
        group.taskObjects.Remove(go);

        ActOnCompleteForGroup(groupIndex);
    }

    public void RemoveTaskObjectAndActOnComplete(GameObject go)
    {
        int groupIndex = FindTaskGroupIndexForTaskObject(go);
        if (groupIndex == -1)
        {
            Debug.LogError("RemoveTaskObjectAndActOnComplete: No TaskGroup found containing task object: " + go.name);
            return;
        }
        TaskGroup group = taskGroups[groupIndex];
        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (group.taskObjects.Count - 1));
        // Update game state progression for this completed task
        UpdateGameStateProgressionForCompletedTask(go.name);
        
        group.taskObjects.Remove(go);

        if (IsItOkayToActOnComplete(groupIndex))
        {
            group.onTasksCompleted.Invoke();
            group.actedOnComplete = true;
            taskGroupsCompleted++;
        }
    }

   // INTERNAL - all checks should be completed
    private void ActOnCompleteForGroup(int groupIndex)
    {
        TaskGroup group = taskGroups[groupIndex];
        if (!group.autoActOnComplete)
            return;       

        if (!IsItOkayToActOnComplete(groupIndex))
            return;

        if (group.actedOnComplete)
        {
            Debug.LogWarning("Group: " + group.taskGroupName + " has already had its onTasksCompleted event invoked. Skipping invocation.");
            return;
        }
        group.onTasksCompleted.Invoke();
        group.actedOnComplete = true;
        taskGroupsCompleted++;
    }

    public void ActOnAnyTaskGroupsCompleted()
    {
        for (int i = 0; i < taskGroups.Count; i++)
        {
            if (IsItOkayToActOnComplete(i))
            {
                taskGroups[i].onTasksCompleted.Invoke();
                taskGroups[i].actedOnComplete = true;
                taskGroupsCompleted++;
            }
        }
    }

    public void ForceCompletetaskGroup(int groupIndex, bool actOnComplete = true)
    {
        if (groupIndex < 0 || groupIndex >= taskGroups.Count)
        {
            Debug.LogError("ForceCompleteTaskGroup: Invalid group index: " + groupIndex);
            return;
        }
        Debug.Log("Force completing all tasks for group: " + taskGroups[groupIndex].taskGroupName + " in scene: " + sceneName);
        // Update game state progression for all remaining tasks in this group
        foreach (var go in taskGroups[groupIndex].taskObjects)
        {
            UpdateGameStateProgressionForCompletedTask(go.name);
        }
        taskGroups[groupIndex].taskObjects.Clear();
        if (taskGroups[groupIndex].autoActOnComplete || actOnComplete)
        {
            taskGroups[groupIndex].onTasksCompleted.Invoke();
            taskGroups[groupIndex].actedOnComplete = true;
            taskGroupsCompleted++;
        }
    }

    public void ForceCompleteAllTaskGroups(bool actOnComplete = true)
    {
        Debug.Log("Force completing all tasks for scene: " + sceneName);
        // Update game state progression for all remaining tasks
        foreach (var group in taskGroups)
        {
            foreach (var go in group.taskObjects)
            {
                UpdateGameStateProgressionForCompletedTask(go.name);
            }
            group.taskObjects.Clear();
            if (group.autoActOnComplete || actOnComplete)
            {
                group.onTasksCompleted.Invoke();
                group.actedOnComplete = true;
                taskGroupsCompleted++;
            }
        }
        taskGroups.Clear();
    }

    public void ClearAllTaskGroups()
    {
        Debug.Log("Clearing all task groups for scene (with NO progression updates): " + sceneName);
        taskGroups.Clear();
    }

    public void ClearTaskGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= taskGroups.Count)
        {
            Debug.LogError("ClearTaskGroup: Invalid group index: " + groupIndex);
            return;
        }
        Debug.Log("Clearing task group: " + taskGroups[groupIndex].taskGroupName + " for scene: " + sceneName + " with NO progression updates");
        taskGroups[groupIndex].taskObjects.Clear();
    }

    public void CompleteNonTaskObject(GameObject go)
    {
        Debug.Log("Completing non-task object: " + go.name + " for scene: " + sceneName);
        UpdateGameStateProgressionForCompletedTask(go.name);
    }

    public bool AreGivenTasksComplete(List<GameObject> taskObjects)
    {
        // Check if all given task objects are in the gameState's sceneProgressionInfo for this scene, and if so, remove them and act on completion. If any are not, log a warning and return without doing anything
        if (!GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(sceneName))
        {
            Debug.LogWarning("Cannot act on tasks complete for scene: " + sceneName + " because no progression info found for this scene in game state.");
            return false;
        }
        foreach (var go in taskObjects)
        {
            if (!GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Contains(go.name))
            {
                //Debug.Log("Task " + go.name + " is not marked as completed for scene: " + sceneName + " in game state progression info.");
                return false;
            }
        }
        return true;
    }
    
    public bool IsGivenTaskComplete(GameObject go)
    {
        // Check if this task object is in the gameState's sceneProgressionInfo for this scene, and if so, remove it and act on completion. If not, log a warning and return without doing anything
        if (!GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(sceneName))
        {
            Debug.LogWarning("Cannot act on task complete for scene: " + sceneName + " because no progression info found for this scene in game state.");
            return false;
        }
        if (!GameManager.Instance.gameState.sceneProgressionInfo[sceneName].Contains(go.name))
        {
            Debug.LogWarning("Task " + go.name + " is not marked as completed for scene: " + sceneName + " in game state progression info.");
            return false;
        }
        return true;
    }
#endregion Task-Group Manage

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
