using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class ProjectBrowserColumnOneTreeViewDragging : AssetsTreeViewDragging
{
    public ProjectBrowserColumnOneTreeViewDragging(TreeView treeView)
      : base(treeView)
    {
    }

    /*public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
    {
        //if (SavedSearchFilters.IsSavedFilter(draggedItem.id) && draggedItem.id == SavedSearchFilters.GetRootInstanceID())
        //    return;
        //ProjectWindowUtil.StartDrag(draggedItem.id, draggedItemIDs);


        DragAndDrop.PrepareStartDrag();
        string title = string.Empty;

            bool flag = ProjectWindowUtil.IsFolder(draggedInstanceID);
            DragAndDrop.objectReferences = ProjectWindowUtil.GetDragAndDropObjects(draggedInstanceID, selectedInstanceIDs);
            DragAndDrop.SetGenericData(ProjectWindowUtil.k_IsFolderGenericData, !flag ? (object)string.Empty : (object)"isFolder");
            string[] dragAndDropPaths = ProjectWindowUtil.GetDragAndDropPaths(draggedInstanceID, selectedInstanceIDs);
            if (dragAndDropPaths.Length > 0)
                DragAndDrop.paths = dragAndDropPaths;
            title = DragAndDrop.objectReferences.Length <= 1 ? ObjectNames.GetDragAndDropTitle(InternalEditorUtility.GetObjectFromInstanceID(draggedInstanceID)) : "<Multiple>";
 
        DragAndDrop.StartDrag(title);
    }

    public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, TreeViewDragging.DropPosition dropPos)
    {
        if (targetItem == null)
            return DragAndDropVisualMode.None;
 
        if (!(DragAndDrop.GetGenericData(ProjectWindowUtil.k_IsFolderGenericData) as string == "isFolder"))
            return DragAndDropVisualMode.None;

        if (perform)
        {
            Object[] objectReferences = DragAndDrop.objectReferences;
            if (objectReferences.Length > 0)
            {
                string assetPath = AssetDatabase.GetAssetPath(objectReferences[0].GetInstanceID());
                if (!string.IsNullOrEmpty(assetPath))
                {
                    string name = new DirectoryInfo(assetPath).Name;
                    SearchFilter filter = new SearchFilter();
                    SearchFilter searchFilter = filter;
                    string[] strArray = new string[1];
                    int index = 0;
                    string str = assetPath;
                    strArray[index] = str;
                    searchFilter.folders = strArray;
                    bool addAsChild = targetItem == parentItem;
                    float listAreaGridSize = ProjectBrowserColumnOneTreeViewGUI.GetListAreaGridSize();
                    Selection.activeInstanceID = SavedSearchFilters.AddSavedFilterAfterInstanceID(name, filter, listAreaGridSize, targetItem.id, addAsChild);
                }
                else
                    Debug.Log((object)("Could not get asset path from id " + (object)objectReferences[0].GetInstanceID()));
            }
        }
        return DragAndDropVisualMode.Copy;
    }*/
}