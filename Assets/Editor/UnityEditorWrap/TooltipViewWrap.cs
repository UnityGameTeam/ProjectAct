using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TooltipViewWrap
{
    public static void Show(string tooltip, Rect rect)
    {
        Type type = typeof(Editor);
        type = type.Assembly.GetType("UnityEditor.TooltipView");
        var mf = type.GetMethod("Show", BindingFlags.Public | BindingFlags.Static, null, new Type[] {typeof(string),typeof(Rect) }, null);
        mf.Invoke(null,new object[] { tooltip ,rect});
    }

    public static void Close()
    {
        Type type = typeof(Editor);
        type = type.Assembly.GetType("UnityEditor.TooltipView");
        var mf = type.GetMethod("Close", BindingFlags.Public | BindingFlags.Static, null, new Type[] {}, null);
        mf.Invoke(null, null);
    }
}
