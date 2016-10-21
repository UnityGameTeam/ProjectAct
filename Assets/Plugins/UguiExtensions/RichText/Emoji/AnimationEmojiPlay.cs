using System;
using System.Collections.Generic;
using UnityEngine;

namespace UguiExtensions
{
    /// <summary>
    /// 用于文本表情动画的播放，这里单独使用一个脚本的理由有三个
    /// 1、原先在TextEx使用协程来播放，不过每一次调用都有9b的gc产生
    /// 2、尝试过在TextEx中动态挂载脚本，由于TextEx是执行在编辑器模式的，在编辑器中停止游戏后不会使动态挂载的脚本消失，运行几次就有几个脚本
    /// 3、不直接使用TextEx中的Update函数来处理，因为如果没有表情动画的情况下，Update基本是在空转，这个就可以在没有动画表情时禁用脚本，禁止Update的调用
    /// </summary>
    public class AnimationEmojiPlay : MonoBehaviour
    {
        [NonSerialized] protected List<EmojiImage> m_EmojiImageList;

        public List<EmojiImage> EmojiImageList
        {
            get
            {
                if (m_EmojiImageList == null)
                {
                    m_EmojiImageList = new List<EmojiImage>();
                }
                return m_EmojiImageList;
            }
        }

        public void Clear()
        {
            if (m_EmojiImageList != null)
            {
                m_EmojiImageList.Clear();
            }
        }

        private void Update()
        {
            if (m_EmojiImageList != null)
            {
                for (int i = 0, count = m_EmojiImageList.Count; i < count; ++i)
                {
                    m_EmojiImageList[i].UpdateAnimationEmoji();
                }
            }
        }
    }
}