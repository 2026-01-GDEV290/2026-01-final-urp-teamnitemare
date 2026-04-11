using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[Serializable]
public class TaskGroup
{
    public string taskGroupName = "";
    public bool completePriorGroupFirst = false;
    public List<GameObject> taskObjects = new List<GameObject>();
    public bool autoActOnComplete = false;
    public UnityEvent onTasksCompleted;
    public bool actedOnComplete = false;
}

[Serializable]
public class QuestComponent : MonoBehaviour
{
    [SerializeField] private List<TaskGroup> taskGroups = new List<TaskGroup>();

    private int taskGroupsCompleted = 0;
    private string sceneName = "";

    private string CurrentSceneName => string.IsNullOrEmpty(sceneName) ? SceneManager.GetActiveScene().name : sceneName;

    public int TaskGroupCount => taskGroups.Count;

    public int GetTotalTasksInAllGroups()
    {
        int total = 0;
        foreach (var group in taskGroups)
        {
            total += group.taskObjects.Count;
        }
        return total;
    }

    private void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    public void ImportLegacyTaskGroups(List<TaskGroup> legacyGroups)
    {
        if (legacyGroups == null || legacyGroups.Count == 0)
            return;

        foreach (var legacyGroup in legacyGroups)
        {
            if (legacyGroup != null)
            {
                taskGroups.Add(legacyGroup);
            }
        }
    }

    private void OnValidate()
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

        TaskGroup lastGroup = taskGroups[taskGroups.Count - 2];
        TaskGroup newGroup = taskGroups[taskGroups.Count - 1];
        if (newGroup.taskGroupName == lastGroup.taskGroupName)
        {
            newGroup.taskGroupName = "TaskGroup" + taskGroups.Count;
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
                    newGroup.taskObjects.Clear();
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
            int existingGroupIndex = taskGroups.FindIndex(g => g.taskGroupName == name);
            return existingGroupIndex;
        }

        taskGroups.Add(new TaskGroup
        {
            taskGroupName = name,
            completePriorGroupFirst = completePriorGroupFirst,
            autoActOnComplete = autoActOnComplete,
            onTasksCompleted = new UnityEvent()
        });

