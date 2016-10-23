using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class EditorUtilityWrap
{
    public static void SetTemporarilyAllowIndieRenderTexture(bool allow)
    {
        Type editorUtilityType = typeof(EditorUtility);
        var mi = editorUtilityType.GetMethod("SetTemporarilyAllowIndieRenderTexture",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(bool) }, null);
        mi.Invoke(null, new object[] { allow });
    }

    public static Matrix4x4 s_InverseMatrix
    {
        get
        {
            Type type = typeof(Handles);
            var pi = type.GetField("s_InverseMatrix", BindingFlags.Static | BindingFlags.NonPublic);

            return (Matrix4x4)pi.GetValue(null);
        }
    }

    public static string GetInvalidFilenameChars()
    {
        Type editorUtilityType = typeof(EditorUtility);
        var mi = editorUtilityType.GetMethod("GetInvalidFilenameChars",
            BindingFlags.Static | BindingFlags.NonPublic, null, null, null);
        return mi.Invoke(null, null) as string;
    }
}
