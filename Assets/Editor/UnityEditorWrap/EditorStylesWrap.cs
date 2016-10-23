using System;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEditor;

public class EditorStylesWrap
{
    public static GUIStyle inspectorBig
    {
        get
        {
            Type type = typeof (EditorStyles);
            var pi = type.GetProperty("inspectorBig",BindingFlags.Static | BindingFlags.NonPublic);

            return (GUIStyle)pi.GetValue(null, null);
        }
    }
}
