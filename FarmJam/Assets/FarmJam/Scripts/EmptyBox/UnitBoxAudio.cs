using UnityEngine;

public class UnitBoxAudio : MonoBehaviour
{
    public AudioSource AudioSource;
    public AudioClip WinClip;

    public void PlayWinClip()
    {
        AudioSource.volume = .2f;
        AudioSource.pitch = 2f;
        AudioSource.PlayOneShot(WinClip);
    }
}
