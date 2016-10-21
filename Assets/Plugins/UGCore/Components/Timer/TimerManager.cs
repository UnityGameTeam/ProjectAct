//******************************
//
// 模块名   : TimerManager
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : 抽象的定时器管理器
//
//******************************

using System;
using System.Collections.Generic;

namespace UGCore.Components
{
    /// <summary>
    /// TimerManager是根据Linux的内核定时器的多级时间轮思想设计的用于游戏使用的定时器管理器
    /// 默认的的定时器Tick间隔为20ms，总共使用5个简单时间轮，每个时间轮的最大槽数分别是: 2^8, 2^6, 2^6, 2^6, 2^6
    ///
    /// 使用
    ///     实现一个具体的定时器管理器继承于TimerManager,可参考GeneralTimerManager等,TimerManager支持暂时和重启功能，
    ///不建议使用单例模式，可用于游戏中本地倒计时或者CD等可以暂时的需求,比如生成一个普通的定时器管理器用于定时游戏活动
    ///生成一个本地战斗定时器管理器,用于管理战斗中的倒计时，CD等可能要暂停的功能，避免上层逻辑过多处理 
    ///     生成一个定时器管理器后，需要隔一段时间调用Tick函数，比如在Unity的Update,LateUpdate或者InvokeRepeating("MethodName",0,0.02f)
    ///中调用，这里一般推荐用InvokeRepeating来调用Tick,这样可能在一帧中调用多次，特别是很卡的一帧中，不过InvokeRepeating的调用
    ///可能在Update之前之后或者LateUpdate的之前之后，因此如果放在定时器中的回调函数需要保证顺序则不建议这样使用，或者这样的回调不应
    ///放入定时器
    ///
    /// 暂停功能
    ///     暂停功能需要调用Pause和Restart，需要注意在Unity中一帧中，如果调用顺序Pause Tick Restart,原本在当前Tick中可执行的定时器
    /// 需要到下一次的Tick才会执行，TimerManager的设计默认在没有定时器的情况下会自动暂停，当暂停的时候可以有效减少Tick中无用的计算
    /// AutoPauseMode = true默认当没有定时器的时候会自动暂停，添加定时器的时候会自动重启，如果想要外部自行控制暂停和重启，AutoPuaseMode
    /// 需要设置为false,然后手动调用Pause和Restart方法，当Reset的时候AutoPuaseMode默认会重新设置为true
    /// </summary>
    public abstract class TimerManager
    {
        public readonly uint    Granularity;    //定时器的默认时间Tick间隔是20ms
        protected TimeWheel[]   mTimeWheels;
        protected uint          mCheckTime;
        protected bool          mIsPaused;
        protected int           mTotalTimerNum;
        protected bool          mAutoPauseMode;

        protected LinkedList<TimerNode>         mReadyToInvokeList;
        protected ITimerCallbackExceptionOutput mCallbackExceptionOutput;
        protected Stack<TimerNode>              mTimerNodeCache; 

        public bool AutoPauseMode { get { return mAutoPauseMode; } set { mAutoPauseMode = value; } }

        public bool IsPaused { get { return mIsPaused;} }

        /// <summary>
        /// 当前经过了多少个Granularity,CurrentCheckTime = n * Granularity
        /// </summary>
        public uint CurrentCheckTime { get { return mCheckTime;} }

        protected TimerManager(uint granularity = 20)
        {
            if(granularity <= 0)
            {
                throw new Exception("Granularity must be > 0");
            }
            Granularity = granularity;

            mAutoPauseMode = true;
            mTimeWheels = new TimeWheel[5];
            mTimeWheels[0] = new TimeWheel(256);
            for (int i = 1; i < 5; ++i)
            {
                mTimeWheels[i] = new TimeWheel(64);
            }
            mReadyToInvokeList = new LinkedList<TimerNode>();
            mTimerNodeCache = new Stack<TimerNode>();
        }

        protected TimerManager(uint granularity, ITimerCallbackExceptionOutput callbackExceptionOutput) : this(granularity)
        {
            SetCallbackExceptionOutput(callbackExceptionOutput);
        }
 
        public virtual void Reset()
        {
            mCheckTime = 0;
            mIsPaused = false;
            mAutoPauseMode = true;
            mTotalTimerNum = 0;
            for (int i = 0; i < 5; ++i)
            {
                mTimeWheels[i].Clear();
            }
            mReadyToInvokeList.Clear();
            mTimerNodeCache.Clear();
        }

        public void Pause()
        {
            if (mIsPaused)
            {
                return;
            }
            mIsPaused = true;
            OnPause();
        }

        //子类需要实现的暂停函数，Pause管理暂停相关状态，子类无需实现
        protected virtual void OnPause()
        {
            
        }

        public void Restart()
        {
            if (!mIsPaused)
            {
                return;
            }
            mIsPaused = false;
            OnRestart();
        }

        protected virtual void OnRestart()
        {

        }