        taskGroups[taskGroups.Count - 1].onTasksCompleted.AddListener(onCompleted);
        return taskGroups.Count - 1;
    }

    private void UpdateGameStateProgressionForCompletedTask(string taskName)
    {
        string activeSceneName = CurrentSceneName;

        if (GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(activeSceneName))
        {
            if (!GameManager.Instance.gameState.sceneProgressionInfo[activeSceneName].Contains(taskName))
            {
                GameManager.Instance.gameState.sceneProgressionInfo[activeSceneName].Add(taskName);
                Debug.Log("Added " + taskName + " to completed objectives for scene: " + activeSceneName);
            }
            else
            {
                Debug.LogWarning("Objective " + taskName + " already marked as completed for scene: " + activeSceneName);
            }
        }
        else
        {
            Debug.LogError("No sceneProgressionInfo found for scene: " + activeSceneName);
        }
    }

    private int FindTaskGroupIndexForTaskObject(GameObject go)
    {
        int groupIndex = taskGroups.FindIndex(g => g.taskObjects.Contains(go));
        return groupIndex;
    }

    private bool IsItOkayToActOnComplete(int groupIndex)
    {
        Debug.Log("Checking if it's okay to act on complete for group: " + taskGroups[groupIndex].taskGroupName + " in scene: " + sceneName);
        Debug.Log("Group has " + taskGroups[groupIndex].taskObjects.Count + " tasks remaining.");

        if (taskGroups[groupIndex].taskObjects.Count > 0)
            return false;

        if (taskGroups[groupIndex].actedOnComplete)
            return false;

        if (groupIndex == 0)
            return true;

        if (!taskGroups[groupIndex].completePriorGroupFirst)
        {
            return true;
        }

        for (int i = groupIndex - 1; i >= 0; i--)
        {
            if (taskGroups[i].taskObjects.Count > 0)
            {
                Debug.Log("Previous group: " + taskGroups[i].taskGroupName + " still has tasks remaining.");
                return false;
            }

            if (!taskGroups[i].completePriorGroupFirst)
                return true;
        }

        return true;
    }

    public void RemoveTaskObject(GameObject go)
    {
        int groupIndex = FindTaskGroupIndexForTaskObject(go);
        if (groupIndex == -1)
        {
            Debug.LogError("RemoveTaskObject: No TaskGroup found containing task object: " + go.name);
            return;
        }

        TaskGroup group = taskGroups[groupIndex];

        if (group.autoActOnComplete)
        {
            RemoveTaskObjectAndActOnComplete(groupIndex, go);
            return;
        }

        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (group.taskObjects.Count - 1));
        UpdateGameStateProgressionForCompletedTask(go.name);
        group.taskObjects.Remove(go);
    }

    private void RemoveTaskObjectAndActOnComplete(int groupIndex, GameObject go)
    {
        if (groupIndex < 0 || groupIndex >= taskGroups.Count)
        {
            Debug.LogError("RemoveTaskObjectAndActOnComplete: Invalid group index: " + groupIndex);
            return;
        }
        TaskGroup group = taskGroups[groupIndex];

        Debug.Log("Removing Task Object: " + go.name + ", tasks left: " + (group.taskObjects.Count - 1));
        UpdateGameStateProgressionForCompletedTask(go.name);
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
        UpdateGameStateProgressionForCompletedTask(go.name);

        group.taskObjects.Remove(go);

        if (IsItOkayToActOnComplete(groupIndex))
        {
            group.onTasksCompleted.Invoke();
            group.actedOnComplete = true;
            taskGroupsCompleted++;
        }
    }

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
        Debug.Log("Force completing all tasks for group: " + taskGroups[groupIndex].taskGroupName + " in scene: " + CurrentSceneName);
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
        Debug.Log("Force completing all tasks for scene: " + CurrentSceneName);
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
        Debug.Log("Clearing all task groups for scene (with NO progression updates): " + CurrentSceneName);
        taskGroups.Clear();
    }

    public void ClearTaskGroup(int groupIndex)
    {
        if (groupIndex < 0 || groupIndex >= taskGroups.Count)
        {
            Debug.LogError("ClearTaskGroup: Invalid group index: " + groupIndex);
            return;
        }
        Debug.Log("Clearing task group: " + taskGroups[groupIndex].taskGroupName + " for scene: " + CurrentSceneName + " with NO progression updates");
        taskGroups[groupIndex].taskObjects.Clear();
    }

    public void CompleteNonTaskObject(GameObject go)
    {
        Debug.Log("Completing non-task object: " + go.name + " for scene: " + CurrentSceneName);
        UpdateGameStateProgressionForCompletedTask(go.name);
    }

    public bool AreGivenTasksComplete(List<GameObject> taskObjects)
    {
        string activeSceneName = CurrentSceneName;
        if (!GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(activeSceneName))
        {
            Debug.LogWarning("Cannot act on tasks complete for scene: " + activeSceneName + " because no progression info found for this scene in game state.");
            return false;
        }
        foreach (var go in taskObjects)
        {
            if (!GameManager.Instance.gameState.sceneProgressionInfo[activeSceneName].Contains(go.name))
            {
                return false;
            }
        }
        return true;
    }

    public bool IsGivenTaskComplete(GameObject go)
    {
        string activeSceneName = CurrentSceneName;
        if (!GameManager.Instance.gameState.sceneProgressionInfo.ContainsKey(activeSceneName))
        {
            Debug.LogWarning("Cannot act on task complete for scene: " + activeSceneName + " because no progression info found for this scene in game state.");
            return false;
        }
        if (!GameManager.Instance.gameState.sceneProgressionInfo[activeSceneName].Contains(go.name))
        {
            Debug.LogWarning("Task " + go.name + " is not marked as completed for scene: " + activeSceneName + " in game state progression info.");
            return false;
        }
        return true;
    }
}