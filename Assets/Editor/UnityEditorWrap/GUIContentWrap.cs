using System;
using UnityEngine;
using System.Reflection;

public class GUIContentWrap
{
    public static GUIContent Temp(string displayName)
    {
        Type type = typeof(GUIContent);
        var mf = type.GetMethod("Temp",
            BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
        return (GUIContent)mf.Invoke(null, new object[] { displayName });
    }
}
