using System;
using System.Collections.Generic;

namespace Pattern.Node
{
    /// <summary>
    /// SwitchNode的具体实现，使用List保存子节点
    /// </summary>   
    public class SwitchNodeList : SwitchNode
    {
        private List<SwitchNode> m_ChildNodes;

        public SwitchNode this[int index]
        {
            get { return m_ChildNodes[index]; }
        }

        public override int ChildCount
        {
            get { return m_ChildNodes.Count; }
        }

        public SwitchNodeList(Action<bool> toggleAction, NodeMode mode = NodeMode.ParalleConnection)
        {
            if (toggleAction != null)
                AddSwitchEvent(toggleAction);

            m_Name = string.Empty;
            SwitchNodeMode = mode;
            m_ChildNodes   = new List<SwitchNode>(0);
            SwitchTurnMode = TurnMode.TreeDownToTop;
        }

        protected override void DetachChild(SwitchNode childNode)
        {
            var index = m_ChildNodes.LastIndexOf(childNode);
            if (index > -1)
            {
                m_ChildNodes.RemoveAt(index);
            }

            if (childNode.IsOpen)
            {
                --OpenChildCount;
            }
            CheckSwitchState();
        }

        protected override void AttachChild(SwitchNode childNode)
        {
            m_ChildNodes.Add(childNode);

            if (childNode.IsOpen)
            {
                ++OpenChildCount;
            }
            CheckSwitchState();
        }

        public override IEnumerator<SwitchNode> GetEnumerator()
        {
            return m_ChildNodes.GetEnumerator();
        }

        public override void DetachChildren()
        {
            for (var i = ChildCount - 1; i >= 0; --i)
            {
                m_ChildNodes[i].ParentNode = null;
            }
        }

        public override SwitchNode Find(string name)
        {
            if (name == null)
            {
                return null;
            }

            string[] names = name.Split('/');
            return FindNode(names, 0);
        }

        public override SwitchNode FindNode(string[] names, int layerIndex)
        {
            for (var i = 0; i < m_ChildNodes.Count; ++i)
            {
                if (m_ChildNodes[i].Name != names[layerIndex])
                    continue;

                if (layerIndex == names.Length - 1)
                {
                    return m_ChildNodes[i];
                }

                var targetNode = m_ChildNodes[i].FindNode(names, layerIndex + 1);
                if (targetNode != null)
                {
                    return targetNode;
                }
            }
            return null;
        }

        public SwitchNode GetChild(int index)
        {
            if (index < 0 || index >= m_ChildNodes.Count)
            {
                return null;
            }
            return m_ChildNodes[index];
        }

        public int GetSiblingIndex()
        {
            if (mParentNode is SwitchNodeList)
            {
                var invokeNodeList = mParentNode as SwitchNodeList;
                return invokeNodeList.m_ChildNodes.IndexOf(this);
            }
            return -1;
        }

        public override bool IsChildOf(SwitchNode parent)
        {
            SwitchNode tempNode = this;
            while (tempNode != null)
            {
                if (tempNode == parent)
                {
                    return true;
                }
                tempNode = tempNode.ParentNode;
            }
            return false;
        }

        public bool SetAsFirstSibling()
        {
            if (mParentNode is SwitchNodeList)
            {
                var parentNode = mParentNode as SwitchNodeList;
                int index = GetSiblingIndex();
                var targetNode = parentNode.m_ChildNodes[index];
                for (var i = index - 1; i >= 0; --i)
                {
                    parentNode.m_ChildNodes[i + 1] = parentNode.m_ChildNodes[i];
                }
                parentNode.m_ChildNodes[0] = targetNode;
                return true;
            }
            return false;
        }

        public bool SetAsLastSibling()
        {
            var parentNode = mParentNode as SwitchNodeList;
            if (parentNode != null)
            {
                int index = GetSiblingIndex();
                var targetNode = parentNode.m_ChildNodes[index];
                parentNode.m_ChildNodes.RemoveAt(index);
                parentNode.m_ChildNodes.Add(targetNode);
                return true;
            }
            return false;
        }

