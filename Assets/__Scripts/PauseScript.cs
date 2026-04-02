using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public void ResumeGamePressed()
    {
        Debug.Log("Resume menu button!");
        //GameManager.Instance.uiManager.PauseMenuClose();
        //var inputManager = GameObject.FindFirstObjectByType<InputManager>();
        //if (inputManager != null)
        //{            
         //   inputManager.GetComponent<InputManager>().PauseMenuClose();
        //}
        GameManager.Instance.ResumeGame();
    }
    public void MainMenuButtonPressed()
    {
        Debug.Log("Main menu button!");
        //GameManager.Instance.UnpauseAndRestoreCursor();
        //GameManager.Instance.ResumeGame();
        // LoadScene detects and does this:
        //GameManager.Instance.ClosePauseMenuAndResumeTime();
        GameManager.Instance.LoadScene(Scenes.MainMenu);
    }
}
