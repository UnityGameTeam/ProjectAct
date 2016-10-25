using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Components
{
    public class UISoundService : ISoundService
    {
        protected bool m_Playable = true;
        protected bool m_Mute;

        protected List<AudioSourceWrap> m_AudioSourceList = new List<AudioSourceWrap>();
        protected GameObject m_AudioObjRoot;

        public bool playable
        {
            get { return m_Playable; }
            set
            {
                if (m_Playable != value)
                {
                    m_Playable = value;
                    if (!m_Playable)
                    {
                        Stop();
                    }
                }
            }
        }

        public bool mute
        {
            get { return m_Mute; }
            set
            {
                if (m_Mute != value)
                {
                    m_Mute = value;
                    for (int i = 0; i < m_AudioSourceList.Count; ++i)
                    {
                        if (m_AudioSourceList[i] != null)
                        {
                            m_AudioSourceList[i].mute = m_Mute;
                        }
                    }
                }
            }
        }

        public UISoundService(GameObject root)
        {
            m_AudioObjRoot = root;
        }

        public void Update()
        {

        }

        public void Play()
        {

        }

        public void Stop()
        {
            for (int i = 0; i < m_AudioSourceList.Count; ++i)
            {
                if (m_AudioSourceList[i] != null)
                {
                    m_AudioSourceList[i].Stop();
                }
            }
        }

        public void Pause()
        {
            if (!m_Playable)
                return;

            for (int i = 0; i < m_AudioSourceList.Count; ++i)
            {
                if (m_AudioSourceList[i] != null)
                {
                    m_AudioSourceList[i].Pause();
                }
            }
        }

        public void UnPause()
        {
            if (!m_Playable)
                return;

            for (int i = 0; i < m_AudioSourceList.Count; ++i)
            {
                if (m_AudioSourceList[i] != null)
                {
                    m_AudioSourceList[i].UnPause();
                }
            }
        }

        public void PlaySound(string clipName, float volume = 0.2f)
        {
            if (!m_Playable)
                return;

            var audioSourceWrap = GetIdleAudioSourceWrap(clipName);
            var version = ++audioSourceWrap.version;
            AssetManager.Instance.LoadAssetAsync(clipName, (asset) =>
            {
                if (m_Playable && asset != null && asset is AudioClip)
                {
                    audioSourceWrap.volume = volume;
                    audioSourceWrap.Play(asset as AudioClip, version);
                }
            });
        }

        public void PlaySound(string clipName, float transtionTime, float volume)
        {
            throw new System.NotImplementedException();
        }

        public void PlaySound(AudioClip clip, float volume = 1)
        {
            throw new System.NotImplementedException();
        }

        public void PlaySoundOnGameObject(GameObject go, AudioClip clip, float volume)
        {
            throw new System.NotImplementedException();
        }

        public void PlaySoundOnGameObject(GameObject go, string clipName, float volume)
        {
            throw new System.NotImplementedException();
        }

        public void StopSoundOnGameObject(GameObject go)
        {
            throw new System.NotImplementedException();
        }

        public void PauseSoundOnGameObject(GameObject go)
        {
            throw new System.NotImplementedException();
        }

        public void UnPauseSoundOnGameObject(GameObject go)
        {
            throw new System.NotImplementedException();
        }

        protected AudioSourceWrap GetIdleAudioSourceWrap(string clipName)
        {
            for (int i = 0; i < m_AudioSourceList.Count; ++i)
            {
                if (m_AudioSourceList[i] != null && m_AudioSourceList[i].name == clipName)
                {
                    return m_AudioSourceList[i];
                }
            }

            var go = new GameObject(clipName);
            go.transform.parent = m_AudioObjRoot.transform;
            var audioSourceWrap = go.AddComponent<AudioSourceWrap>();
            audioSourceWrap.loop = false;
            audioSourceWrap.mute = m_Mute;
            m_AudioSourceList.Add(audioSourceWrap);
            return audioSourceWrap;
        }
    }
}