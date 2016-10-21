using UnityEngine;
using System.Collections.Generic;

namespace UguiExtensions
{
    public static class FontUpdateTrackerEx
    {
        private static Dictionary<Font, List<TextEx>> m_Tracked = new Dictionary<Font, List<TextEx>>();

        public static void TrackText(TextEx t)
        {
            if (t.font == null)
                return;

            List<TextEx> exists;
            m_Tracked.TryGetValue(t.font, out exists);
            if (exists == null)
            {
                exists = new List<TextEx>();
                m_Tracked.Add(t.font, exists);

                Font.textureRebuilt += RebuildForFont;
            }

            for (int i = 0; i < exists.Count; i++)
            {
                if (exists[i] == t)
                    return;
            }

            exists.Add(t);
        }

        private static void RebuildForFont(Font f)
        {
            List<TextEx> texts;
            m_Tracked.TryGetValue(f, out texts);

            if (texts == null)
                return;

            for (var i = 0; i < texts.Count; i++)
                texts[i].FontTextureChanged();
        }

        public static void UntrackText(TextEx t)
        {
            if (t.font == null)
                return;

            List<TextEx> texts;
            m_Tracked.TryGetValue(t.font, out texts);

            if (texts == null)
                return;

            texts.Remove(t);
        }
    }
}