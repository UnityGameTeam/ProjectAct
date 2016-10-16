using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Pattern.Node
{
    /// <summary>
    /// InvokeNode是可调用节点的实现，可以用于注册Action，一个InvokeNode调用Invoke方法,除了会调用自身的Action,也会
    /// 调用子节点的Invoke,总共有以下8种不同的调用顺序，假如当前节点是a,a构成了如下的树形结构
    /// 
    ///                    a
    ///                   /\ 
    ///                  b1 b2
    ///                  /\  \  
    ///                c1 c2 c3
    ///                /  / \  \
    ///               d1 d2 d3 d4 
    /// 
    /// 1、LayerTopToDown
    ///    使用广度优先父节点处理模式，如调用a,则调用顺序 a -> b1 b2 -> c1 c2 c3 -> d1 d2 d3 d4
    ///    从上往下逐层调用, 使用这种方式需要额外的内存分配  
    /// 
    /// 2、LayerDownToTop
    ///    使用广度优先子节点处理模式，与1相反的层处理顺序d4 d3 d2 d1 -> c3 c2 c1 -> b2 b1 -> a,使用这种方式需要额外的内存分配
    /// 
    /// 3、LayerReverseTopToDown
    ///    与1相同的处理方式，唯一不同的是，每一层的处理顺序是相反的，比如 a -> b2 b1 -> c3 c2 c1 -> d4 d3 d2 d1，比1需要更多一点的内存分配
    /// 
    /// 4、LayerReverseDownToTop
    ///    与2相同的处理方式，唯一不同的是，每一层的处理顺序是相反的，比如 d1 d2 d3 d4 -> c1 c2 c3 ->  b1 b2 -> a，比2需要更多一点的内存分配
    /// 
    /// 5、TreeTopToDown
    ///    使用深度优先父节点模式，比如调用a,会先选一个子树处理，顺序： a -> b1 -> c1 -> d1 -> c2 -> d2 -> d3 -> b2 -> c3 -> d4 从左边的子树到右边的子树的处理顺序
    /// 
    /// 6、TreeDownToTop
    ///    使用深度优先子节点模式，与5中的模式相反，会先处理子节点 顺序： d1 -> c1 -> d2 -> d3 -> c2 -> b1 -> d4 -> c3 -> b2 -> a 从左边的子树到右边的子树的处理顺序
    ///   
    /// 7、TreeReverseTopToDown
    ///    使用深度优先父节点模式，比如调用a,会先选一个子树处理，顺序： a -> b2 -> c3 -> d4 -> b1 -> c2 -> d3 -> d2 -> c1 -> d1 从右边的子树到左边的子树的处理顺序
    ///    使用这种方式需要额外的内存分配  
    /// 
    /// 8、TreeReverseDownToTop
    ///    使用深度优先子节点模式，与7中的模式相反，会先处理子节点 顺序： d4 -> c3 -> b2 -> d3 -> d2 -> c2 -> d1 -> c1 -> b1 -> a 从右边的子树到左边的子树的处理顺序
    ///    使用这种方式需要额外的内存分配  
    /// 
    /// 默认的模式是6 TreeDownToTop
    /// 
    /// 以上的层从左到右，子树从左到右的顺序关系，只要在InvokeNodeList中有效，InvokeNodeList使用List保存子节点，可以保证调用顺序
    /// 在InvokeNodeSet中，实际只有4种模式，比如LayerReverseTopToDown的效果等价于LayerTopToDown的效果，因为其内部使用HashSet保存子
    /// 节点，层和子树之间的顺序是不被保证的
    /// 
    /// 节点的调用支持组合，比如
    /// 
    ///                    a
    ///                   /\ 
    ///                  b1 b2
    ///                 /  
    ///                c1 
    ///                
    /// 如果a,b1,b2都是LayerTopToDown模式，则调用顺序应该是 a b1 b2 c1
    /// 如果a,b2是LayerTopToDown模式，b1是TreeDownToTop模式，调用顺序应该是 a c1 b1 c2     
    /// </summary>
    public abstract class InvokeNode : INode<InvokeNode>
    {
        public enum InvokeMode
        {
            LayerTopToDown,
            LayerDownToTop,
            LayerReverseTopToDown,
            LayerReverseDownToTop,
            TreeTopToDown,
            TreeDownToTop,
            TreeReverseTopToDown,
            TreeReverseDownToTop
        }

        protected string     m_Name;
        protected InvokeNode mParentNode;
        protected Action     mInvokeEvent;
        protected InvokeMode mInvokeMode = InvokeMode.TreeDownToTop;
        protected bool       mIsEnable = true;

        public virtual int ChildCount { get; protected set; }

        public bool EnableSelf
        {
            get { return mIsEnable; }
            set { mIsEnable = value; }
        }

        public bool EnableInHierachy
        {
            get
            {
                var parent = this;
                while (parent != null)
                {
                    if (!parent.EnableSelf)
                    {
                        return false;
                    }
                    parent = parent.ParentNode;
                }
                return true;
            }
        }

        public virtual Action InvokeEvent
        {
            get
            {
                if (mInvokeEvent == null)
                {
                    return () => { };
                }
                return mInvokeEvent;
            }
        }

        public virtual InvokeMode NodeInvokeMode
        {
            get { return mInvokeMode; }
            set { mInvokeMode = value; }
        }

        public virtual string Name
        {
            get { return m_Name; }
            set
            {
                m_Name = value;
                if (value == null)
                {
                    m_Name = string.Empty;
                }
            }
        }

        public virtual InvokeNode ParentNode
        {
            get { return mParentNode; }
            set
            {
                if (mParentNode != value && value != this)
                {
                    //如果value的父节点有自己存在，不能修改父节点
                    if (value != null)
                    {
                        var tempParent = value.mParentNode;
                        while (tempParent != null)
                        {
                            if (tempParent == this)
                            {
                                return;
                            }
                            tempParent = tempParent.mParentNode;
                        }
                    }

                    if (mParentNode != null)
                        mParentNode.DetachChild(this);

                    mParentNode = value;

                    if (mParentNode != null)
                        mParentNode.AttachChild(this);
                }
            }
        }

        public virtual void AddInvokeEvent(Action action)
        {
            mInvokeEvent += action;
        }

        public virtual void RemoveInvokeEvent(Action action)
        {
            mInvokeEvent -= action;
        }

        public virtual void ClearInvokeEvent()
        {
            mInvokeEvent = null;
        }

        protected virtual void DetachChild(InvokeNode childNode)
        {

        }

        protected virtual void AttachChild(InvokeNode childNode)
        {

        }

        public abstract IEnumerator<InvokeNode> GetEnumerator();
        public abstract void                    DetachChildren();
        public abstract InvokeNode              Find(string name);
        public abstract bool                    IsChildOf(InvokeNode parent);
        public abstract InvokeNode              FindNode(string[] names, int layerIndex);
        public abstract void                    Invoke();
    }
}