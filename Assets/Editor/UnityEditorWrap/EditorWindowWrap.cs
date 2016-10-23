using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class EditorWindowWrap
{
    public static bool HasFocus(EditorWindow window)
    {
        var fi = typeof(EditorWindow).GetField("m_Parent", BindingFlags.NonPublic | BindingFlags.Instance);
        var obj = fi.GetValue(window);
        var p = obj.GetType().GetProperty("hasFocus", BindingFlags.Public | BindingFlags.Instance);
        return (bool)p.GetValue(obj, null);
    }
}
