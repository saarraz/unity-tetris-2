#nullable enable
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MusicTempoController : MonoBehaviour
{
    public AudioSource? AudioSource;
    public AudioClip[]? TempoClips;

    public void UpTempo()
    {
        AudioClip? nextClip = (from clip in TempoClips where clip.length < AudioSource!.clip.length orderby clip.length descending select clip).FirstOrDefault();
        if (!nextClip)
        {
            return;
        }
        float relativePosition = AudioSource!.time / AudioSource!.clip.length;
        AudioSource!.clip = nextClip;
        AudioSource!.Play();
        AudioSource!.time = relativePosition * nextClip.length;
    }
}
