//******************************
//
// 模块名   : TimeNode
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : 定时器节点，保存于定时器管理器中的定时器对象
//
//******************************
using System;
using System.Collections.Generic;

namespace UGCore.Components
{
    public class TimerNode
    {
        /// <summary>
        /// TimerNode的超时回调函数，当定时器未被移除的时候，可以修改此回调
        /// 定时器的回调不应该做一些耗时的操作
        /// </summary>
        public Action Callback { get; set; }

        /// <summary>
        /// 定时超时后继续执行的间隔时间，当定时器未被移除的时候，可以修改此间隔
        /// </summary>
        public int    Interval { get; set; }

        /// <summary>
        /// 定时器的超时时间，对于间隔执行的定时器超时后，下一次的DeadTime = DeadTime + Interval
        /// 除了TimerManager外不应该调用set方法，DeadTime的设置统一由TimerManager管理，外部调用
        /// 可能导致未知的行为
        /// </summary>
        public uint   DeadTime { get; internal set; }

        /// <summary>
        /// 是否持续调用，此属性用于间隔执行的定时器
        /// 
        /// 由于Unity的定时器是单线程的，与渲染和逻辑处理处于一个线程，可能发生一种情况
        /// 
        ///     当前TimerManager在 1s 的时候Tick，这时添加一个TimerNode在1s后执行，间隔 
        ///     1s 后继续执行，如果这个线程做了非常耗时的操作，假设到了10s的才第二次Tick 
        /// 
        /// 1 如果ContinueInvoke = false则定时器只执行1次
        /// 2 如果ContinueInvoke = true 则定时器连续执行9次，即原来应该在2s,3s,4s,5s,6s,7s,8s,9s,10s处各执行一次
        ///  
        /// 
        /// 注意
        ///     如果只想执行 3次,比如到4s的时候就删除，对于ContinueInvoke = true的回调函数设计要特别小心，需要在回调
        /// 中判断达到条件的时候，把定时器节点从定时器管理器中删除，避免发生5s.6s.7s,8s,9s,10s的连续调用
        /// 
        /// 此属性可适用于倒计时的设计
        /// </summary>
        public bool ContinueInvoke { get; set; }

        /// <summary>
        /// TimerNode在双向链表中的宿主节点，外部程序集不应该访问
        /// </summary>
        internal LinkedListNode<TimerNode> TimerNodeHost { get; set; }

        /// <summary>
        /// 管理此TimerNode的TimerManager，外部程序集不应该访问
        /// </summary>
        internal TimerManager TimerManagerHost;

        /// <summary>
        /// TimerNode的构造函数，外部程序集不应该访问
        /// </summary>
        internal TimerNode(uint deadTime, int interval, Action callback,bool continueInvoke, TimerManager timerManagerHost)
        {
            DeadTime = deadTime;
            Interval = interval;
            Callback = callback;
            ContinueInvoke = continueInvoke;
            this.TimerManagerHost = timerManagerHost;
        }

        /// <summary>
        /// TimerNode是否已经从TimerManager中删除了
        /// </summary>
        public bool IsRemoved
        {
            get { return TimerNodeHost == null || TimerNodeHost.List == null; }
        }

        /// <summary>
        /// 从TimerManager中删除TimerNode
        /// </summary>
        public void RemoveTimer()
        {
            if (!IsRemoved)
            {
                //等价于 timerNode.TimerNodeHost.List.Remove(timerNode.TimerNodeHost);
                //不过为了定时器的优化设计，使用TimerManager来删除
                TimerManagerHost.DeleteTimer(this);
            }
        }
    }
}