        public void SetCallbackExceptionOutput(ITimerCallbackExceptionOutput callbackExceptionOutput)
        {
            mCallbackExceptionOutput = callbackExceptionOutput;
        }

        protected virtual uint GetCurrentMillisecond()
        {
            return 0;
        }

        public virtual void CacheTimerNode(TimerNode timerNode)
        {
            if (timerNode != null)
            {
                timerNode.RemoveTimer();
                mTimerNodeCache.Push(timerNode);
            }
        }

        public virtual TimerNode AddTimer(uint startTime, int intervalTime, Action callback,bool continueInvoke = false)
        {
            TimerNode timerNode = null;
            if (mTimerNodeCache.Count == 0)
            {
                timerNode = new TimerNode(mCheckTime + startTime, intervalTime, callback, continueInvoke, this);
            }
            else
            {
                timerNode = mTimerNodeCache.Pop();
                timerNode.RemoveTimer();
                timerNode.DeadTime = mCheckTime + startTime;
                timerNode.Interval = intervalTime;
                timerNode.Callback = callback;
                timerNode.ContinueInvoke = continueInvoke;
                timerNode.TimerManagerHost = this;
            }
            
            return AddTimerNode(startTime, timerNode);
        }

        /// <summary>
        /// 定时器超时后，用于继续添加需要间隔执行的TimerNode
        /// </summary>
        protected virtual void AddIntervalTimer(uint startTime, TimerNode timerNode)
        {
            timerNode.DeadTime = mCheckTime + startTime;
            AddTimerNode(startTime, timerNode);
        }

        public virtual void DeleteTimer(TimerNode timerNode)
        {
            if (timerNode.TimerManagerHost != this)
            {
                throw new Exception("timerNode.timerManagerHost != this,cannot delete the timerNode");
            }
            timerNode.TimerNodeHost.List.Remove(timerNode.TimerNodeHost);
            --mTotalTimerNum;

            if (mAutoPauseMode && mTotalTimerNum <= 0)
            {
                Pause();
            }
        }

        /// <summary>
        /// 将超时的定时器加入到准备列表中，在DoTimeoutCallback中统一处理
        /// </summary>
        /// <param name="timerNodeHost"></param>
        protected void AddToReadyList(LinkedListNode<TimerNode> timerNodeHost)
        {
            mReadyToInvokeList.AddLast(timerNodeHost);
        }

        /// <summary>
        /// 安全调用回调函数
        /// </summary>
        /// <param name="callback"></param>
        protected void SafeInvokeCallback(Action callback)
        {
            try
            {
                if (callback != null)
                {
                    callback();
                }
            }
            catch (Exception exception)
            {
                if (mCallbackExceptionOutput != null)
                {
                    mCallbackExceptionOutput.ProcessExceptionMsg(exception);
                }
            }
        }

        /// <summary>
        /// 执行所有的定时器超时回调
        /// </summary>
        protected void DoTimeoutCallback()
        {
            for (var timerNodeHost = mReadyToInvokeList.First; timerNodeHost != null;)
            {
                var nextTimerNodeHost = timerNodeHost.Next;
                mReadyToInvokeList.Remove(timerNodeHost);

                var timerNode = timerNodeHost.Value;
                if (timerNode.Interval > 0)
                {
                    if (timerNode.ContinueInvoke)
                    {
                        var overtime = mCheckTime - timerNode.DeadTime;
                        var invokeTimes = overtime/timerNode.Interval + 1;
                        uint newStartTime = (uint)(timerNode.Interval - (overtime % timerNode.Interval));
                        AddIntervalTimer(newStartTime, timerNode);
                        while (invokeTimes > 0 && !timerNode.IsRemoved)
                        {
                            SafeInvokeCallback(timerNode.Callback);
                            --invokeTimes;
                        }
                    }
                    else
                    {
                        AddIntervalTimer((uint)timerNode.Interval, timerNode);
                        SafeInvokeCallback(timerNode.Callback);
                    }
                }
                else
                {
                    SafeInvokeCallback(timerNode.Callback);
                }

                timerNodeHost = nextTimerNodeHost;
            }
        }

        /// <summary>
        /// 外部调用，每隔一定的时间Tick一次
        /// </summary>
        public virtual void Tick()
        {
            if (mIsPaused)
            {
                return;
            }

            uint now = GetCurrentMillisecond();
            uint loopNum = now > mCheckTime ? (now - mCheckTime) / Granularity : 0;

            TimeWheel wheel = mTimeWheels[0];
            for (uint i = 0; i < loopNum; ++i)
            {
                LinkedList<TimerNode> spoke;
                if (wheel.TryGetSpoke((int)wheel.CurrentSpokeIndex,out spoke))
                {
                    for (var timerNodeHost = spoke.First; timerNodeHost != null;)
                    {
                        var nextTimerNodeHost = timerNodeHost.Next;
                        timerNodeHost.List.Remove(timerNodeHost);
                        timerNodeHost.Value.TimerNodeHost = null;
                        --mTotalTimerNum;
                        AddToReadyList(timerNodeHost);
                        timerNodeHost = nextTimerNodeHost;
                    }
                }

                mCheckTime += Granularity;

                ++wheel.CurrentSpokeIndex;
                if (wheel.CurrentSpokeIndex >= wheel.MaxSpokes)
                {
                    wheel.CurrentSpokeIndex = 0;
                    Cascade(1);
                }    
            }

            DoTimeoutCallback();

            if (mAutoPauseMode && mTotalTimerNum <= 0)
            {          
                Pause();
            }
        }

