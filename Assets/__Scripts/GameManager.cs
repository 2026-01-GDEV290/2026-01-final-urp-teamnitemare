using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
//using TMPro;


[Serializable]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public InputManager inputManager;
    //AudioClip clickSound;

    //[SerializeField] public PlayerSessionManager sessionManager = new PlayerSessionManager();
    //[SerializeField] public GameStateServer gameStateServer = new GameStateServer();

    // (currently) Only for Editor inspection:
    //[SerializeField] public GameStateClient gameStateClient;
    //public GameStateClient gameStateClient2;

    //public ServerDispatch serverDispatch = new ServerDispatch();

    //public MultiplayerModes currentMultiplayerMode = MultiplayerModes.Disconnected;

    //public bool forceHotseat = true;

    //public List<string> hotseatPlayerNames = new List<string>() { "PlayerUNO", "Player2" };

    [SerializeField] public UIManager uiManager = null;

    //[SerializeField] public CagedGame cagedGame = null;
    [SerializeField] public GameState gameState = new GameState();
    [SerializeField] public PlayerState playerState = new PlayerState();

    bool mouseHideForGameScenes = true;
    bool timeScaleFreezeForPause = true;


    // Awake - Called before *FIRST* Scene, not destroyed or recreated on other Scene loads
    void Awake()
    {
        Debug.Log("GM->Awake()");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);        
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start - Called before the FIRST frame of *FIRST* Scene, not destroyed/recreated on other Scene loads
    void Start()
    {
        Debug.Log("GM->Start()");

        /*AudioClip clip = Resources.Load<AudioClip>("Audio/GenericAudioClip");
        if (clip == null)
        {
            Debug.LogError("Failed to load audio clip from Resources folder.");
            return;
        }
        AudioManager.Play(clip, 0.10f);*/

        //AudioManager.Play(AudioManager.musicPlaceholder, 1f);
        //AudioManager.Loop();
        
        //Debug.Log("Playing music: " + clip.name);
#if UNITY_EDITOR
        // Keep current editor level if in Editor
        //LevelCurrentInternalInit();
#else
        //LoadLevel(GameManager.Level.MainMenu);
#endif
    }

    public void LoadScene(Scenes scene, int sceneIndex = -1)
    {
        Debug.Log("GM->LoadScene(): " + scene.ToString());
        if (gameState.currentGameState == GameStates.Paused)
        {
            Debug.LogWarning("GM->LoadScene(): Game is paused, closing pause menu.");
            //playerState.SceneDestroyed();
            //gameState.SceneDestroyed();
            ClosePauseMenuAndResumeTime(true);
            SceneLoadingGameCleanup();
        }
        if (gameState.currentGameState == GameStates.Playing)
        {
            SceneLoadingGameCleanup();
        }
        gameState.previousScene = gameState.currentScene;
        // These have a lifetime of one scene (maybe put in SceneDestroyed()?)
        gameState.currentSceneScript = null;
        uiManager = null;
        switch (scene)
        {
            case Scenes.MainMenu:
                SceneManager.LoadScene(GameState.scenesSO.mainMenuScene);
                //currentScene = Scenes.MainMenu;
                gameState.currentScene = GameState.scenesSO.mainMenuSceneEnum;
                gameState.currentGameState = GameStates.UI;
                break;
            case Scenes.Game:
                sceneIndex = sceneIndex >= 0 && sceneIndex < GameState.scenesSO.gameScenes.Count ? sceneIndex : 0;
                SceneManager.LoadScene(GameState.scenesSO.gameScenes[sceneIndex]);
                //currentScene = Scenes.Game;
                gameState.currentScene = GameState.scenesSO.gameSceneEnum;
                gameState.currentGameState = GameStates.Playing;
                break;
            case Scenes.GameOver:
                SceneManager.LoadScene(GameState.scenesSO.gameOverScene);
                //currentScene = Scenes.GameOver;
                gameState.currentScene = GameState.scenesSO.gameOverSceneEnum;                
                gameState.currentGameState = GameStates.GameOver;
                break;
            case Scenes.DCExperiments:
                SceneManager.LoadScene(GameState.scenesSO.DCExperimentsScene);
                //currentScene = Scenes.DCExperiments;
                gameState.currentScene = GameState.scenesSO.DCExperimentsSceneEnum;
                gameState.currentGameState = GameStates.Playing;
                break;
            case Scenes.UITest:
                SceneManager.LoadScene(GameState.scenesSO.UITestScene);
                //currentScene = Scenes.UITest;
                gameState.currentScene = GameState.scenesSO.UITestSceneEnum;
                gameState.currentGameState = GameStates.Playing;
                break;
            case Scenes.UILayout:
                SceneManager.LoadScene(GameState.scenesSO.UILayoutScene);
                //currentScene = Scenes.UILayout;
                gameState.currentScene = GameState.scenesSO.UILayoutSceneEnum;
                gameState.currentGameState = GameStates.UI;
                break;
            default:
                Debug.LogError("Unknown scene: " + scene);
                break;
        }
        if (gameState.currentGameState == GameStates.UI)
        {
            // Ensure pause menu is closed when loading UI scenes (done above)
            //ClosePauseMenuAndResumeTime(false);
            MouseCursorSetForUI();
        }
        else if (gameState.currentGameState == GameStates.Playing)
        {
            // Hide mouse cursor in game scenes
            MouseCursorSetForGame();
        }
    }

    public void LoadNextScene()
    {
        Debug.Log("GM->LoadNextScene() from scene: " + gameState.currentScene.ToString());
        
        if (gameState.currentGameState == GameStates.Paused)
        {
            Debug.LogWarning("GM->LoadScene(): Game is paused, closing pause menu.");
            //playerState.SceneDestroyed();
            //gameState.SceneDestroyed();
            ClosePauseMenuAndResumeTime(true);
            SceneLoadingGameCleanup();
        }
    
        if (gameState.currentScene == Scenes.Game)
        {
            // get index of current scene
            int currentIndex = GameState.scenesSO.gameScenes.IndexOf(SceneManager.GetActiveScene().name);
            if (currentIndex == -1)
            {
                Debug.LogError("Current scene is marked as Game but not found in gameScenes list: " + SceneManager.GetActiveScene().name);
                currentIndex = 0; // default to first scene
            }
            if (currentIndex + 1 >= GameState.scenesSO.gameScenes.Count)
            {
                Debug.LogWarning("No next scene in gameScenes list, returning to main menu (maybe gameover(?)).");
                LoadScene(Scenes.MainMenu);
                return;
            }
            else
            {
                // These have a lifetime of one scene (maybe put in SceneDestroyed()?)
                gameState.currentSceneScript = null;
                uiManager = null;
                SceneManager.LoadScene(GameState.scenesSO.gameScenes[currentIndex + 1]);
                VerifyCurrentScene();
            }
        }
        else if (gameState.currentScene == Scenes.MainMenu)
        {
            Debug.Log("LoadNextScene() called from MainMenu, loading first game scene.");
            // These have a lifetime of one scene (maybe put in SceneDestroyed()?)
            gameState.currentSceneScript = null;
            uiManager = null;
            SceneManager.LoadScene(GameState.scenesSO.gameScenes[0]);
            VerifyCurrentScene();
            return;
        }
        else
        {
            Debug.LogWarning("LoadNextScene() called but current scene is not a Game scene: " + gameState.currentScene.ToString() +
                "; loading MainMenu scene.");
            // Optionally, could decide what to do based on currentScene (e.g. if MainMenu, start first game scene)
            LoadScene(Scenes.MainMenu);
            return;
        }
    }

    public void VerifyCurrentScene()
    {
        gameState.previousScene = gameState.currentScene;
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName == GameState.scenesSO.mainMenuScene)
        {
            if (gameState.currentScene != Scenes.MainMenu)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + gameState.currentScene.ToString() + "; updating to " + Scenes.MainMenu.ToString());
                gameState.currentScene = Scenes.MainMenu;
                gameState.currentGameState = GameStates.UI;
            }
        }
        else if (GameState.scenesSO.gameScenes.Contains(activeSceneName))
        {
            if (gameState.currentScene != Scenes.Game)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + gameState.currentScene.ToString() + "; updating to " + Scenes.Game.ToString());
                gameState.currentScene = Scenes.Game;
                gameState.currentGameState = GameStates.Playing;
                // Assuming gameScenes are ordered by level and start at Level1
                gameState.currentLevel = (GameLevels)GameState.scenesSO.gameScenes.IndexOf(activeSceneName) + 1;
            }
        }
        else if (activeSceneName == GameState.scenesSO.gameOverScene)
        {
            if (gameState.currentScene != Scenes.GameOver)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + gameState.currentScene.ToString() + "; updating to " + Scenes.GameOver.ToString());
                gameState.currentScene = Scenes.GameOver;
                gameState.currentGameState = GameStates.GameOver;
            }
        }
        else if (activeSceneName == GameState.scenesSO.DCExperimentsScene)
        {
            if (gameState.currentScene != Scenes.DCExperiments)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + gameState.currentScene.ToString() + "; updating to " + Scenes.DCExperiments.ToString());
                gameState.currentScene = Scenes.Game; // !!
                gameState.currentGameState = GameStates.Playing;
            }
        }
        else if (activeSceneName == GameState.scenesSO.UITestScene)
        {
            if (gameState.currentScene != Scenes.UITest)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + gameState.currentScene.ToString() + "; updating to " + Scenes.UITest.ToString());
                gameState.currentScene = Scenes.Game; // !!
                gameState.currentGameState = GameStates.Playing;
            }
        }
        else if (activeSceneName == GameState.scenesSO.UILayoutScene)
        {
            if (gameState.currentScene != Scenes.UILayout)
            {
                Debug.Log("currentScene mismatch; currentScene set to " + gameState.currentScene.ToString() + "; updating to " + Scenes.UILayout.ToString());
                gameState.currentScene = Scenes.UILayout;
                gameState.currentGameState = GameStates.UI;
            }
        }
        else
        {
            Debug.LogWarning("Active scene does not match any known scenes in ScenesSO: " + activeSceneName);
        }
        if (gameState.currentGameState == GameStates.UI)
        {
            // Ensure pause menu is closed when loading UI scenes
            if (uiManager != null)
            {
                uiManager.PauseMenuClose();
            }
            MouseCursorSetForUI();
        }
        else if (gameState.currentGameState == GameStates.Playing)
        {
            MouseCursorSetForGame();
        }
    }

    void SceneLoadingGameCleanup()
    {
        // The following are done in SceneDestroyed():
        //gameState.SceneDestroyed();
        //playerState.SceneDestroyed();
    }

   // Scene -> Scene script (in each level) calls the following Awake/Start/Destroyed functions
    public void SceneAwake(Scene sceneScript)
    {
        VerifyCurrentScene();
        Debug.Log("GM->SceneAwake() for scene: " + SceneManager.GetActiveScene().name + " currentScene: " + gameState.currentScene.ToString());
        gameState.currentSceneScript = sceneScript;
        if (gameState.currentScene == Scenes.Game)
        {
            // For now, we can have these in the level or created here if missing
            //! GameManager should be detached from this in the future
            if (uiManager == null)
            {
                var uIManagerGO = GameObject.Find("UIManager");
                if (uIManagerGO == null)
                {
                    //! Could be problematic depending on Editor-defined variables:
                    uIManagerGO = new GameObject("UIManager", typeof(UIManager));
                }
                uiManager = uIManagerGO.GetComponent<UIManager>();
            }
            // add scene to gameState.sceneProgressionInfo if not already there
            if (!gameState.sceneProgressionInfo.ContainsKey(SceneManager.GetActiveScene().name))
            {
                gameState.sceneProgressionInfo.Add(SceneManager.GetActiveScene().name, new List<string>());
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SceneStart()
    {
        Debug.Log("GM->SceneStart() for scene: " + SceneManager.GetActiveScene().name);
        if (gameState.currentScene == Scenes.Game)
        {
            //gameState.SceneStart();
            //playerState.SceneStart();            
        }
    }

    public void SceneDestroyed()
    {
        Debug.Log("GM->SceneDestroyed() for scene: " + SceneManager.GetActiveScene().name + " currentScene: " + gameState.currentScene.ToString());
        if (gameState.previousScene == Scenes.Game)
        {
            // Unsubscribe from events in game here

            //gameState.SceneDestroyed();
            //playerState.SceneDestroyed();
            // 'Unloading'?
            //currentGameState = GameStates.Loading;
            // These have a lifetime of one scene:
            gameState.currentSceneScript = null;
            uiManager = null;
        }
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.ExitPlaymode();
#else
        Application.Quit(); // For standalone builds
#endif
        Debug.Log("Player Has Quit the Game");
    }

    // Update is called once per frame
    //void Update() {}

    public void ModalDialogueSetIsOpen(bool mouseCursorForUI = true)
    {
        gameState.inGameModalDialogueActive = true;
        if (mouseCursorForUI)
        {
            MouseCursorSetForUI();
        }
    }
    public void ModalDialogueSetIsClosed(bool mouseCursorForGame = true)
    {
        gameState.inGameModalDialogueActive = false;
        if (mouseCursorForGame)
        {
            MouseCursorSetForGame();
        }
    }

    public bool IsModalDialogueActive()
    {
        return gameState.inGameModalDialogueActive;
    }

    public void ClosePauseMenuAndResumeTime(bool enableMouseCursorForGame = true)
    {
        uiManager.PauseMenuClose();
        if (timeScaleFreezeForPause)
        {
            Time.timeScale = 1f;
        }
        if (!IsModalDialogueActive())
        {
            if (enableMouseCursorForGame)
            {
                MouseCursorSetForGame();
            }
        }
        gameState.currentGameState = GameStates.Playing;
    }

    public void PauseGame()
    {
        if (gameState.currentGameState != GameStates.Playing)
        {
            Debug.LogWarning("GM->PauseGame(): Cannot pause, game is not in Playing state.");
            return;
        }
        //flipOutGame.GameEventSaveStateAndTransition(FlipOutGameEvents.Paused);

        // Show pause menu UI
        if (uiManager.PauseMenuOpen())
        {
            // enable mouse cursor for pause menu
            MouseCursorSetForUI();
            gameState.currentGameState = GameStates.Paused;

            // Freeze game time?
            if (timeScaleFreezeForPause)
            {
                Time.timeScale = 0f;
            }
        }
    }

    public void ResumeGame()
    {
        if (gameState.currentGameState != GameStates.Paused)
        {
            Debug.LogWarning("GM->ResumeGame(): Cannot resume, game is not in Paused state.");
            return;
        }
        //flipOutGame.GameEventRestoreState();
        // Hide pause menu UI
        ClosePauseMenuAndResumeTime(true);
    }

    public void MouseCursorSetForUI()
    {
        if (mouseHideForGameScenes)
        {
            //Debug.Log("GM->Setting mouse cursor for UI (visible/unlocked).");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    public void MouseCursorSetForGame()
    {
        if (mouseHideForGameScenes)
        {
            //Debug.Log("GM->Setting mouse cursor for Game (hidden/locked).");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

}
