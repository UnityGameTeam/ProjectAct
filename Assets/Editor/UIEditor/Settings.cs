using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;

public class Settings
{
    private static List<IPrefType> m_AddedPrefs = new List<IPrefType>();
    private static SortedList<string, object> m_Prefs = new SortedList<string, object>();

    internal static void Add(IPrefType value)
    {
        m_AddedPrefs.Add(value);
    }

    internal static T Get<T>(string name, T defaultValue) where T : IPrefType, new()
    {
        Load();
        if (defaultValue == null)
            throw new ArgumentException("default can not be null", "defaultValue");

        if (m_Prefs.ContainsKey(name))
            return (T)m_Prefs[name];

        string @string = EditorPrefs.GetString(name, string.Empty);
        if (@string == string.Empty)
        {
            Set(name, defaultValue);
            return defaultValue;
        }

        defaultValue.FromUniqueString(@string);
        Set(name, defaultValue);
        return defaultValue;
    }

    internal static void Set<T>(string name, T value) where T : IPrefType
    {
        Load();
        EditorPrefs.SetString(name, value.ToUniqueString());
        m_Prefs[name] = value;
    }

    private static void Load()
    {
        if (!Enumerable.Any(m_AddedPrefs))
            return;

        List<IPrefType> list = new List<IPrefType>(m_AddedPrefs);
        m_AddedPrefs.Clear();

        using (List<IPrefType>.Enumerator enumerator = list.GetEnumerator())
        {
            while (enumerator.MoveNext())
                enumerator.Current.Load();
        }
    }
}
