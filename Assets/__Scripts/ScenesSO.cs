using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "ScenesSO", menuName = "Scriptable Objects/ScenesSO")]
public class ScenesSO : ScriptableObject
{
    // Unfortunately Unity has no built-in SceneReference type
    // There are community implementations, but for now we'll just use strings
    //public SceneReference mainMenuScene;
    public string mainMenuScene;
    public Scenes mainMenuSceneEnum = Scenes.MainMenu;
    public List<string> gameScenes;
    public Scenes gameSceneEnum = Scenes.Game;
    public string gameOverScene;
    public Scenes gameOverSceneEnum = Scenes.GameOver;
    //public string creditsScene;
    public string DCExperimentsScene;
    public Scenes DCExperimentsSceneEnum = Scenes.Game; //Scenes.DCExperiments;

    public string UITestScene;
    public Scenes UITestSceneEnum = Scenes.Game;    //UITest;
    public string UILayoutScene;
    public Scenes UILayoutSceneEnum = Scenes.Game;  //UILayout;
}
