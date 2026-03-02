using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public Slider volumeSlider;
    public Toggle audioToggle;

    void Start()
    {
        volumeSlider.value=AudioManager.Instance?.volume??0;
        audioToggle.SetIsOnWithoutNotify(AudioManager.Instance?.audioEnabled??false);

        volumeSlider.onValueChanged.AddListener(UpdateVolume);
        audioToggle.onValueChanged.AddListener(OnToggleAudio);
    }

    void OnDestroy()
    {
        volumeSlider.onValueChanged.RemoveListener(UpdateVolume);
        audioToggle.onValueChanged.RemoveListener(OnToggleAudio);
    }

    public void UpdateVolume(float volume)
    {
        AudioManager.Instance?.SetVolume(volume);
    }

    public void OnBackButtonClick()
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        GameManager.Instance?.LoadScene("MainScene");
    }

    public void OnToggleAudio(bool isOn)
    {
        AudioManager.Instance?.PlayOneShot("notification_ok");
        AudioManager.Instance?.ToggleAudio();

    }
}