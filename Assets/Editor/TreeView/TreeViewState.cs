using System.Collections.Generic;
using UnityEngine;

//CreateAssetUtility先注释看看后面有没有影响

public class TreeViewState
{
    private List<int> m_SelectedIDs = new List<int>();
    private List<int> m_ExpandedIDs = new List<int>();
    private RenameOverlay m_RenameOverlay = new RenameOverlay();

    //[SerializeField]
    //private CreateAssetUtility m_CreateAssetUtility = new CreateAssetUtility();

    public Vector2 scrollPos;
    [SerializeField]
    private int m_LastClickedID;
    [SerializeField]
    private string m_SearchString;
    [SerializeField]
    private float[] m_ColumnWidths;

    public List<int> selectedIDs
    {
        get
        {
            return m_SelectedIDs;
        }
        set
        {
            m_SelectedIDs = value;
        }
    }

    public int lastClickedID
    {
        get
        {
            return m_LastClickedID;
        }
        set
        {
            m_LastClickedID = value;
        }
    }

    public List<int> expandedIDs
    {
        get
        {
            return m_ExpandedIDs;
        }
        set
        {
            m_ExpandedIDs = value;
        }
    }

    public RenameOverlay renameOverlay
    {
        get
        {
            return m_RenameOverlay;
        }
        set
        {
            m_RenameOverlay = value;
        }
    }

    /*public CreateAssetUtility createAssetUtility
    {
        get
        {
            return m_CreateAssetUtility;
        }
        set
        {
            m_CreateAssetUtility = value;
        }
    }*/

    public float[] columnWidths
    {
        get
        {
            return m_ColumnWidths;
        }
        set
        {
            m_ColumnWidths = value;
        }
    }

    public string searchString
    {
        get
        {
            return m_SearchString;
        }
        set
        {
            m_SearchString = value;
        }
    }

    public void OnAwake()
    {
        m_RenameOverlay.Clear();
        //m_CreateAssetUtility = new CreateAssetUtility();
    }
}
 