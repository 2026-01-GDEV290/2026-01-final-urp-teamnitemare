using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    
    public Button[] buttons;
    public InputField playerNameInput;

    public Slider mainVolumeSlider;
    public Toggle muteToggle;

    public void Awake()
    {
        /*
        foreach (Button b in buttons)
        {
            b.onClick.AddListener(ButtonSound);
        }
        playerNameInput.onEndEdit.AddListener(SetPlayerName);
        mainVolumeSlider.onValueChanged.AddListener(delegate { VolumeChange(); });
        muteToggle.onValueChanged.AddListener(delegate { MuteToggle(muteToggle.isOn); });
        */
    }

    void Start()
    {
        // playerNameInput.text = PlayerPreferences.Instance.playerName;
        // mainVolumeSlider.value = PlayerPreferences.Instance.mainVolume;
        // muteToggle.isOn = PlayerPreferences.Instance.mainMuted;
    }

    public void ButtonSound()
    {
        AudioManager.PlaySoundAt(AudioManager.uiAudioSourcesSO.UIMenuClick, 1f);
    }

    public void MuteToggle(bool isMuted)
    {
        //Debug.Log("Mute called, bool = " + isMuted);
        if (isMuted)
        {
            AudioManager.Mute();
            AudioListener.volume = 0f;
            PlayerPreferences.Instance.mainMuted = true;
            PlayerPreferences.Instance.SavePreferences();
        }
        else
        {
            AudioManager.UnMute();
            AudioListener.volume = PlayerPreferences.Instance.mainVolume;
            PlayerPreferences.Instance.mainMuted = false;
            PlayerPreferences.Instance.SavePreferences();
        }
    }

    public void VolumeChange()
    {
        PlayerPreferences.Instance.SetMainVolume(mainVolumeSlider.value);
        //Debug.Log("Volume changed to " + mainVolumeSlider.value);
    }

    public void PlayerBackButton()
    {

    }

    public void SetPlayerName(string name)
    {
        //InputField playerNameInput = GameObject.Find("PlayerNameInput").GetComponent<InputField>();
        GameManager.Instance.playerState.playerName = name;
        PlayerPreferences.Instance.SetPlayerName(name);
        //Debug.Log("Player name set to: " + name);
    }

    public void StartGameButton()
    {
        GameManager.Instance.LoadScene(Scenes.Game);
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
        GameManager.Quit();
    }
}
