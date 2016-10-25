using UguiExtensions;
using UnityEngine;

namespace GameLogic.Components
{
    public class BgSoundService : ISoundService
    {
        protected bool m_Playable = true;
        protected bool m_Mute;

        protected GameObject      m_AudioObjRoot;
        protected AudioSourceWrap m_AudioSourceWrap;

        protected int m_PlayState = 0;   //0正常播放 1过渡中
        protected int m_PlayVersion = 0; 

        protected string m_PlayingClipName;

        protected string m_WaitingClipName;
        protected float  m_TargetVolume;
        protected float  m_TranstionStartTime;
        protected float  m_TranstionTime;

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
                    if (m_AudioSourceWrap != null)
                    {
                        m_AudioSourceWrap.mute = m_Mute;
                    }
                }
            }
        }

        public BgSoundService(GameObject root)
        {
            m_AudioObjRoot = root;
        }

        public void Update()
        {
            if (m_AudioSourceWrap != null && m_AudioSourceWrap.isPlaying)
            {
                if (m_PlayState == 0)
                {
                    if (m_TranstionTime > 0.05f && m_AudioSourceWrap.clip != null && m_AudioSourceWrap.time >= m_AudioSourceWrap.clip.length - m_TranstionTime)
                    {
                        m_TranstionStartTime = m_TranstionTime;
                        m_PlayState = 1;
                    }
                }
                else if (m_PlayState == 1)
                {
                    if (m_TranstionStartTime > 0)
                    {
                        var volume = m_TranstionStartTime / m_TranstionTime * m_TargetVolume;
                        m_AudioSourceWrap.volume = Mathf.Max(0, volume);

                        m_TranstionStartTime -= Time.unscaledDeltaTime;
                        if (m_TranstionStartTime < 0)
                        {
                            m_PlayState = 2;
                            m_AudioSourceWrap.volume = 0;

                            var version = ++m_AudioSourceWrap.version;
                            var playVersion = ++m_PlayVersion;
                            AssetManager.Instance.LoadAssetAsync(m_WaitingClipName, (asset) =>
                            {
                                if (playVersion == m_PlayVersion)
                                {
                                    m_PlayState = 0;
                                    if (m_Playable && asset != null && asset is AudioClip)
                                    {
                                        m_AudioSourceWrap.volume = m_TargetVolume;
                                        m_AudioSourceWrap.Play(asset as AudioClip, version);
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        public void Play()
        {
            if (!m_Playable)
                return;

            if (m_AudioSourceWrap != null && !m_AudioSourceWrap.isPlaying)
            {
                m_AudioSourceWrap.Play(m_AudioSourceWrap.clip, m_AudioSourceWrap.version);
            }
        }

        public void Stop()
        {
            if (m_AudioSourceWrap != null)
            {
                m_AudioSourceWrap.Stop();
            }
        }

        public void Pause()
        {
            if (!m_Playable)
                return;

            if (m_AudioSourceWrap != null)
            {
                m_AudioSourceWrap.Pause();
            }
        }

        public void UnPause()
        {
            if (!m_Playable)
                return;

            if (m_AudioSourceWrap != null)
            {
                m_AudioSourceWrap.UnPause();
            }
        }

        public void PlaySound(string clipName, float volume = 0.2f)
        {
            PlaySound(clipName, 1, volume);
        }

        public void PlaySound(string clipName, float transtionTime, float volume)
        {
            if (!m_Playable)
                return;

            var audioSourceWrap = GetIdleAudioSourceWrap();
            if (m_PlayState == 0)
            {
                if (m_PlayingClipName == null)
                {
                    m_PlayingClipName = clipName;
                    m_WaitingClipName = clipName;
                    m_TranstionTime = transtionTime;
                    m_TargetVolume = volume;

                    var version = ++audioSourceWrap.version;
                    AssetManager.Instance.LoadAssetAsync(clipName, (asset) =>
                    {
                        if (m_Playable && asset != null && asset is AudioClip)
                        {
                            audioSourceWrap.volume = volume;
                            audioSourceWrap.Play(asset as AudioClip, version);
                        }
                    });
                    return;
                }

                if (m_PlayingClipName != null && m_PlayingClipName != clipName)
                {
                    m_PlayingClipName = clipName;
                    m_WaitingClipName = clipName;
                    m_TranstionTime = transtionTime;
                    m_TargetVolume = volume;
                    m_PlayState = 1;
                    m_TranstionStartTime = m_TranstionTime;
                }
            }
            else if(m_PlayState == 1)
            {
                m_PlayingClipName = clipName;
                m_WaitingClipName = clipName;
                m_TranstionTime = transtionTime;
                m_TargetVolume = volume;
            }
            else if(m_PlayState == 2)
            {
                m_PlayingClipName = clipName;
                m_WaitingClipName = clipName;
                m_TranstionTime = transtionTime;
                m_TargetVolume = volume;

                var version = ++audioSourceWrap.version;
                var playVersion = ++m_PlayVersion;
                AssetManager.Instance.LoadAssetAsync(m_WaitingClipName, (asset) =>
                {
                    if (playVersion == m_PlayVersion)
                    {
                        m_PlayState = 0;
                        if (m_Playable && asset != null && asset is AudioClip)
                        {
                            audioSourceWrap.volume = m_TargetVolume;
                            audioSourceWrap.Play(asset as AudioClip, version);
                        }
                    }
                });
            }
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

        protected AudioSourceWrap GetIdleAudioSourceWrap()
        {
            if (m_AudioSourceWrap != null)
                return m_AudioSourceWrap;

            var go = new GameObject("BgSoundService");
            go.transform.parent = m_AudioObjRoot.transform;
            m_AudioSourceWrap = go.AddComponent<AudioSourceWrap>();
            m_AudioSourceWrap.loop = true;
            m_AudioSourceWrap.mute = m_Mute;
            return m_AudioSourceWrap;
        }
    }
}