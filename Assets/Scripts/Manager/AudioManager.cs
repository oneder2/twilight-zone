using UnityEngine;

public class AudioManager : Singleton<AudioManager>
{
    private AudioSource backgroundMusicSource; // 背景音乐
    private AudioSource sfxSource;            // 音效

    override protected void Awake()
    {
        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.loop = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false; // 音效不循环
    }

    public void PlayBackgroundMusic(AudioClip clip)
    {
        if (clip != null)
        {
            backgroundMusicSource.clip = clip;
            backgroundMusicSource.Play();
        }
        else
        {
            backgroundMusicSource.Stop();
        }
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip); // 播放一次性音效
        }
    }

    public void SetMusicVolume(float volume)
    {
        backgroundMusicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}