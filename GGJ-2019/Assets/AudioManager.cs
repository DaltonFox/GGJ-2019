using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {

    private AudioSource mainLoopSource;
    private AudioSource endLoopSource;
    private AudioSource currentSource;

    public Vector2 volumeLevels;
    private float currentVolume;

    public IEnumerator FadeOut(AudioSource audioSource, float FadeTime)
    {
        float startVolume = audioSource.volume;

        while (audioSource.volume > 0)
        {
            audioSource.volume -= startVolume * Time.deltaTime / FadeTime;

            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public IEnumerator FadeIn(AudioSource audioSource, float FadeTime)
    {
        float startVolume = 0.05f;
        audioSource.volume = 0;
        audioSource.Play();

        while (audioSource.volume < currentVolume)
        {
            audioSource.volume += startVolume * Time.deltaTime / FadeTime;

            yield return null;
        }

        audioSource.volume = currentVolume;
        setCurrentSource(audioSource);
    }

    private void Start()
    {
        AudioSource[] sources = gameObject.GetComponents<AudioSource>();
        mainLoopSource = sources[0];
        endLoopSource = sources[1];
        currentSource = mainLoopSource;
        currentVolume = volumeLevels[0];
    }

    public void startMainLoop()
    {
        currentVolume = volumeLevels[0];
        StartCoroutine(FadeOut(endLoopSource, 0.5f));
        StartCoroutine(FadeIn(mainLoopSource, 6f));
    }

    public void startEndLoop()
    {
        currentVolume = volumeLevels[1];
        StartCoroutine(FadeOut(mainLoopSource, 0.25f));
        StartCoroutine(FadeIn(endLoopSource, 0.25f));
    }

    public void setCurrentSource(AudioSource audiosource)
    {
        currentSource = audiosource;
    }
}
