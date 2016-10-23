using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class EditorGUIUtilityWrap
{
    public static void SetRenderTextureNoViewport(RenderTexture rt)
    {
        Type editorGUIUtility = typeof (EditorGUIUtility);
        var mf = editorGUIUtility.GetMethod("SetRenderTextureNoViewport",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] {typeof (RenderTexture)}, null);
        mf.Invoke(null, new object[] {rt});
    }

    public static GUIContent TextContent(string name)
    {
        Type editorGUIUtility = typeof(EditorGUIUtility);
        var mf = editorGUIUtility.GetMethod("TextContent",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
        return (GUIContent)mf.Invoke(null, new object[] { name });
    }

    public static bool HasHolddownKeyModifiers(Event evt)
    {
        Type editorGUIUtility = typeof(EditorGUIUtility);
        var mf = editorGUIUtility.GetMethod("HasHolddownKeyModifiers",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(Event) }, null);
        return (bool)mf.Invoke(null, new object[] { evt });
    }
}
