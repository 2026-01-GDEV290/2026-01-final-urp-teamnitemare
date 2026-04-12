using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

// TODO: Collectibles with same-name, count etc implementation

[Serializable]
public class TaskGroup
{
    public string taskGroupName = "";
    public bool isCollectibleGroup = false;
    public bool completeReferencedTaskFirst = false;
    public TaskGroup taskObjectToCompleteFirst;
    public List<QuestComponent> taskObjects = new List<QuestComponent>();
    public bool autoActOnComplete = false;
    public UnityEvent onTasksCompleted = new UnityEvent();
    public bool actedOnComplete = false;
    public int taskMinimumForCompletion = 0;
    public int tasksCompleted = 0;

    public QuestInfo ConvertToQuestInfo()
    {
        // full assignment
        QuestInfo questInfo = new QuestInfo {
            isGlobalQuest = false,
            questName = taskGroupName, 
            sceneBelongingTo = SceneManager.GetActiveScene().name,
            questDescription = "",
            isCompleted = actedOnComplete,
            numObjectivesCompleted = tasksCompleted,
            totalObjectives = taskObjects.Count,
            questTasks = new List<QuestTask>(),
        };
        foreach (var go in taskObjects)
        {
            questInfo.questTasks.Add(new QuestTask {
                taskName = go.questComponentId, isCollectibleTask = go.isCollectible,
                isCompleted = actedOnComplete, taskDescription = "",
                taskValue = 1, taskMaxValue = 1, oneTimeCompletion = true});
        }
        return questInfo;
    }
    public QuestTask ConvertToQuestTask(int index)
    {
        if (index < 0 || index >= taskObjects.Count)
        {
            Debug.LogError("ConvertToQuestTask: Index out of range for task objects. Index: " + index + ", Count: " + taskObjects.Count);
            return null;
        }
        var go = taskObjects[index];
        return new QuestTask {
            taskName = go.questComponentId, isCollectibleTask = go.isCollectible,
            isCompleted = actedOnComplete, taskDescription = "",
            taskValue = 1, taskMaxValue = 1, oneTimeCompletion = true
        };
    }
}

[Serializable]
public class Quest : MonoBehaviour
{
    [SerializeField] private TaskGroup taskGroup = new TaskGroup();

    public string QuestName => taskGroup.taskGroupName;
    public int TaskCount => taskGroup.taskObjects.Count;
    public int TasksRemaining => taskGroup.taskMinimumForCompletion != 0 ? taskGroup.taskMinimumForCompletion - taskGroup.tasksCompleted : 0;

    private string sceneName = "";

    private string QuestGroupId => gameObject.name + "_" + taskGroup.taskGroupName;

    public bool ActedOnComplete => taskGroup.actedOnComplete;

    bool questAdded = false;


    private void Awake()
    {
        sceneName = SceneManager.GetActiveScene().name;
    }

    private void Start()
    {
        if (!questAdded)
        {
            QuestManager.Instance.AddQuest(this);
            questAdded = true;
        }
    }

    public static Quest CreateQuest(string questName, string sceneName, TaskGroup taskGroup)
    {
        GameObject questGO = new GameObject(questName);
        Quest quest = questGO.AddComponent<Quest>();
        quest.sceneName = sceneName;
        quest.taskGroup = taskGroup;
        if (quest.taskGroup.onTasksCompleted == null)
        {
            quest.taskGroup.onTasksCompleted = new UnityEvent();
        }
        if (string.IsNullOrEmpty(quest.taskGroup.taskGroupName))
        {
            quest.taskGroup.taskGroupName = questName;
        }
        Debug.Log("Created quest with name: " + questName + " in scene: " + sceneName);
        QuestManager.Instance.AddQuest(quest);
        quest.questAdded = true;
        return quest;
    }
    public static Quest CreateQuest(string questName, string sceneName, string taskGroupName)
    {
        TaskGroup taskGroup = new TaskGroup { taskGroupName = taskGroupName };
        return CreateQuest(questName, sceneName, taskGroup);
    }

    public Quest FindQuestByName(string questName)
    {
        return QuestManager.Instance.FindQuest(questName);
    }

    public TaskGroup GetTaskGroup()
    {
        return taskGroup;
    }

