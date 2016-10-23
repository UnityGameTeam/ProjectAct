using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SpriteRectCache : ScriptableObject
{
    [SerializeField]
    public List<SpriteRect> m_Rects;

    public int Count
    {
        get
        {
            if (m_Rects != null)
                return m_Rects.Count;
            return 0;
        }
    }

    public SpriteRect RectAt(int i)
    {
        if (i >= Count)
            return null;
        return m_Rects[i];
    }

    public void AddRect(SpriteRect r)
    {
        if (m_Rects == null)
            return;
        m_Rects.Add(r);
    }

    public void RemoveRect(SpriteRect r)
    {
        if (m_Rects == null)
            return;
        m_Rects.Remove(r);
    }

    public void ClearAll()
    {
        if (m_Rects == null)
            return;
        m_Rects.Clear();
    }

    public int GetIndex(SpriteRect spriteRect)
    {
        if (m_Rects != null)
        {
            return m_Rects.FindIndex((sp)=> { return sp == spriteRect; });
        }
        return 0;
    }

    public bool Contains(SpriteRect spriteRect)
    {
        if (m_Rects != null)
            return m_Rects.Contains(spriteRect);

        return false;
    }

    private void OnEnable()
    {
        if (m_Rects != null)
            return;

        m_Rects = new List<SpriteRect>();
    }
}
