using System;
using System.Collections.Generic;

namespace Pattern.Node
{
    /// <summary>
    /// SwitchNode的具体实现，使用HashSet保存子节点
    /// </summary>   
    public class SwtichNodeSet : SwitchNode
    {
        private HashSet<SwitchNode> m_ChildNodes;

        public override int ChildCount
        {
            get { return m_ChildNodes.Count; }
        }

        public SwtichNodeSet(Action<bool> toggleAction, NodeMode mode = NodeMode.ParalleConnection)
        {
            if (toggleAction != null)
                AddSwitchEvent(toggleAction);

            m_Name = string.Empty;
            SwitchNodeMode = mode;
            m_ChildNodes = new HashSet<SwitchNode>();
            SwitchTurnMode = TurnMode.TreeDownToTop;
        }

        protected override void DetachChild(SwitchNode childNode)
        {
            m_ChildNodes.Remove(childNode);

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
            List<SwitchNode> tempNodes = new List<SwitchNode>(ChildCount);
            var enumerator = m_ChildNodes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                tempNodes.Add(enumerator.Current);
            }

            for (var i = 0; i < tempNodes.Count; i++)
            {
                tempNodes[i].ParentNode = null;
            }
        }

        /// <summary>
        /// 由于子节点使用HashSet，Find如果有同名路径，可能得到不稳定的值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override SwitchNode Find(string name)
        {
            if (name == null)
            {
                return null;
            }

            string[] names = name.Split('/');
            return FindNode(names, 0);
        }

        /// <summary>
        /// 由于子节点使用HashSet，FindNode如果有同名路径，可能得到不稳定的值
        /// </summary>
        /// <param name="names"></param>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        public override SwitchNode FindNode(string[] names, int layerIndex)
        {
            var enumerator = m_ChildNodes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Name != names[layerIndex])
                    continue;

                if (layerIndex == names.Length - 1)
                {
                    return enumerator.Current;
                }

                var targetNode = enumerator.Current.FindNode(names, layerIndex + 1);
                if (targetNode != null)
                {
                    return targetNode;
                }
            }
            return null;
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
                case TurnMode.TreeReverseTopToDown:
                case TurnMode.TreeReverseDownToTop:
                    InvokeTreeMode(turnOn);
                    break;

                case TurnMode.LayerTopToDown:
                case TurnMode.LayerReverseTopToDown:
                    InvokeLayerTopToDown(turnOn);
                    break;

                case TurnMode.LayerDownToTop:
                case TurnMode.LayerReverseDownToTop:
                    InvokeLayerDownToTop(turnOn);
                    break;
            }
        }

        protected virtual void InvokeTreeMode(bool turnOn)
        {
            if (SwitchTurnMode == TurnMode.TreeTopToDown || SwitchTurnMode == TurnMode.TreeReverseTopToDown)
            {
                ChangeSwitchNodeState(this, turnOn);
            }

            var enumerator = m_ChildNodes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (enumerator.Current.IsOpen != turnOn)
                    InvokeSwitch(enumerator.Current, turnOn);
            }

            if (SwitchTurnMode == TurnMode.TreeDownToTop || SwitchTurnMode == TurnMode.TreeReverseDownToTop)
            {
                ChangeSwitchNodeState(this, turnOn);
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

                if (switchNode.SwitchTurnMode == TurnMode.LayerTopToDown || switchNode.SwitchTurnMode == TurnMode.LayerReverseTopToDown)
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current.IsOpen != turnOn)
                            switchNodes.Enqueue(enumerator.Current);
                    }
                }

                if (switchNode.SwitchTurnMode == TurnMode.LayerTopToDown || switchNode.SwitchTurnMode == TurnMode.LayerReverseTopToDown)
                {
                    ChangeSwitchNodeState(switchNode, turnOn);
                }
                else
                {
                    InvokeSwitch(switchNode, turnOn);
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

                if (switchNode.SwitchTurnMode == TurnMode.LayerDownToTop || switchNode.SwitchTurnMode == TurnMode.LayerReverseDownToTop)
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
                if (switchNode.SwitchTurnMode == TurnMode.LayerDownToTop || switchNode.SwitchTurnMode == TurnMode.LayerReverseDownToTop)
                {
                    ChangeSwitchNodeState(switchNode, turnOn);
                }
                else
                {
                    InvokeSwitch(switchNode, turnOn);
                }
            }
        }

    }
}