using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGameButton()
    {
        SceneManager.LoadScene("Dialogue System 2");
    }

    public void OptionsButton()
    {
        //GameManager.Instance.LoadScene(Scenes.Options);
    }

    public void ContinueButton()
    {
        //GameManager.Instance.LoadScene(Scenes.Game);
    }

    public void QuitButton()
    {
#if (UNITY_EDITOR)
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
