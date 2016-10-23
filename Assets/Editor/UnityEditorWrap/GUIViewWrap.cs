using System;
using System.Reflection;
using UnityEngine;

public class GUIViewWrap
{
    public static object current
    {
        get
        {
            Type type = typeof(UnityEditor.Editor);
            type = type.Assembly.GetType("UnityEditor.GUIView");

            var mf = type.GetProperty("current",
                BindingFlags.Static | BindingFlags.Public);
            return mf.GetValue(null, null);
        }
    }

    public static void Repaint(object obj)
    {
        Type type = typeof(UnityEditor.Editor);
        type = type.Assembly.GetType("UnityEditor.GUIView");
        var mf = type.GetMethod("Repaint",
            BindingFlags.Public | BindingFlags.Instance, null, new Type[]{}, null);
        mf.Invoke(obj, null);
    }
}
