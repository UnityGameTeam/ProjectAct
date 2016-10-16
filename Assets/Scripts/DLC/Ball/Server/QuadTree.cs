using System;
using System.Collections.Generic;

public class QuadTreeSetting
{
    public int MaxLevel;
    public int MaxObjectNum;
}

public interface IQuadTreeItem<T> where T : IQuadTreeItem<T>
{
    Rectangle Rect { get; set; }
    QuadTree<T> QuadTreeNode { get; set; }
}

public class QuadTree<T> where T : IQuadTreeItem<T>
{
    private int       level;
    private Rectangle range;
    private QuadTree<T>  parent;
    private QuadTreeSetting setting;

    private List<QuadTree<T>> branches;
    private List<T> objects;  
     
    public QuadTree(QuadTree<T> parent, Rectangle range, QuadTreeSetting setting)
    {
        this.parent = parent;
        this.range = range;
        this.setting = setting;
        this.level = parent == null ? 1 : parent.level + 1;
        branches = new List<QuadTree<T>>(4);
        objects = new List<T>();
    }

    public void Add(T item, bool split = true)
    {
        if (item == null)
            return;

        if (branches.Count > 0)
        {
            var index = GetIndex(item.Rect);
            if (index != -1)
            {
                branches[index].Add(item, split);
                return;
            }
        }

        objects.Add(item);

        if (split && branches.Count == 0 && objects.Count > setting.MaxObjectNum && level < setting.MaxLevel)
        {
            Split();

            var removeCount = 0;
            for (var i = objects.Count - 1; i >= 0; --i)
            {
                var index = GetIndex(objects[i].Rect);
                if (index != -1)
                {
                    ++removeCount;
                    branches[index].Add(objects[i]);
                }
                else
                {
                    objects.RemoveRange(i + 1, removeCount);
                    removeCount = 0;
                }
            }

            if (removeCount > 0)
            {
                objects.RemoveRange(0, removeCount);
            }
        }
    }

    /// <summary>
    /// 从四叉树中移除Item,实际是调用Item上绑定的四叉树节点来删除
    /// </summary>
    public void Remove(T item)
    {
        if (item == null)
            return;

        if (item.QuadTreeNode == null)
            return;

        var index = item.QuadTreeNode.objects.IndexOf(item);
        if (index != -1)
            item.QuadTreeNode.objects.RemoveAt(index);

        item.QuadTreeNode = null;
    }

    public int GetNodeCount()
    {
        var count = objects.Count;
        if (branches.Count > 0)
        {
            for (int i = 0; i < 4; ++i)
            {
                count += branches[i].GetNodeCount();
            }
        }
        return count;
    }

    public void Update(T item)
    {
        if (item == null || item.QuadTreeNode == null)
            return;

        if (!item.Rect.IsInner(item.QuadTreeNode.range))
        {
            if (item.QuadTreeNode.parent != null)
            {
                if (item.Rect.IsInner(item.QuadTreeNode.parent.range))
                {
                    Remove(item);
                    item.QuadTreeNode.parent.Add(item, false);
                }
                return;
            }

            Remove(item);
            Add(item, false);
        }
        else 
        {
            if (item.QuadTreeNode.branches.Count > 0)
            {
                var index = item.QuadTreeNode.GetIndex(item.Rect);
                if (index != -1)
                {
                    Remove(item);
                    item.QuadTreeNode.branches[index].Add(item, false);
                }
            }
        }
    }

    public void Clear()
    {
        objects.Clear();

        if (branches.Count > 0)
        {
            for (int i = 0; i < 4; ++i)
            {
                branches[i].Clear();
            }
        }

        branches.Clear();
    }

    protected void Split()
    {
        var w2 = this.range.halfWidth*0.5f;
        var h2 = this.range.halfHeight*0.5f;
        var x = this.range.x;
        var y = this.range.y;

        branches.Add(new QuadTree<T>(this, new Rectangle(x + w2, y - h2, w2, h2),setting));
        branches.Add(new QuadTree<T>(this, new Rectangle(x - w2, y - h2, w2, h2), setting));
        branches.Add(new QuadTree<T>(this, new Rectangle(x - w2, y + h2, w2, h2), setting));
        branches.Add(new QuadTree<T>(this, new Rectangle(x + w2, y + h2, w2, h2), setting));
    }

    public int GetIndex(Rectangle rect)
    {

        var onTop = rect.y - rect.halfHeight >= range.y;
        var onBottom = rect.y + rect.halfHeight <= range.y;
        var onLeft = rect.x + rect.halfWidth <= range.x;
        var onRight = rect.x - rect.halfWidth >= range.x;

        if (onTop)
        {
            if (onRight)
            {
                return 0;
            }
            if (onLeft)
            {
                return 1;
            }
        }
        else if (onBottom)
        {
            if (onLeft)
            {
                return 2;
            }
            if (onRight)
            {
                return 3;
            }
        }

        //如果物体跨越多个象限，则放回-1
        return -1;
    }

    public void Query(List<T> results,Rectangle rect, Predicate<T> predicate)
    {
        if (branches.Count > 0)
        {
            var index = this.GetIndex(rect);
            if (index != -1)
            {
                branches[index].Query(results,rect,predicate);
            }
            else
            {
                // 切割矩形
                var childRects = rect.Carve(range);
                for (var i = childRects.Count - 1; i >= 0; --i)
                {
                    index = this.GetIndex(childRects[i]);
                    branches[index].Query(results, childRects[i], predicate);
                }
            }
        }

        if (predicate != null)
        {
            for (int i = 0, count = objects.Count; i < count; ++i)
            {
                if (predicate(objects[i]))
                {
                    results.Add(results[i]);
                }
            }
        }
        else
        {
            results.AddRange(objects);
        }
    }
}
