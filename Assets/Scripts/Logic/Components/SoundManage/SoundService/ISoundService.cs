using UnityEngine;

namespace GameLogic.Components
{
    /// <summary>
    /// 优化：写一个基础基类，让UISoundService,BgSoundService,FxSoundService继承
    /// </summary>
    public interface ISoundService
    {
        bool playable { get; set; }
        bool mute { get; set; }
        void Update();
        void Play();
        void Stop();
        void Pause();
        void UnPause();
        void PlaySound(string clipName, float volume = 0.2f);
        void PlaySound(string clipName, float transtionTime, float volume);
        void PlaySound(AudioClip clip, float volume = 0.2f);
        void PlaySoundOnGameObject(GameObject go, AudioClip clip, float volume);
        void PlaySoundOnGameObject(GameObject go, string clipName, float volume);
        void StopSoundOnGameObject(GameObject go);
        void PauseSoundOnGameObject(GameObject go);
        void UnPauseSoundOnGameObject(GameObject go);
    }
}