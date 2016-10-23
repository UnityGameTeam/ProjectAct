using UnityEngine;

public class PrefKey : IPrefType
{
    private bool m_Loaded;
    private string m_name;
    private Event m_event;
    private string m_Shortcut;
    private string m_DefaultShortcut;

    public string Name
    {
        get
        {
            Load();
            return m_name;
        }
    }

    public Event KeyboardEvent
    {
        get
        {
            Load();
            return m_event;
        }
        set
        {
            Load();
            m_event = value;
        }
    }

    public bool activated
    {
        get
        {
            Load();
            if (Event.current.Equals(this))
                return !GUIUtilityWrap.textFieldInput;
            return false;
        }
    }

    public PrefKey()
    {
        m_Loaded = true;
    }

    public PrefKey(string name, string shortcut)
    {
        m_name = name;
        m_Shortcut = shortcut;
        m_DefaultShortcut = shortcut;
        Settings.Add(this);
        m_Loaded = false;
    }

    public static implicit operator Event(PrefKey pkey)
    {
        pkey.Load();
        return pkey.m_event;
    }

    public void Load()
    {
        if (m_Loaded)
            return;

        m_Loaded = true;
        m_event = Event.KeyboardEvent(m_Shortcut);
        PrefKey prefKey = Settings.Get(m_name, this);
        m_name = prefKey.Name;
        m_event = prefKey.KeyboardEvent;
    }

    public string ToUniqueString()
    {
        Load();
        return string.Concat(m_name, ";", !m_event.alt ? string.Empty : "&", !m_event.command ? string.Empty : "%", !m_event.shift ? string.Empty : "#", !m_event.control ? string.Empty : "^", m_event.keyCode);
    }

    public void FromUniqueString(string s)
    {
        Load();
        int length = s.IndexOf(";");
        if (length < 0)
        {
            Debug.LogError("Malformed string in Keyboard preferences");
        }
        else
        {
            m_name = s.Substring(0, length);
            m_event = Event.KeyboardEvent(s.Substring(length + 1));
        }
    }

    internal void ResetToDefault()
    {
        Load();
        m_event = Event.KeyboardEvent(m_DefaultShortcut);
    }
}
