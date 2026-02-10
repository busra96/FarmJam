using UnityEngine;

public class EmptyBoxAudio : MonoBehaviour
{
    public AudioSource AudioSource;

    public AudioClip SelectClip;
    public AudioClip WinClip;
    public AudioClip FailClip;
    public AudioClip BackClip;
    public AudioClip SpawnClip;

    public void PlayAudioClip(EmptyBoxAudioClipType emptyBoxAudioClipType)
    {
        AudioSource.volume = .2f;
        AudioSource.PlayOneShot(ReturnAudioClip(emptyBoxAudioClipType));
    }
    
    public AudioClip ReturnAudioClip(EmptyBoxAudioClipType emptyBoxAudioClipType)
    {
        switch (emptyBoxAudioClipType)
        {
            case EmptyBoxAudioClipType.Select:
                return SelectClip;
            case EmptyBoxAudioClipType.Win:
                return WinClip;
            case EmptyBoxAudioClipType.Fail:
                return FailClip;
            case EmptyBoxAudioClipType.Back:
                return BackClip;
            case EmptyBoxAudioClipType.Spawn:
                return SpawnClip;
            default:
                return null;
        }
    }
}


public enum EmptyBoxAudioClipType
{
    Select,
    Win,
    Fail,
    Back,
    Spawn
}