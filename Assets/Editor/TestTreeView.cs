using UnityEditor;
using UnityEngine;

public class TestTreeView : EditorWindow
{

    private TreeViewState m_FolderTreeState;
    private TreeView m_FolderTree;

    [MenuItem("Tools/wwwwww")]
    private static void Test()
    {
        GetWindow<TestTreeView>();
    }


    public void OnEnable()
    {
        if (this.m_FolderTreeState == null)
            this.m_FolderTreeState = new TreeViewState();
      //  this.m_FolderTreeState.renameOverlay.isRenamingFilename = true;

        this.m_FolderTree = new TreeView((EditorWindow) this, this.m_FolderTreeState);
        this.m_FolderTree.deselectOnUnhandledMouseDown = false;
        //   this.m_FolderTree.selectionChangedCallback += new System.Action<int[]>(this.FolderTreeSelectionCallback);
        //  this.m_FolderTree.contextClickItemCallback += new System.Action<int>(this.FolderTreeViewContextClick);
        //  this.m_FolderTree.onGUIRowCallback += new System.Action<int, Rect>(this.OnGUIAssetCallback);
        //   this.m_FolderTree.dragEndedCallback += new System.Action<int[], bool>(this.FolderTreeDragEnded);
        this.m_FolderTree.Init(new Rect(0,0,400,1000), 
             new FolderTreeViewDataSource(this.m_FolderTree), 
             new ProjectBrowserColumnOneTreeViewGUI(this.m_FolderTree),
             new ProjectBrowserColumnOneTreeViewDragging(this.m_FolderTree));
        this.m_FolderTree.ReloadData();
    }

    private void OnGUI()
    {
        var m_TreeViewKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
        m_FolderTree.OnEvent();
        this.m_FolderTree.OnGUI(new Rect(0, 0, 400, 1000), m_TreeViewKeyboardControlID);
    }
}
