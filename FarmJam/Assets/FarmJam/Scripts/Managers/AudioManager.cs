using Signals;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource AudioSource;
    public AudioClip GameFailAudioClip;
    public AudioClip GameWinAudioClip;
    public AudioClip BackgroundAudioClip;

    public void Init()
    {
        AudioSignals.OnGameFailSoundPlay.AddListener(PlayFailSound);
        AudioSignals.OnGameWinSoundPlay.AddListener(PlayWinSound);

        PlayBackgroundSound();
    }

    public void Disable()
    {
        AudioSignals.OnGameFailSoundPlay.RemoveListener(PlayFailSound);
        AudioSignals.OnGameWinSoundPlay.RemoveListener(PlayWinSound);
    }

    public void PlayBackgroundSound()
    {
        AudioSource.clip = BackgroundAudioClip;
        AudioSource.volume = .075f;
        AudioSource.Play();
    }
    
    public void PlayWinSound()
    {
        AudioSource.clip = GameWinAudioClip;
        AudioSource.volume = .3f;
        AudioSource.Play();
    }
    
    public void PlayFailSound()
    {
        AudioSource.clip = GameFailAudioClip;
        AudioSource.volume = .3f;
        AudioSource.Play();
    }
}
