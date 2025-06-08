using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public AudioClip bgMusic;
    public AudioClip enter, flip, match, mismatch, gameover;

    private AudioSource sfxSource;
    private AudioSource bgmSource;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // Create two audio sources: one for BGM, one for SFX
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length < 2)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            bgmSource = gameObject.AddComponent<AudioSource>();
        }
        else
        {
            sfxSource = sources[0];
            bgmSource = sources[1];
        }

        bgmSource.loop = true;
        bgmSource.volume = 0.5f;
        bgmSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
    }

    void Start()
    {
        if (bgMusic != null)
        {
            bgmSource.clip = bgMusic;
            bgmSource.Play();
        }
    }

    public void PlayEnter() => sfxSource.PlayOneShot(enter);
    public void PlayFlip() => sfxSource.PlayOneShot(flip);
    public void PlayMatch() => sfxSource.PlayOneShot(match);
    public void PlayMismatch() => sfxSource.PlayOneShot(mismatch);
    public void PlayGameOver() => sfxSource.PlayOneShot(gameover);
}
