using System;
using UnityEngine;
using System.Collections;
using System.Reflection;
using UnityEditor;

public class HandlesWrap
{
    public static Matrix4x4 s_InverseMatrix
    {
        get
        {
            Type type = typeof(Handles);
            var pi = type.GetField("s_InverseMatrix", BindingFlags.Static | BindingFlags.NonPublic);

            return (Matrix4x4)pi.GetValue(null);
        }
    }
}
