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

public class GameState
{
    public static string GameName = "Team Nitemare's Caged? Game";

    public GameStates currentGameState = GameStates.Loading;

    public static ScenesSO scenesSO;

    public Scenes currentScene = Scenes.LoadingScreen;
    public Scenes previousScene = Scenes.LoadingScreen;


}
