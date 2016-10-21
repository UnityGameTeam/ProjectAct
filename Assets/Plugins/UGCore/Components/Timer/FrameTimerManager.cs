//******************************
//
// 模块名   : FrameTimerManager
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : 使用Unity的Time.time的超时时间设计的定时器管理器
//
//******************************

using UnityEngine;

namespace UGCore.Components
{
    public class FrameTimerManager : TimerManager
    {
        protected uint mPausedTime;
        protected uint mLastPausedTime;

        public FrameTimerManager(uint granularity = 20) : base(granularity)
        {

        }

        public override void Reset()
        {
            base.Reset();
            mPausedTime = 0;
            mLastPausedTime = 0;
        }

        protected override void OnPause()
        {
            base.OnPause();
            mLastPausedTime = GetCurrentMillisecond();
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            mPausedTime += GetCurrentMillisecond() - mLastPausedTime;
            mLastPausedTime = 0;
        }

        protected override uint GetCurrentMillisecond()
        {
            return (uint)(Time.time * 1000 - mPausedTime);
        }
    }
}
