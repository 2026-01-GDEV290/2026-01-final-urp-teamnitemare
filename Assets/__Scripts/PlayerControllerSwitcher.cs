using UnityEngine;

public class PlayerControllerSwitcher : MonoBehaviour, ISaveable
{
    [Header("Player Pawn References")]
    public GameObject playerPawn1; // Pawn 1 with CharacterController + Camera
    public GameObject playerPawn2; // Pawn 2 with CharacterController + Camera

    [Header("Control Scripts")]
    public MonoBehaviour player1Controller; // Script controlling player1 movement
    public MonoBehaviour player2Controller; // Script controlling player2 movement

    [SerializeField] bool enableHotKeySwitching = true; // Set to false to disable hotkey switching

    private GameObject activePlayerPawn;

    void Start()
    {
        // Start with player1 active
        SetActivePlayer(playerPawn1, player1Controller);
    }

    void Update()
    {
        // // Example: Press Tab to switch
        if (enableHotKeySwitching && Input.GetKeyDown(KeyCode.Tab))
        {
            SwitchPlayer();
        }
    }

    public void SwitchPlayer()
    {
        if (activePlayerPawn == playerPawn1)
            SetActivePlayer(playerPawn2, player2Controller);
        else
            SetActivePlayer(playerPawn1, player1Controller);
    }

    void SetActivePlayer(GameObject newPlayer, MonoBehaviour newController)
    {
        // Disable both players' controls and cameras
        player1Controller.enabled = false;
        player2Controller.enabled = false;
        playerPawn1.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);
        playerPawn2.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);

        // Enable the new player's control and camera
        newController.enabled = true;
        newPlayer.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);

        // Update active player reference
        activePlayerPawn = newPlayer;
    }
#region ISaveable Implementation
    public object CaptureState()
    {
        return activePlayerPawn == playerPawn1 ? "Player1" : "Player2";
    }

    public void RestoreState(object state)
    {
        string activePlayerName = (string)state;
        if (activePlayerName == "Player1")
            SetActivePlayer(playerPawn1, player1Controller);
        else
            SetActivePlayer(playerPawn2, player2Controller);
    }
#endregion ISaveable Implementation
}
