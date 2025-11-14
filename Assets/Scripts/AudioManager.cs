using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private Sound[] backgroundClips;
    [SerializeField] private Sound[] oneShotClips;

    private Dictionary<string, AudioClip> backgroundLibrary = new();
    private Dictionary<string, AudioClip> oneShotLibrary = new();

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;

        InitLibrary();
    }

    private void InitLibrary()
    {
        foreach (Sound s in backgroundClips)
            backgroundLibrary[s.name] = s.clip;

        foreach (Sound s in oneShotClips)
            oneShotLibrary[s.name] = s.clip;
    }

    public void PlayBackground(string name, float volume = 1f)
    {
        if (!backgroundLibrary.TryGetValue(name, out var clip))
            return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void PlayOneShot(string name, float volume = 1f)
    {
        if (!oneShotLibrary.TryGetValue(name, out var clip))
            return;

        Debug.Log($"Playing SFX: {name}");
        sfxSource.PlayOneShot(clip, volume);
    }
}

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;
}