        public bool SetSiblingIndex(int index)
        {
            var parentNode = mParentNode as SwitchNodeList;
            if (parentNode != null)
            {
                if (index < 0 || index >= parentNode.m_ChildNodes.Count)
                {
                    return false;
                }

                int srcIndex = GetSiblingIndex();
                if (index < srcIndex)
                {
                    var targetNode = parentNode.m_ChildNodes[srcIndex];
                    for (var i = srcIndex - 1; i >= index; --i)
                    {
                        parentNode.m_ChildNodes[i + 1] = parentNode.m_ChildNodes[i];
                    }
                    parentNode.m_ChildNodes[index] = targetNode;
                }
                else if (index > srcIndex)
                {
                    var targetNode = parentNode.m_ChildNodes[srcIndex];
                    for (var i = srcIndex; i < index; ++i)
                    {
                        parentNode.m_ChildNodes[i] = parentNode.m_ChildNodes[i + 1];
                    }
                    parentNode.m_ChildNodes[index] = targetNode;
                }
                return true;
            }
            return false;
        }

        protected override void CheckSwitchState()
        {
            if (SwitchNodeMode == NodeMode.SeriesConnection)
            {
                if (m_ChildNodes.Count > 0 && m_ChildNodes.Count == OpenChildCount)
                {
                    IsOpen = true;
                    return;
                }
            }
            else if (SwitchNodeMode == NodeMode.ParalleConnection)
            {
                if (OpenChildCount > 0)
                {
                    IsOpen = true;
                    return;
                }
            }

            if (m_ChildNodes.Count > 0)
            {
                IsOpen = false;
                return;
            }

            if (CheckZeroChildState)
            {
                IsOpen = false;
            }
        }

        protected override void ResetNodeState(bool turnOn)
        {
            if (turnOn)
            {              
                OpenChildCount = m_ChildNodes.Count;
            }
            else
            {
                OpenChildCount = 0;
            }
            mIsOpen = turnOn;
            SwitchEvent.Invoke(IsOpen);
        }

        protected override void Switch(bool turnOn)
        {
            if (m_ChildNodes.Count == 0)
            {
                mIsOpen = turnOn;
                SwitchEvent.Invoke(turnOn);
                return;
            }

            switch (SwitchTurnMode)
            {
                case TurnMode.TreeDownToTop:
                case TurnMode.TreeTopToDown:
                    InvokeTreeMode(turnOn);
                    break;

                case TurnMode.TreeReverseTopToDown:
                case TurnMode.TreeReverseDownToTop:
                    InvokeTreeReverseMode(turnOn);
                    break;

                case TurnMode.LayerTopToDown:
                    InvokeLayerTopToDown(turnOn);
                    break;

                case TurnMode.LayerDownToTop:
                    InvokeLayerDownToTop(turnOn);
                    break;

                case TurnMode.LayerReverseTopToDown:
                    InvokeLayerReverseTopToDown(turnOn);
                    break;

                case TurnMode.LayerReverseDownToTop:
                    InvokeLayerReverseDownToTop(turnOn);
                    break;
            }
        }

        protected virtual void InvokeTreeMode(bool turnOn)
        {
            if (SwitchTurnMode == TurnMode.TreeTopToDown)
            {
                SwitchNodeList.ChangeSwitchNodeState(this, turnOn);
            }

            for (var i = 0; i < m_ChildNodes.Count; i++)
            {
                if(m_ChildNodes[i].IsOpen != turnOn)
                    SwitchNodeList.InvokeSwitch(m_ChildNodes[i],turnOn);
            }

            if (SwitchTurnMode == TurnMode.TreeDownToTop)
            {
                SwitchNodeList.ChangeSwitchNodeState(this, turnOn);
            }
        }

        protected virtual void InvokeTreeReverseMode(bool turnOn)
        {
            if (SwitchTurnMode == TurnMode.TreeReverseTopToDown)
            {
                SwitchNodeList.ChangeSwitchNodeState(this, turnOn);
            }

            for (var i = m_ChildNodes.Count - 1; i >= 0; --i)
            {
                if (m_ChildNodes[i].IsOpen != turnOn)
                    SwitchNodeList.InvokeSwitch(m_ChildNodes[i], turnOn);
            }

            if (SwitchTurnMode == TurnMode.TreeReverseDownToTop)
            {
                SwitchNodeList.ChangeSwitchNodeState(this, turnOn);
            }
        }

