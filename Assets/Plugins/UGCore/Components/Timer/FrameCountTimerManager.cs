//******************************
//
// 模块名   : FrameCountTimerManager
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : 使用Unity的Time.frameCount的超时时间设计的定时器管理器,即帧数定时器
//
//******************************

using System;
using UnityEngine;

namespace UGCore.Components
{
    /// <summary>
    /// FrameCountTimerManager的Tick应该每帧调用一次
    /// </summary>
    public class FrameCountTimerManager : FrameTimerManager
    {
        public FrameCountTimerManager(uint granularity = 1) : base(granularity)
        {

        }

        /// <summary>
        /// 在帧数定时器管理器中，添加的Timer的startTime必须大于0，表示至少是下一帧才执行
        /// </summary>
        public override TimerNode AddTimer(uint startTime, int intervalTime, Action callback, bool continueInvoke = false)
        {
            if (startTime <= 0)
            {
                throw new Exception("startTime must be > 0 in FrameCountTimerManager");
            }

            if (GetCurrentMillisecond() != mCheckTime) //如果当前帧还没有Tick,startTime += 1，保证同一帧添加的定时器不会因为在Tick之前或之后添加导致不同的调用行为
            {
                ++startTime;
            }

            return base.AddTimer(mCheckTime + startTime, intervalTime, callback, continueInvoke);
        }

        protected override uint GetCurrentMillisecond()
        {
            return (uint)(Time.frameCount - mPausedTime);
        }
    }
}