        /// <summary>
        /// 添加定时器到相应的时间轮的插槽上
        /// </summary>
        protected virtual TimerNode AddTimerNode(uint millisecond, TimerNode timerNode)
        {
            uint interval   = millisecond / Granularity;   // 计算毫秒数跨越了多少个单位时间刻度
            if (interval != 0 && millisecond % Granularity == 0)
            {
                --interval;
            }

            uint threshold1 = 256;                         // 1 << 8           第一个时间轮代表的最大刻度数
            uint threshold2 = 16384;                       // 1 << (8 + 6)     第二个时间轮代表的最大刻度数
            uint threshold3 = 1048576;                     // 1 << (8 + 2 * 6) 第三个时间轮代表的最大刻度数
            uint threshold4 = 67108864;                    // 1 << (8 + 3 * 6) 第四个时间轮代表的最大刻度数

            if (interval < threshold1)
            {
                //跨度小于第一个时间轮的跨度，加入到第一个时间轮中，并通过取模获取在时间轮中的哪个插槽上，以下类似
                uint index = (interval + mTimeWheels[0].CurrentSpokeIndex) & 255; //使用位运算对2^8 = 256 进行取模
                mTimeWheels[0].AddTimerNode((int)index, timerNode);
            }
            else if (interval < threshold2)
            {
                uint index = ((interval - threshold1 + mTimeWheels[1].CurrentSpokeIndex * threshold1) >> 8) & 63;
                mTimeWheels[1].AddTimerNode((int)index, timerNode);
            }
            else if (interval < threshold3)
            {
                //uint index = ((interval - threshold2 + mTimeWheels[2].CurrentSpokeIndex * threshold2) >> (8 + 6)) & 63;
                uint index = ((interval - threshold2 + mTimeWheels[2].CurrentSpokeIndex * threshold2) >> 14) & 63;
                mTimeWheels[2].AddTimerNode((int)index, timerNode);
            }
            else if (interval < threshold4)
            {
                //uint index = ((interval - threshold3 + mTimeWheels[3].CurrentSpokeIndex * threshold3) >> (8 + 2 * 6)) & 63;
                uint index = ((interval - threshold3 + mTimeWheels[3].CurrentSpokeIndex * threshold3) >> 20) & 63;
                mTimeWheels[3].AddTimerNode((int)index, timerNode);
            }
            else
            {
                //uint index = ((interval - threshold4 + mTimeWheels[4].CurrentSpokeIndex * threshold4) >> (8 + 3 * 6)) & 63;
                uint index = ((interval - threshold4 + mTimeWheels[4].CurrentSpokeIndex * threshold4) >> 26) & 63;
                mTimeWheels[4].AddTimerNode((int)index, timerNode);
            }

            ++mTotalTimerNum;
            if (mAutoPauseMode)
            {
                Restart();
            }          
            return timerNode;
        }

        /// <summary>
        /// 上一个时间轮转过一轮后，检索下一个时间轮当前插槽中的定时器
        /// </summary>
        /// <param name="wheelIndex"></param>
        protected virtual void Cascade(uint wheelIndex)
        {
            if (wheelIndex < 1 || wheelIndex >= 5)
            {
                return;
            }

            TimeWheel wheel = mTimeWheels[wheelIndex];
            int currentSpokeIndex = (int) wheel.CurrentSpokeIndex;
            ++wheel.CurrentSpokeIndex;
            LinkedList<TimerNode> spoke;
            if (wheel.TryGetSpoke(currentSpokeIndex,out spoke))
            {
                for (var timerNodeHost = spoke.First; timerNodeHost != null;)
                {
                    var nextTimerNodeHost = timerNodeHost.Next;
                    timerNodeHost.List.Remove(timerNodeHost);
                    var timerNode = timerNodeHost.Value;
                    --mTotalTimerNum;
                    if (timerNode.DeadTime <= mCheckTime)
                    {
                        timerNode.TimerNodeHost = null;
                        AddToReadyList(timerNodeHost);
                    }
                    else
                    {
                        AddTimerNode(timerNode.DeadTime - mCheckTime, timerNode);
                    }
                    timerNodeHost = nextTimerNodeHost;
                }    
            }

            if (wheel.CurrentSpokeIndex >= wheel.MaxSpokes)
            {
                wheel.CurrentSpokeIndex = 0;
                Cascade(++wheelIndex);
            }
        }

    }
}
