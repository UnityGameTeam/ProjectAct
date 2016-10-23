using System;
using System.Collections.Generic;
using UnityEngine;

public class TreeViewItem : IComparable<TreeViewItem>
{
    private int m_ID;
    private TreeViewItem m_Parent;
    private List<TreeViewItem> m_Children;
    private int m_Depth;
    private string m_DisplayName;
    private Texture2D m_Icon;
    private object m_UserData;

    public virtual int id
    {
        get
        {
            return m_ID;
        }
        set
        {
            m_ID = value;
        }
    }

    public virtual string displayName
    {
        get
        {
            return m_DisplayName;
        }
        set
        {
            m_DisplayName = value;
        }
    }

    public virtual int depth
    {
        get
        {
            return m_Depth;
        }
        set
        {
            m_Depth = value;
        }
    }

    public virtual bool hasChildren
    {
        get
        {
            if (m_Children != null)
                return m_Children.Count > 0;
            return false;
        }
    }

    public virtual List<TreeViewItem> children
    {
        get
        {
            return m_Children;
        }
        set
        {
            m_Children = value;
        }
    }

    public virtual TreeViewItem parent
    {
        get
        {
            return m_Parent;
        }
        set
        {
            m_Parent = value;
        }
    }

    public virtual Texture2D icon
    {
        get
        {
            return m_Icon;
        }
        set
        {
            m_Icon = value;
        }
    }

    public virtual object userData
    {
        get
        {
            return m_UserData;
        }
        set
        {
            m_UserData = value;
        }
    }

    public TreeViewItem(int id, int depth, TreeViewItem parent, string displayName)
    {
        m_Depth = depth;
        m_Parent = parent;
        m_ID = id;
        m_DisplayName = displayName;
    }

    public void AddChild(TreeViewItem child)
    {
        if (m_Children == null)
            m_Children = new List<TreeViewItem>();
        m_Children.Add(child);
        if (child == null)
            return;
        child.parent = this;
    }

    public virtual int CompareTo(TreeViewItem other)
    {
        return displayName.CompareTo(other.displayName);
    }

    public override string ToString()
    {
        return string.Format("Item: '{0}' ({1}), has {2} children, depth {3}, parent id {4}", displayName,id, !hasChildren ? 0 : children.Count, depth, parent == null ? -1 : parent.id);
    }
}
