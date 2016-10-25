using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Components
{
    public enum SoundServiceId
    {
        UISound = 0,  //UI
        FxSound,      //音效
        BGSound,      //背景
    }

    public class SoundManager
    {
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SoundManager();
                }
                return _instance;
            }
        }

        protected List<ISoundService> m_SoundServiceList = new List<ISoundService>(); 

        public void Update()
        {
            for (int i = 0; i < m_SoundServiceList.Count; ++i)
            {
                if (m_SoundServiceList[i] != null)
                {
                    m_SoundServiceList[i].Update();
                }
            }
        }

        public bool AddSoundServie(int serviceId, ISoundService soundService)
        {
            if (serviceId < 0 || soundService == null)
            {
                return false;
            }

            if (serviceId >= m_SoundServiceList.Count)
            {
                for (int i = 0, count = serviceId - m_SoundServiceList.Count; i < count; ++i)
                {
                    m_SoundServiceList.Add(null);
                }
                m_SoundServiceList.Add(soundService);
                return true;
            }

            if (m_SoundServiceList[serviceId] == null)
            {
                m_SoundServiceList[serviceId] = soundService;
                return true;
            }
            return false;
        }

        public ISoundService GetSoundServie(int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return null;
            }

            return m_SoundServiceList[serviceId];
        }

        public void RemoveSoundServie(int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            m_SoundServiceList[serviceId] = null;
        }

        public void SetPlayableSoundService(bool playable,int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].playable = playable;
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.playable = playable;
            }
        }

        public void MuteSoundService(int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].mute = true;
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.mute = true;
            }
        }

        public void UnmuteSoundService(int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].mute = false;
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.mute = false;
            }
        }

        public void PlaySoundService(int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].Play();
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.Play();
            }
        }

        public void StopSoundService(int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].Stop();
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.Stop();
            }
        }

        public void PauseSoundService(int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].Pause();
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.Pause();
            }
        }

        public void UnPauseSoundService(int serviceId = -1)
        {
            if (serviceId >= m_SoundServiceList.Count)
            {
                return;
            }

            if (serviceId < 0)
            {
                for (int i = 0; i < m_SoundServiceList.Count; ++i)
                {
                    if (m_SoundServiceList[i] != null)
                    {
                        m_SoundServiceList[i].UnPause();
                    }
                }
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.UnPause();
            }
        }

        public void PlaySound(string clipName, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.PlaySound(clipName);
            }
        }

        public void PlaySound(AudioClip clip, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.PlaySound(clip);
            }
        }

        public void PlaySoundOnGameObject(GameObject go, AudioClip clip, float volume, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.PlaySoundOnGameObject(go, clip, volume);
            }
        }

        public void PlaySoundOnGameObject(GameObject go, string clipName, float volume, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.PlaySoundOnGameObject(go, clipName, volume);
            }
        }

        public void StopSoundOnGameObject(GameObject go, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.StopSoundOnGameObject(go);
            }
        }

        public void PauseSoundOnGameObject(GameObject go, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.PauseSoundOnGameObject(go);
            }
        }

        public void UnPauseSoundOnGameObject(GameObject go, int serviceId)
        {
            if (serviceId >= m_SoundServiceList.Count || serviceId < 0)
            {
                return;
            }

            var soundService = GetSoundServie(serviceId);
            if (soundService != null)
            {
                soundService.UnPauseSoundOnGameObject(go);
            }
        }
    }
}