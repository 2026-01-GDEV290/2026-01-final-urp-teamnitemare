using UnityEngine;
using System;
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

public class GameState
{
    public string GameName = "Team Nitemare's Caged? Game";

    public GameStates currentGameState = GameStates.Loading;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;

    public GameLevels currentLevel = GameLevels.None;
}
