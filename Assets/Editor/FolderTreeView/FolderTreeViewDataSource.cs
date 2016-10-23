using System.Collections.Generic;
using UnityEditor;

public class FolderTreeViewDataSource : TreeViewDataSource
{
    private static string kFolderTreeViewString = "FolderTreeView";

    public FolderTreeViewDataSource(TreeView treeView) : base(treeView)
    {
        showRootItem      = false;         //不显示根对象
        rootIsCollapsable = false;         //根对象不折叠
    }

    /// <summary>
    /// 设置展开
    /// </summary>
    /// <param name="id">item的id</param>
    /// <param name="expand">是否展开</param>
    /// <returns></returns>
    public override bool SetExpanded(int id, bool expand)
    {
        if (!base.SetExpanded(id, expand))
            return false;

        if (m_RootItem.hasChildren)
        {
            foreach (var item in m_RootItem.children)
            {
                if (item.id == id)
                    EditorPrefs.SetBool(kFolderTreeViewString + item.displayName, expand);
            }
        }
        return true;
    }

    public override bool IsExpandable(TreeViewItem item)
    {
        if (!item.hasChildren)
            return false;

        if (item == m_RootItem)
            return rootIsCollapsable;

        return true;
    }
 
    public bool IsVisibleRootNode(TreeViewItem item)
    {
        if (item.parent != null)
            return item.parent.parent == null;
        return false;
    }

   /* public override bool IsRenamingItemAllowed(TreeViewItem item)
    {
        if (IsVisibleRootNode(item))
            return false;
        return base.IsRenamingItemAllowed(item);
    }
*/
    public override void FetchData()
    {
        m_RootItem = new TreeViewItem(int.MaxValue, 0, null, "Invisible Root Item");
        SetExpanded(m_RootItem, true);

        List<TreeViewItem> list = new List<TreeViewItem>();
        int folderInstanceId = 1234;
        int depth = 0;
        string displayName = "Assets";
        TreeViewItem parent = new TreeViewItem(folderInstanceId, depth, m_RootItem, displayName);
       // {
          //  icon = EditorGUIUtility.FindTexture(EditorResourcesUtilityWrap.folderIconName)
       // };
 
        list.Add(parent);
        List<TreeViewItem> list2 = new List<TreeViewItem>();
        for (int i = 0; i<10;++i)
        {
            TreeViewItem item = new TreeViewItem(i, 1, null, "displayName" + i);
           // {
         //       icon = EditorGUIUtility.FindTexture(EditorResourcesUtilityWrap.folderIconName)
         //   };
            item.parent = parent;
            list2.Add(item);

            if (i == 9)
            {
                for (int j = 0; j < 10; ++j)
                {
                    TreeViewItem item1 = new TreeViewItem(j + 10, 2, null, "displayName" + i);
             //       {
            //            icon = EditorGUIUtility.FindTexture(EditorResourcesUtilityWrap.folderIconName)
             //       };
                    item1.parent = item;
                    list.Add(item1);
                }
            }
        }
        parent.children = list2;
        m_RootItem.children = list;

        using (List<TreeViewItem>.Enumerator enumerator = m_RootItem.children.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                TreeViewItem current = enumerator.Current;
                bool @bool = EditorPrefs.GetBool(kFolderTreeViewString + current.displayName, true);
                SetExpanded(current, @bool);
            }
        }
        m_NeedRefreshVisibleFolders = true;
    }
}