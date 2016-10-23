using System;
using UnityEngine;
using System.Reflection;
using UnityEditor;

public class EditorGUIWrap
{
    public static TextEditor s_RecycledEditor
    {
        get
        {
            Type type = typeof(EditorGUI);
            var fi = type.GetField("s_RecycledEditor", BindingFlags.NonPublic | BindingFlags.Static);
            return fi.GetValue(null) as TextEditor;
        }
    }

    public static string DoTextField(TextEditor textEditor, int id, Rect position, string text, GUIStyle style,
        string allowedletters, out bool changed, bool reset, bool multiline, bool passwordField)
    {
        changed = false;
        Type type = typeof(EditorGUI);
       
        var mf = type.GetMethod("DoTextField", BindingFlags.NonPublic | BindingFlags.Static,null,new Type[]
        {
            type.Assembly.GetType("UnityEditor.EditorGUI+RecycledTextEditor") ,
            typeof(int),typeof(Rect),typeof(string),typeof(GUIStyle),typeof(string),typeof(bool).MakeByRefType(),typeof(bool),typeof(bool),typeof(bool)
        }, null);

        var param = new object[] { textEditor, id, position, text, style, allowedletters, changed, reset, multiline, passwordField };
        var result = mf.Invoke(null, param) as string;
        changed = true;
        return result;
    }
}
