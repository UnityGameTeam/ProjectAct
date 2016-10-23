using System;
using System.Reflection;
using UnityEngine;

public class GUIClipWrap
{
    public static Rect visibleRect
    {
        get
        {
            Type type = typeof(GameObject);
            type = type.Assembly.GetType("UnityEngine.GUIClip");
            var mf = type.GetProperty("visibleRect", BindingFlags.Public | BindingFlags.Static);
            return (Rect)mf.GetValue(null,null);
        }
    }

    public static void Push(Rect screenRect, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset)
    {
        Type type = typeof(GameObject);
        type = type.Assembly.GetType("UnityEngine.GUIClip");
        var mf = type.GetMethod("Push",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Rect), typeof(Vector2), typeof(Vector2), typeof(bool) }, null);
        mf.Invoke(null, new object[] { screenRect, scrollOffset, renderOffset, resetOffset});
    }

    public static void Pop()
    {
        Type type = typeof(GameObject);
        type = type.Assembly.GetType("UnityEngine.GUIClip");
        var mf = type.GetMethod("Pop",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] {}, null);
        mf.Invoke(null, new object[] { });
    }

    public static Vector2 Unclip(Vector2 pos)
    {
        Type type = typeof(GameObject);
        type = type.Assembly.GetType("UnityEngine.GUIClip");
        var mf = type.GetMethod("Unclip",
            BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(Vector2) }, null);
        return (Vector2)mf.Invoke(null, new object[] { pos }); ;
    }
}
