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
    public string taskUniqueId;
    public string taskName;
    public string questTaskTag;
    public bool isCollectibleTask;    
    public string taskDescription;
    public bool isCompleted;
    public int taskValue;
    public int taskMaxValue;
    public bool oneTimeCompletion;
    [SerializeReference] 
    public List<QuestTask> requiredTasks = null; // Tasks that must be completed before this task can be completed
 };

 [Serializable]
 public class QuestInfo
{
    public string questUniqueId;
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
    public int visit;
    public List<QuestInfo> questsInScene;
}

[Serializable]
public class GameState
{
    public string GameName = "Team Nitemare's Caged? Game";

    public GameStates currentGameState = GameStates.Loading;

    public bool inGameModalDialogueActive = false;

    public static ScenesSO scenesSO;

    // unique scene names list
    
    public List<string> sceneNames = new List<string>();

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;
    public string currentSceneName = "";
    public string previousSceneName = "";

    public Scene currentSceneScript = null;
    //public GameObject playerPawn = null;
    public GameLevels currentLevel = GameLevels.None;

    // Scene, Tasks Completed
    //[field: SerializeField] public Dictionary<string, List<string>> sceneProgressionInfo = new Dictionary<string, List<string>>();

    public List<int> scenesInOrderOfVisit = new List<int>();
    public SceneQuestInfo globalQuestInfo = new SceneQuestInfo { sceneName = "GLOBAL", visit = -1, questsInScene = new List<QuestInfo>() };
    public List<SceneQuestInfo> sceneQuestInfos = new List<SceneQuestInfo>();

    public int numCurrentLevelObjectivesCompleted = 0;
    public int totalCurrentLevelObjectives = 0;
    public bool currentLevelCompleted = false;

    public void Initialize()
    {
        if (scenesSO == null)
        {
            scenesSO = Resources.Load<ScenesSO>("ScenesSO"); 
            if (scenesSO == null)
            {
                Debug.LogError("GameState->Initialize: Failed to load ScenesSO from Resources. Make sure there is a ScenesSO asset in a Resources folder.");
            }
        }
        sceneNames = scenesSO.GetAllScenes();
    }

    // Enforces one scene in list and optionally visit order
    public void AddScene(string sceneName, bool addToVisitOrder = true)
    {
        int index = sceneNames.FindIndex(s => s == sceneName);
        if (index == -1)
        {
            sceneNames.Add(sceneName);
            index = sceneNames.Count - 1;
            //Debug.Log("Added scene: " + sceneName + " to game state with index: " + index);
        }
        if (addToVisitOrder)
        {
            scenesInOrderOfVisit.Add(index);
        }
    }
    public bool SceneExists(string sceneName)
    {
        return sceneNames.Contains(sceneName);
    }
    public int SceneIndex(string sceneName)
    {
        // returns index of scene if found, otherwise -1
        return sceneNames.FindIndex(s => s == sceneName);
    }
    public int GetSceneVisitCount(string sceneName)
    {
        int index = SceneIndex(sceneName);
        if (index == -1)
        {
            Debug.LogError("GetSceneVisitCount: Scene: " + sceneName + " not found in game state.");
            return 0;
        }
        return scenesInOrderOfVisit.FindAll(s => s == index).Count;
    }
    public int GetSceneVisitCount(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= sceneNames.Count)
        {
            Debug.LogError("GetSceneVisitCount: Scene index: " + sceneIndex + " is out of bounds in game state.");
            return 0;
        }
        return scenesInOrderOfVisit.FindAll(s => s == sceneIndex).Count;
    }