    public void AddTaskObject(QuestComponent go)
    {
        taskGroup.taskObjects.Add(go);
        if (taskGroup.taskMinimumForCompletion != 0)
        {
            Debug.Log("Added task object w/o increment: " + go.questComponentId + " to group: " + taskGroup.taskGroupName + " in scene: " + sceneName + ". Tasks remaining to complete group: " + TasksRemaining);
        }
        QuestManager.Instance.AddTaskObject(this, go, false);
    }

    private bool IsItOkayToActOnComplete()
    {
        if (taskGroup.taskObjects.Count > taskGroup.taskMinimumForCompletion || taskGroup.actedOnComplete)
            return false;

        if (taskGroup.taskObjectToCompleteFirst != null && !taskGroup.completeReferencedTaskFirst)
        {
            if (taskGroup.taskObjectToCompleteFirst.actedOnComplete == true)
            {
                taskGroup.completeReferencedTaskFirst = true;
                return true;
            }
        }
        // either nothing to complete first or that first thing was completed
        return true;
    }

    public int FindTaskGroupIndexForTaskObject(QuestComponent go)
    {
        for (int i = 0; i < taskGroup.taskObjects.Count; i++)
        {
            if (taskGroup.taskObjects[i] == go)
            {
                return i;
            }
        }
        return -1; // not found
    }

    public QuestInfo ConvertToQuestInfo()
    {
        return taskGroup.ConvertToQuestInfo();
    }

    public void CompleteTaskObject(QuestComponent go)
    {
        int groupIndex = FindTaskGroupIndexForTaskObject(go);
        if (groupIndex == -1)
        {
            Debug.LogError("CompleteTaskObject: No TaskGroup found containing task object: " + go.questComponentId);
            return;
        }
        CompleteTaskObjectAndActOnComplete(go);
    }

    private void CompleteTaskObjectAndActOnComplete(QuestComponent go)
    {
        TaskGroup group = taskGroup;
        Debug.Log("Removing Quest Object: " + go.questComponentId + ", tasks left: " + TasksRemaining + " for group: " + group.taskGroupName + " in scene: " + sceneName);
        QuestManager.Instance.CompletedQuestObject(this, go);

        group.taskObjects.Remove(go);
        group.tasksCompleted++;

        if (IsItOkayToActOnComplete())
        {
            group.onTasksCompleted.Invoke();
            group.actedOnComplete = true;
            QuestManager.Instance.CompletedQuest(this);
        }
    }

    public void ForceCompleteTaskGroup(bool actOnComplete = true)
    {
        Debug.Log("Force completing all tasks for group: " + taskGroup.taskGroupName + " in scene: " + sceneName);
        foreach (var go in taskGroup.taskObjects)
        {
            QuestManager.Instance.CompletedQuestObject(this, go);
        }
        taskGroup.taskObjects.Clear();
        if (taskGroup.autoActOnComplete || actOnComplete)
        {
            taskGroup.onTasksCompleted.Invoke();
            taskGroup.actedOnComplete = true;
            QuestManager.Instance.CompletedQuest(this);
        }
    }

    public void ClearTaskGroup()
    {
        Debug.Log("Clearing task group for scene (with NO progression updates): " + sceneName);
        taskGroup = new TaskGroup();
    }

    public void CompleteNonTaskObject(QuestComponent go)
    {
        Debug.Log("Completing non-task object: " + go.questComponentId + " for scene: " + sceneName);
        QuestManager.Instance.CompletedNonQuestObject(go);
    }

    // Note this isn't "correct" in terms of checking GameState task completion,
    // however it is quick if this Quest did have the given task as an objective
    public bool IsGivenTaskComplete(QuestComponent go)
    {
        // check if the given task object is still in the task group (i.e. not complete)
        foreach (var taskObject in taskGroup.taskObjects)
        {
            if (taskObject == go)
            {
                return false;
            }
        }
        return true;
    }

    // Note this isn't "correct" in terms of checking GameState task completion,
    // however it is quick if this Quest did have the given tasks as objectives
    public bool AreGivenTasksComplete(List<QuestComponent> taskObjects)
    {
        // find components in task group and check if they are all complete (i.e. not in the task group anymore)
        foreach (var go in taskObjects)
        {
            if (IsGivenTaskComplete(go) == false)
            {
                Debug.Log("Task object: " + go.questComponentId + " is not yet complete for scene: " + sceneName);
                return false;
            }
        }
        return true;
    }


}