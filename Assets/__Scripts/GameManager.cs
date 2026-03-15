using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
//using TMPro;



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
    public GameState gameState = new GameState();
    public PlayerState playerState = new PlayerState();


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
            uiManager.PauseMenuClose();
            //playerState.SceneDestroyed();
            //gameState.SceneDestroyed();
            SceneLoadingGameCleanup();
        }
        if (gameState.currentGameState == GameStates.Playing)
        {
            SceneLoadingGameCleanup();
        }
        gameState.previousScene = gameState.currentScene;
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
    }

    void SceneLoadingGameCleanup()
    {
        // The following are done in SceneDestroyed():
        //gameState.SceneDestroyed();
        //playerState.SceneDestroyed();
    }

   // Scene -> Scene script (in each level) calls the following Awake/Start/Destroyed functions
    public void SceneAwake()
    {
        VerifyCurrentScene();
        Debug.Log("GM->SceneAwake() for scene: " + SceneManager.GetActiveScene().name + " currentScene: " + gameState.currentScene.ToString());
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
    void Update()
    {

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
        uiManager.PauseMenuOpen();
        gameState.currentGameState = GameStates.Paused;
        // Freeze game time
        //Time.timeScale = 0f;
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
        uiManager.PauseMenuClose();
        gameState.currentGameState = GameStates.Playing;
        // Resume game time
        //Time.timeScale = 1f;
    }

}
