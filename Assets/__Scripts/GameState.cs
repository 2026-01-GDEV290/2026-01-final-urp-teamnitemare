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
public class GameState
{
    public string GameName = "Team Nitemare's Caged? Game";

    public GameStates currentGameState = GameStates.Loading;

    public bool inGameModalDialogueActive = false;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;

    public Scene currentSceneScript = null;
    //public GameObject playerPawn = null;
    public GameLevels currentLevel = GameLevels.None;

    // Scene, Tasks Completed
    [field: SerializeField] public Dictionary<string, List<string>> sceneProgressionInfo = new Dictionary<string, List<string>>();

    public int numCurrentLevelObjectivesCompleted = 0;
    public int totalCurrentLevelObjectives = 0;
    public bool currentLevelCompleted = false;

}
