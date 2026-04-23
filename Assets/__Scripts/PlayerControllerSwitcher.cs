using UnityEngine;

public class PlayerControllerSwitcher : MonoBehaviour
{
    [Header("Player Pawn References")]
    public GameObject player1; // Pawn 1 with CharacterController + Camera
    public GameObject player2; // Pawn 2 with CharacterController + Camera

    [Header("Control Scripts")]
    public MonoBehaviour player1Controller; // Script controlling player1 movement
    public MonoBehaviour player2Controller; // Script controlling player2 movement

    private GameObject activePlayer;

    void Start()
    {
        // Start with player1 active
        SetActivePlayer(player1, player1Controller);
    }

    void Update()
    {
        // // Example: Press Tab to switch
        // if (Input.GetKeyDown(KeyCode.Tab))
        // {
        //     if (activePlayer == player1)
        //         SetActivePlayer(player2, player2Controller);
        //     else
        //         SetActivePlayer(player1, player1Controller);
        // }
    }

    void SetActivePlayer(GameObject newPlayer, MonoBehaviour newController)
    {
        // Disable both players' controls and cameras
        player1Controller.enabled = false;
        player2Controller.enabled = false;
        player1.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);
        player2.GetComponentInChildren<Camera>(true).gameObject.SetActive(false);

        // Enable the new player's control and camera
        newController.enabled = true;
        newPlayer.GetComponentInChildren<Camera>(true).gameObject.SetActive(true);

        // Update active player reference
        activePlayer = newPlayer;
    }
}
