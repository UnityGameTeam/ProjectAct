using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;

public class AssetsTreeViewDragging : TreeViewDragging
{
    public AssetsTreeViewDragging(TreeView treeView) : base(treeView)
    {

    }

    public override void StartDrag(TreeViewItem draggedItem, List<int> draggedItemIDs)
    {
        DragAndDrop.PrepareStartDrag();
       // DragAndDrop.objectReferences = ProjectWindowUtil.GetDragAndDropObjects(draggedItem.id, draggedItemIDs);
       // DragAndDrop.paths = ProjectWindowUtil.GetDragAndDropPaths(draggedItem.id, draggedItemIDs);
        if (DragAndDrop.objectReferences.Length > 1)
            DragAndDrop.StartDrag("<Multiple>");
        else
            DragAndDrop.StartDrag(ObjectNames.GetDragAndDropTitle(InternalEditorUtility.GetObjectFromInstanceID(draggedItem.id)));
    }

    public override DragAndDropVisualMode DoDrag(TreeViewItem parentItem, TreeViewItem targetItem, bool perform, DropPosition dropPos)
    {
        HierarchyProperty property = new HierarchyProperty(HierarchyType.Assets);
        if (parentItem == null || !property.Find(parentItem.id, (int[])null))
            property = (HierarchyProperty)null;
        return InternalEditorUtility.ProjectWindowDrag(property, perform);
    }
}