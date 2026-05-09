using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null || _bgmSource.clip == clip) return;
        _bgmSource.clip = clip;
        _bgmSource.loop = loop;
        _bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {   
        Debug.Log($"Playing SFX: {clip?.name ?? "null"}");
        
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }
}