#region Quest Progression
    public int QuestIndex(string sceneName, string questUniqueId)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene.FindIndex(q => q.questUniqueId == questUniqueId);
        }
        return -1; // Quest not found
    }
    public int TaskIndex(string sceneName, string questUniqueId, string taskName)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                return questInfo.questTasks.FindIndex(t => t.taskName == taskName);
            }
        }
        return -1; // Task not found
    }
    public QuestInfo GetQuestInfo(string sceneName, string questUniqueId)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            return sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
        }
        return null; // Quest not found
    }
    public QuestTask GetTaskInfo(string sceneName, string questUniqueId, string taskUniqueId)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                return questInfo.questTasks.Find(t => t.taskUniqueId == taskUniqueId);
            }
        }
        return null; // Task not found
    }
    public QuestTask GetTaskInfo(string sceneName, string taskUniqueId)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            foreach (var quest in sceneInfo.questsInScene)
            {
                QuestTask taskInfo = quest.questTasks.Find(t => t.taskUniqueId == taskUniqueId);
                if (taskInfo != null)
                {
                    return taskInfo;
                }
            }
        }
        return null; // Task not found
    }
    public bool IsTaskComplete(string sceneName, string taskUniqueId)
    {
        QuestTask taskInfo = GetTaskInfo(sceneName, taskUniqueId);
        if (taskInfo != null)
        {
            return taskInfo.isCompleted;
        }
        return false; // Task not found
    }
    public bool IsQuestComplete(string sceneName, string questUniqueId)
    {
        QuestInfo questInfo = GetQuestInfo(sceneName, questUniqueId);
        if (questInfo != null)
        {
            return questInfo.isCompleted;
        }
        return false; // Quest not found
    }
    public void AddQuestToScene(string sceneName, QuestInfo questInfo, int visit = -1)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            sceneInfo.questsInScene.Add(questInfo);
        }
        else
        {
            SceneQuestInfo newSceneInfo = new SceneQuestInfo { sceneName = sceneName, visit = visit, questsInScene = new List<QuestInfo> { questInfo } };
            sceneQuestInfos.Add(newSceneInfo);
        }
    }
    public void MarkQuestComplete(string sceneName, string questUniqueId)
    {
        QuestInfo questInfo = GetQuestInfo(sceneName, questUniqueId);
        if (questInfo != null)
        {
            questInfo.isCompleted = true;
            Debug.Log("Marked quest: " + questInfo.questName + " in scene: " + sceneName + " as complete.");
        }
        else
        {
            Debug.LogWarning("Cannot mark quest: " + questUniqueId + " as complete because it was not found in scene: " + sceneName);
        }
    }
    public void MarkTaskComplete(string sceneName, string questUniqueId, string taskUniqueId)
    {
        QuestTask taskInfo = GetTaskInfo(sceneName, questUniqueId, taskUniqueId);
        if (taskInfo != null)
        {
            taskInfo.isCompleted = true;
            Debug.Log("Marked task: " + taskInfo.taskName + " in quest: " + questUniqueId + " in scene: " + sceneName + " as complete.");
        }
        else
        {
            Debug.LogWarning("Cannot mark task: " + taskUniqueId + " as complete because it was not found in quest: " + questUniqueId + " in scene: " + sceneName);
        }
    }
    public void AddTaskObjectToScene(string sceneName, string questUniqueId, QuestTask taskInfo)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo questInfo = sceneInfo.questsInScene.Find(q => q.questUniqueId == questUniqueId);
            if (questInfo != null)
            {
                questInfo.questTasks.Add(taskInfo);
            }
            else
            {
                Debug.LogWarning("QCannot add task: " + taskInfo.taskName + " for quest: " + questUniqueId + " in scene: " + sceneName + " because no info found for this quest in game state.");
            }
        }
        else
        {
            Debug.LogError("SCannot add task: " + taskInfo.taskName + " for quest: " + questUniqueId + " in scene: " + sceneName + " because no info found for this scene in game state.");
        }
    }
    public void AddNonTaskObjectToSceneAsCompleted(string sceneName, string nonTaskUniqueId)
    {
        SceneQuestInfo sceneInfo = sceneQuestInfos.Find(s => s.sceneName == sceneName);
        if (sceneInfo != null)
        {
            QuestInfo nonTaskObjectAsQuest = new QuestInfo
            {
                questUniqueId = "NonQuest_" + nonTaskUniqueId,
                isGlobalQuest = false,
                questName = "NonQuest_" + nonTaskUniqueId,
                sceneBelongingTo = sceneName,
                questDescription = "NonQuest - Non-task object: " + nonTaskUniqueId,
                isCompleted = true,
                numObjectivesCompleted = 0,
                totalObjectives = 0,
                questTasks = new List<QuestTask>()
            };
            sceneInfo.questsInScene.Add(nonTaskObjectAsQuest);
        }
        else
        {
            Debug.LogWarning("Cannot add non-task object: " + nonTaskUniqueId + " as completed for scene: " + sceneName + " because no progression info found for this scene in game state.");
        }
    }
    public void AddGlobalQuest(QuestInfo questInfo)
    {
        globalQuestInfo.questsInScene.Add(questInfo);
    }
#endregion Quest Progression
}
