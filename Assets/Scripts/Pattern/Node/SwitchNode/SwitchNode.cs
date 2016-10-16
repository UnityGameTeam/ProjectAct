using System;
using System.Collections.Generic;

namespace Pattern.Node
{
    /// <summary>
    /// SwitchNode是开关节点的抽象，支持串联和并联模式，一个开关节点
    /// 关闭打开自身，也会同时关闭和打开当前状态不同的子节点，同时也
    /// 可能根据自身的对象状态，导致父对象打开关闭，类似电路的开关
    /// 
    /// 关于子节点自身的八种关闭打开方式和InvokeNode中的相同，参考InvokeNode
    /// 的注释   
    /// </summary>
    public abstract class SwitchNode : INode<SwitchNode>, ISwitchable
    {
        public enum NodeMode
        {
            SeriesConnection,          //串联模式
            ParalleConnection          //并联模式
        }

        public enum TurnMode
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

        protected string       m_Name;
        protected SwitchNode   mParentNode;
        protected bool         mIsOpen;       //当前开关节点是否打开
        protected Action<bool> mSwitchEvent;
        protected TurnMode     mInvokeMode = TurnMode.TreeDownToTop;

        public bool CheckZeroChildState { get; set; }      //当前状态是打开的时候，当子对象为0时，是否检查当前节点的打开状态以改变当前节点的状态

        public virtual int ChildCount { get; protected set; }
    
        public virtual int OpenChildCount { get; protected set; }

        public virtual Action<bool> SwitchEvent
        {
            get
            {
                if (mSwitchEvent == null)
                {
                    return (open) => { };
                }
                return mSwitchEvent;
            }
        }

        public virtual TurnMode SwitchTurnMode
        {
            get { return mInvokeMode; }
            set { mInvokeMode = value; }
        }

        public virtual NodeMode SwitchNodeMode
        {
            get; protected set;
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

        public virtual SwitchNode ParentNode
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

        public virtual bool IsOpen
        {
            get { return mIsOpen; }
            protected set
            {
                if (mIsOpen != value)
                {
                    mIsOpen = value;
                    SwitchEvent.Invoke(mIsOpen);
                    if (mParentNode != null)
                    {
                        ParentNode.ModifyChildrenOpenCount(mIsOpen);
                    }
                }
            }
        }

        public virtual void AddSwitchEvent(Action<bool> action)
        {
            mSwitchEvent += action;
        }

        public virtual void RemoveSwitchEvent(Action<bool> action)
        {
            mSwitchEvent -= action;
        }

        public virtual void ClearSwitchEvent()
        {
            mSwitchEvent = null;
        }

        public virtual bool SwitchOn()
        {
            if (!IsOpen)
            {
                Switch(true);

                if (ParentNode != null)
                {
                    ParentNode.ModifyChildrenOpenCount(true);
                }
                return true;
            }
            return false;
        }

        public virtual bool SwitchOff()
        {
            if (IsOpen)
            {
                Switch(false);

                if (ParentNode != null)
                {
                    ParentNode.ModifyChildrenOpenCount(false);
                }
                return true;
            }
            return false;
        }

        public virtual bool Toggle(bool turnOn)
        {
            if (turnOn)
                return SwitchOn();
            return SwitchOff();
        }

        protected virtual void ModifyChildrenOpenCount(bool isOpen)
        {
            if (isOpen)
            {
                ++OpenChildCount;
            }
            else
            {
                --OpenChildCount;
                if (OpenChildCount < 0)
                {
                    OpenChildCount = 0;
                }
            }
            CheckSwitchState();
        }

        protected virtual void CheckSwitchState()
        {

        }

        protected virtual void Switch(bool turnOn)
        {

        }

        protected virtual void DetachChild(SwitchNode childNode)
        {

        }

        protected virtual void AttachChild(SwitchNode childNode)
        {

        }

        protected virtual void ResetNodeState(bool turnOn)
        {

        }

        protected static void ChangeSwitchNodeState(SwitchNode node, bool turnOn)
        {
            node.ResetNodeState(turnOn);
        }

        protected static void InvokeSwitch(SwitchNode node, bool turnOn)
        {
            node.Switch(turnOn);
        }

        public abstract IEnumerator<SwitchNode> GetEnumerator();
        public abstract void DetachChildren();
        public abstract SwitchNode Find(string name);
        public abstract bool IsChildOf(SwitchNode parent);
        public abstract SwitchNode FindNode(string[] names, int layerIndex);
    }
}