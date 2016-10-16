using System.Collections.Generic;

namespace Pattern.Node
{
    public interface INode<T>
    {
        string Name { get; }
        int ChildCount { get; }
        T ParentNode { get; }

        IEnumerator<T> GetEnumerator();
        void DetachChildren();
        T Find(string name);
        bool IsChildOf(T parent);
    }
}