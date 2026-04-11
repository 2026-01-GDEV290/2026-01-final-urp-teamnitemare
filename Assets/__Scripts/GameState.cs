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
    public void AddGlobalQuest(QuestInfo questInfo)
    {
        globalQuestInfo.questsInScene.Add(questInfo);
    }

}
