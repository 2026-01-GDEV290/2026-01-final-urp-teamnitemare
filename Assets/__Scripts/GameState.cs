using UnityEngine;
using System;
using System.Collections.Generic;
//using UnityEditor;

[Serializable]
public enum Scenes
{
    LoadingScreen,
    MainMenu,
    Game,
    GameOver,
    DCExperiments,
    UITest,
    UILayout
}

[Serializable]
public enum GameStates
{
    Loading,
    Playing,
    Paused,
    UI,
    GameOver,
    Win,
    Lose
 };

 [Serializable]
 public enum MultiplayerModes
 {
    Disconnected,
    LocalHotseat,
    Online
 };

 [Serializable]
 public enum GameLevels
 {
    Interlude,
    Level1,
    Level2,
    Level3,
    Level4,
    Level5,
    Experimental,
    None
 };

 [Serializable]
 public class QuestTask
 {
    public string taskName;
    public bool isCollectibleTask;    
    public string taskDescription;
    public bool isCompleted;
    public int taskValue;
    public int taskMaxValue;
    public bool oneTimeCompletion;
    public List<QuestTask> requiredTasks; // Tasks that must be completed before this task can be completed
 };

 [Serializable]
 public class QuestInfo
{
    public bool isGlobalQuest;
    public string questName;
    public string sceneBelongingTo; // null/empty if global quest
    public string questDescription;
    public bool isCompleted;
    public int numObjectivesCompleted;
    public int totalObjectives;
    public List<QuestTask> questTasks;
}

[Serializable]
public class SceneQuestInfo
{
    public string sceneName;    // "GLOBAL" for global quests
    public List<QuestInfo> questsInScene;
}

[Serializable]
public class GameState
{
    public string GameName = "Team Nitemare's Caged? Game";

    public GameStates currentGameState = GameStates.Loading;

    public bool inGameModalDialogueActive = false;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;
    public string currentSceneName = "";
    public string previousSceneName = "";

    public Scene currentSceneScript = null;
    //public GameObject playerPawn = null;
    public GameLevels currentLevel = GameLevels.None;

    // Scene, Tasks Completed
    //[field: SerializeField] public Dictionary<string, List<string>> sceneProgressionInfo = new Dictionary<string, List<string>>();

    public List<string> scenesInOrderOfVisit = new List<string>();
    public SceneQuestInfo globalQuestInfo = new SceneQuestInfo { sceneName = "GLOBAL", questsInScene = new List<QuestInfo>() };
    public List<SceneQuestInfo> sceneQuestInfos = new List<SceneQuestInfo>();

    public int numCurrentLevelObjectivesCompleted = 0;
    public int totalCurrentLevelObjectives = 0;
    public bool currentLevelCompleted = false;


#region Quest Progression

    public int GetSceneVisitCount(string sceneName)
    {
        return scenesInOrderOfVisit.FindAll(s => s == sceneName).Count;
    }

    public int QuestIndex(string sceneName, string questName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene.FindIndex(q => q.questName == questName);
        }
        return -1; // Quest not found
    }
    public int TaskIndex(string sceneName, string questName, string taskName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questName == questName);
            if (questInfo != null)
            {
                return questInfo.questTasks.FindIndex(t => t.taskName == taskName);
            }
        }
        return -1; // Task not found
    }
    public QuestInfo GetQuestInfo(string sceneName, string questName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene.Find(q => q.questName == questName);
        }
        return null; // Quest not found
    }
    public QuestTask GetTaskInfo(string sceneName, string questName, string taskName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questName == questName);
            if (questInfo != null)
            {
                return questInfo.questTasks.Find(t => t.taskName == taskName);
            }
        }
        return null; // Task not found
    }
    public QuestTask GetTaskInfo(string sceneName, string taskName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            foreach (var quest in sceneInfo.questsInScene)
            {
                QuestTask taskInfo = quest.questTasks.Find(t => t.taskName == taskName);
                if (taskInfo != null)
                {
                    return taskInfo;
                }
            }
        }
        return null; // Task not found
    }
    public bool IsTaskComplete(string sceneName, string taskName)
    {
        QuestTask taskInfo = GetTaskInfo(sceneName, taskName);
        if (taskInfo != null)
        {
            return taskInfo.isCompleted;
        }
        return false; // Task not found
    }
    public bool IsQuestComplete(string sceneName, string questName)
    {
        QuestInfo questInfo = GetQuestInfo(sceneName, questName);
        if (questInfo != null)
        {
            return questInfo.isCompleted;
        }
        return false; // Quest not found
    }
    public void AddQuestToScene(string sceneName, QuestInfo questInfo)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            sceneInfo.questsInScene.Add(questInfo);
        }
        else
        {
            SceneQuestInfo newSceneInfo = new SceneQuestInfo { sceneName = sceneName, questsInScene = new List<QuestInfo> { questInfo } };
            sceneQuestInfos.Add(newSceneInfo);
        }
    }
    public void MarkQuestComplete(string sceneName, string questName)
    {
        QuestInfo questInfo = GetQuestInfo(sceneName, questName);
        if (questInfo != null)
        {
            questInfo.isCompleted = true;
            Debug.Log("Marked quest: " + questName + " in scene: " + sceneName + " as complete.");
        }
        else
        {
            Debug.LogWarning("Cannot mark quest: " + questName + " as complete because it was not found in scene: " + sceneName);
        }
    }
    public void MarkTaskComplete(string sceneName, string questName, string taskName)
    {
        QuestTask taskInfo = GetTaskInfo(sceneName, questName, taskName);
        if (taskInfo != null)
        {
            taskInfo.isCompleted = true;
            Debug.Log("Marked task: " + taskName + " in quest: " + questName + " in scene: " + sceneName + " as complete.");
        }
        else
        {
            Debug.LogWarning("Cannot mark task: " + taskName + " as complete because it was not found in quest: " + questName + " in scene: " + sceneName);
        }
    }
    public void AddTaskObjectToScene(string sceneName, string questName, QuestTask taskInfo)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questName == questName);
            if (questInfo != null)
            {
                questInfo.questTasks.Add(taskInfo);
            }
            else
            {
                Debug.LogWarning("Cannot add task: " + taskInfo.taskName + " for quest: " + questName + " in scene: " + sceneName + " because no info found for this quest in game state.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot add task: " + taskInfo.taskName + " for quest: " + questName + " in scene: " + sceneName + " because no info found for this scene in game state.");
        }
    }
    public void AddNonTaskObjectToSceneAsCompleted(string sceneName, string objectName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo nonTaskObjectAsQuest = new QuestInfo
            {
                isGlobalQuest = false,
                questName = objectName,
                sceneBelongingTo = sceneName,
                questDescription = "Non-task object: " + objectName,
                isCompleted = true,
                numObjectivesCompleted = 0,
                totalObjectives = 0,
                questTasks = new List<QuestTask>()
            };
            sceneInfo.questsInScene.Add(nonTaskObjectAsQuest);
        }
        else
        {
            Debug.LogWarning("Cannot add non-task object: " + objectName + " as completed for scene: " + sceneName + " because no progression info found for this scene in game state.");
        }
    }
    public void AddGlobalQuest(QuestInfo questInfo)
    {
        globalQuestInfo.questsInScene.Add(questInfo);
    }
#endregion Quest Progression
}
