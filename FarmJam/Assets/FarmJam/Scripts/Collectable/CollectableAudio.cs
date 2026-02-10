using System.Collections.Generic;
using UnityEngine;

public class CollectableAudio : MonoBehaviour
{
    public AudioSource AudioSource;
    public List<AudioClip> JumpClips;

    public void PlayJumpClip()
    {
        AudioSource.volume = .2f;
        AudioSource.PlayOneShot(JumpClips[Random.Range(0, JumpClips.Count)]);
    }
}
