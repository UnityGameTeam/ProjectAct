using UnityEngine;

namespace GameLogic.Components
{
    public class AudioSourceWrap : MonoBehaviour
    {
        protected int          m_Version;     //用于解决同步问题
        protected AudioSource  m_AudioSource;
        protected object       m_Tag;
        
        public int version
        {
            get { return m_Version;  }
            set { m_Version = value; }
        }

        public bool isPlaying
        {
            get { return audioSource.isPlaying; }
        }

        public bool loop
        {
            get { return audioSource.loop; }
            set { audioSource.loop = value; }
        }

        public float volume
        {
            get { return audioSource.volume; }
            set { audioSource.volume = value; }
        }

        public float time
        {
            get { return audioSource.time; }
        }

        public AudioClip clip
        {
            get { return audioSource.clip; }
        }

        public bool mute
        {
            get { return audioSource.mute; }
            set { audioSource.mute = value; }
        }

        public object tag
        {
            get { return m_Tag; }
            set { m_Tag = value; }
        }

        protected AudioSource audioSource
        {
            get
            {
                if (m_AudioSource == null)
                    m_AudioSource = gameObject.AddComponent<AudioSource>();
                return m_AudioSource;
            }
        }

        public void Play(AudioClip clip, int version)
        {
            audioSource.clip = clip;
            if (version == m_Version)
            {
                audioSource.Play();
            }
        }

        public void UnPause()
        {
            audioSource.UnPause();
        }

        public void Pause()
        {
            ++m_Version;
            audioSource.Pause();
        }

        public void Stop()
        {
            ++m_Version;
            audioSource.Stop();
        }
    }
}
