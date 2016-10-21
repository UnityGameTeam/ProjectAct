//******************************
//
// 模块名   : GeneralTimerManager
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : 使用Stopwatch的超时时间设计的定时器管理器
//
//******************************

using System.Diagnostics;

namespace UGCore.Components
{
    public class GeneralTimerManager : FrameTimerManager
    {
        private Stopwatch mStopwatch;

        public GeneralTimerManager(uint granularity = 20) : base(granularity)
        {
            mStopwatch = new Stopwatch();
            mStopwatch.Start();
        }

        public override void Reset()
        {
            base.Reset();
            mStopwatch.Reset();
        }

        protected override uint GetCurrentMillisecond()
        {
            return (uint) (mStopwatch.ElapsedMilliseconds - mPausedTime);
        }
    }
}
