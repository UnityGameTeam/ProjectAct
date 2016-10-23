using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class ProjectBrowserColumnOneTreeViewGUI : AssetsTreeViewGUI
{
    private Texture2D k_FavoritesIcon = EditorGUIUtility.FindTexture("Favorite Icon");
    private Texture2D k_FavoriteFolderIcon = EditorGUIUtility.FindTexture("FolderFavorite Icon");
    private Texture2D k_FavoriteFilterIcon = EditorGUIUtility.FindTexture("Search Icon");
    private const float k_DistBetweenRootTypes = 15f;
    private bool m_IsCreatingSavedFilter;

    public ProjectBrowserColumnOneTreeViewGUI(TreeView treeView)
      : base(treeView)
    {
    }

    public override Vector2 GetTotalSize()
    {
        Vector2 totalSize = base.GetTotalSize();
        totalSize.y += 15f;
        return totalSize;
    }

    public override Rect GetRowRect(int row, float rowWidth)
    {
        List<TreeViewItem> rows = this.m_TreeView.data.GetRows();
        return new Rect(0.0f, this.GetTopPixelOfRow(row, rows), rowWidth, this.k_LineHeight);
    }

    private float GetTopPixelOfRow(int row, List<TreeViewItem> rows)
    {
        float num = (float)row * this.k_LineHeight;
        //if (ProjectBrowser.GetItemType(rows[row].id) == ProjectBrowser.ItemType.Asset)
        //    num += 15f;
        return num;
    }

    public override int GetNumRowsOnPageUpDown(TreeViewItem fromItem, bool pageUp, float heightOfTreeView)
    {
        return (int)Mathf.Floor(heightOfTreeView / this.k_LineHeight) - 1;
    }

    public override void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
    {
        float num1 = this.m_TreeView.state.scrollPos.y;
        float height = this.m_TreeView.GetTotalRect().height;
        firstRowVisible = (int)Mathf.Floor(num1 / this.k_LineHeight);
        lastRowVisible = firstRowVisible + (int)Mathf.Ceil(height / this.k_LineHeight);
        float num2 = 15f / this.k_LineHeight;
        firstRowVisible = firstRowVisible - (int)Mathf.Ceil(2f * num2);
        lastRowVisible = lastRowVisible + (int)Mathf.Ceil(2f * num2);
        firstRowVisible = Mathf.Max(firstRowVisible, 0);
        lastRowVisible = Mathf.Min(lastRowVisible, this.m_TreeView.data.rowCount - 1);
    }

    public override void OnRowGUI(Rect rowRect, TreeViewItem item, int row, bool selected, bool focused)
    {
        bool useBoldFont = this.IsVisibleRootNode(item);
        this.DoItemGUI(rowRect, row, item, selected, focused, useBoldFont);
    }

    private bool IsVisibleRootNode(TreeViewItem item)
    {
        return (this.m_TreeView.data as FolderTreeViewDataSource).IsVisibleRootNode(item);
    }

    protected override Texture GetIconForItem(TreeViewItem item)
    {
        if (item != null && (Object)item.icon != (Object)null)
            return (Texture)item.icon;
       // SearchFilterTreeItem searchFilterTreeItem = item as SearchFilterTreeItem;
      //  if (searchFilterTreeItem == null)
            return base.GetIconForItem(item);
     //   if (this.IsVisibleRootNode(item))
    //        return (Texture)this.k_FavoritesIcon;
    //    if (searchFilterTreeItem.isFolder)
   //         return (Texture)this.k_FavoriteFolderIcon;
        return (Texture)this.k_FavoriteFilterIcon;
    }

   /* public static float GetListAreaGridSize()
    {
        float num = -1f;
        if ((Object)ProjectBrowser.s_LastInteractedProjectBrowser != (Object)null)
            num = ProjectBrowser.s_LastInteractedProjectBrowser.listAreaGridSize;
        return num;
    }

    internal virtual void BeginCreateSavedFilter(SearchFilter filter)
    {
        string str = "New Saved Search";
        this.m_IsCreatingSavedFilter = true;
        int num = SavedSearchFilters.AddSavedFilter(str, filter, ProjectBrowserColumnOneTreeViewGUI.GetListAreaGridSize());
        this.m_TreeView.Frame(num, true, false);
        this.m_TreeView.state.renameOverlay.BeginRename(str, num, 0.0f);
    }
*/
  /*  protected override void RenameEnded()
    {
        int userData = this.GetRenameOverlay().userData;
        ProjectBrowser.ItemType itemType = ProjectBrowser.GetItemType(userData);
        if (this.m_IsCreatingSavedFilter)
        {
            this.m_IsCreatingSavedFilter = false;
            if (this.GetRenameOverlay().userAcceptedRename)
            {
                SavedSearchFilters.SetName(userData, this.GetRenameOverlay().name);
                TreeView treeView = this.m_TreeView;
                int[] selectedIDs = new int[1];
                int index = 0;
                int num1 = userData;
                selectedIDs[index] = num1;
                int num2 = 1;
                treeView.SetSelection(selectedIDs, num2 != 0);
            }
            else
                SavedSearchFilters.RemoveSavedFilter(userData);
        }
        else if (itemType == ProjectBrowser.ItemType.SavedFilter)
        {
            if (!this.GetRenameOverlay().userAcceptedRename)
                return;
            SavedSearchFilters.SetName(userData, this.GetRenameOverlay().name);
        }
        else
        {
            base.RenameEnded();
            if (!this.GetRenameOverlay().userAcceptedRename)
                return;
            this.m_TreeView.NotifyListenersThatSelectionChanged();
        }
    }*/

}