        protected virtual void InvokeLayerTopToDown(bool turnOn)
        {
            Queue<SwitchNode> switchNodes = new Queue<SwitchNode>(m_ChildNodes.Count + 1);
            switchNodes.Enqueue(this);

            while (switchNodes.Count > 0)
            {
                var switchNode = switchNodes.Dequeue();
                var enumerator = switchNode.GetEnumerator();

                if (switchNode.SwitchTurnMode == TurnMode.LayerTopToDown)
                {
                    while (enumerator.MoveNext())
                    {
                        if(enumerator.Current.IsOpen != turnOn)
                            switchNodes.Enqueue(enumerator.Current);
                    }
                }

                if (switchNode.SwitchTurnMode == TurnMode.LayerTopToDown)
                {
                    SwitchNodeList.ChangeSwitchNodeState(switchNode, turnOn);
                }
                else
                {
                    SwitchNodeList.InvokeSwitch(switchNode, turnOn);
                }
            }
        }

        protected virtual void InvokeLayerDownToTop(bool turnOn)
        {
            var count = m_ChildNodes.Count + 1;
            Queue<SwitchNode> switchNodes = new Queue<SwitchNode>(count);
            Stack<SwitchNode> switchOrderNodes = new Stack<SwitchNode>(count);
            switchNodes.Enqueue(this);

            while (switchNodes.Count > 0)
            {
                var switchNode = switchNodes.Dequeue();
                switchOrderNodes.Push(switchNode);
                var enumerator = switchNode.GetEnumerator();

                if (switchNode.SwitchTurnMode == TurnMode.LayerDownToTop)
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.IsOpen != turnOn)
                            switchNodes.Enqueue(enumerator.Current);
                    }
                }
            }

            while (switchOrderNodes.Count > 0)
            {
                var switchNode = switchOrderNodes.Pop();
                if (switchNode.SwitchTurnMode == TurnMode.LayerDownToTop)
                {
                    SwitchNodeList.ChangeSwitchNodeState(switchNode, turnOn);
                }
                else
                {
                    SwitchNodeList.InvokeSwitch(switchNode, turnOn);
                }
            }
        }

        protected virtual void InvokeLayerReverseTopToDown(bool turnOn)
        {
            var count = m_ChildNodes.Count + 1;
            Queue<SwitchNode> switchNodes = new Queue<SwitchNode>(count);
            Stack<SwitchNode> switchReverseOrderNodes = new Stack<SwitchNode>(count);
            switchNodes.Enqueue(this);

            while (switchNodes.Count > 0)
            {
                var switchNode = switchNodes.Dequeue();
                var enumerator = switchNode.GetEnumerator();

                if (switchNode.SwitchTurnMode == TurnMode.LayerReverseTopToDown)
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.IsOpen != turnOn)
                            switchReverseOrderNodes.Push(enumerator.Current);
                    }
                }

                while (switchReverseOrderNodes.Count > 0)
                {
                    switchNodes.Enqueue(switchReverseOrderNodes.Pop());
                }

                if (switchNode.SwitchTurnMode == TurnMode.LayerReverseTopToDown)
                {
                    SwitchNodeList.ChangeSwitchNodeState(switchNode, turnOn);
                }
                else
                {
                    SwitchNodeList.InvokeSwitch(switchNode, turnOn);
                }
            }
        }

        protected virtual void InvokeLayerReverseDownToTop(bool turnOn)
        {
            var count = m_ChildNodes.Count + 1;
            Queue<SwitchNode> switchNodes = new Queue<SwitchNode>(count);
            Stack<SwitchNode> switchOrderNodes = new Stack<SwitchNode>(count);
            Stack<SwitchNode> switchReverseOrderNodes = new Stack<SwitchNode>(count);
            switchNodes.Enqueue(this);

            while (switchNodes.Count > 0)
            {
                var switchNode = switchNodes.Dequeue();
                switchOrderNodes.Push(switchNode);
                var enumerator = switchNode.GetEnumerator();

                if (switchNode.SwitchTurnMode == TurnMode.LayerReverseDownToTop)
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.IsOpen != turnOn)
                            switchReverseOrderNodes.Push(enumerator.Current);
                    }
                }

                while (switchReverseOrderNodes.Count > 0)
                {
                    switchNodes.Enqueue(switchReverseOrderNodes.Pop());
                }
            }

            while (switchOrderNodes.Count > 0)
            {
                var switchNode = switchOrderNodes.Pop();
                if (switchNode.SwitchTurnMode == TurnMode.LayerReverseDownToTop)
                {
                    SwitchNodeList.ChangeSwitchNodeState(switchNode, turnOn);
                }
                else
                {
                    SwitchNodeList.InvokeSwitch(switchNode, turnOn);
                }
            }
        }
    }
}