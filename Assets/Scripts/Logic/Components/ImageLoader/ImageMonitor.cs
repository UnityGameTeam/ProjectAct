using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Components
{
    public class ImageMonitor : MonoBehaviour
    {
        private RawImage m_RawImage;

        private void Awake()
        {
            if (m_RawImage == null)
            {
                m_RawImage = GetComponent<RawImage>();
            }
        }

        private void OnDisable()
        {
            if (m_RawImage != null)
            {
                m_RawImage.texture = null;
            }
        }
    }
}