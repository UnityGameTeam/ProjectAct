using UnityEditor;
using UnityEngine;

public class HorizontalSplitLine
{
    protected static Vector2 s_MouseDeltaReaderLastPos;

    protected float m_MinXPos;

    public float PositionX
    {
        get;
        protected set;
    }

    public HorizontalSplitLine(float posX, float minXPos)
    {
        PositionX = posX;
        m_MinXPos = minXPos;
    }

    public void OnGUI(float offsetX, float marginY, float height)
    {
        if (Event.current.type == EventType.Repaint)
        {
            Color color = GUI.color;
            GUI.color *= !EditorGUIUtility.isProSkin ? new Color(0.6f, 0.6f, 0.6f, 1.333f) : new Color(0.12f, 0.12f, 0.12f, 1.333f);
            GUI.DrawTexture(new Rect(PositionX - offsetX, marginY, 1f, height), EditorGUIUtility.whiteTexture);
            GUI.color = color;
        }
    }

    public void ResizeHandling(float marginTop, float width, float height, float minOwnerWidth = 0)
    {
        Rect position = new Rect(PositionX, marginTop, 5f, height);
        if (Event.current.type == EventType.Repaint)
            EditorGUIUtility.AddCursorRect(position, MouseCursor.SplitResizeLeftRight);

        float newPos = 0.0f;
        float mouseDelta = MouseDeltaReader(position, true).x;
        if (!Mathf.Approximately(mouseDelta, 0f))
        {
            PositionX += mouseDelta;
            newPos = Mathf.Clamp(PositionX, m_MinXPos, width - m_MinXPos);
        }

        float delta = minOwnerWidth - m_MinXPos;
        if (width - m_MinXPos < delta)
            newPos = width - delta;

        if (newPos > 0.0)
            PositionX = newPos;
    }

    internal static Vector2 MouseDeltaReader(Rect position, bool activated)
    {
        int controlId = GUIUtility.GetControlID("MouseDeltaReader".GetHashCode(), FocusType.Passive, position);
        Event current = Event.current;
        switch (current.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (activated && GUIUtility.hotControl == 0 && (position.Contains(current.mousePosition) && current.button == 0))
                {
                    GUIUtility.hotControl = controlId;
                    GUIUtility.keyboardControl = 0;
                    s_MouseDeltaReaderLastPos = GUIClipWrap.Unclip(current.mousePosition);
                    current.Use();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlId && current.button == 0)
                {
                    GUIUtility.hotControl = 0;
                    current.Use();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlId)
                {
                    Vector2 vector2_1 = GUIClipWrap.Unclip(current.mousePosition);
                    Vector2 vector2_2 = vector2_1 - s_MouseDeltaReaderLastPos;
                    s_MouseDeltaReaderLastPos = vector2_1;
                    current.Use();
                    return vector2_2;
                }
                break;
        }
        return Vector2.zero;
    }
}
