using System;
using UGCore.Modules;

namespace UGCore.Components
{
    public class Countdown
    {
        public const int SecondsDay = 86400;
        public const int SecondsHour = 3600;
        public const int SecondsMinute = 60;

        private int            seconds;
        private TimerNode      timerNode;
        private Action<string> onOutput;

        //支持的解析格式{dd} {hh} {mm} {ss}
        //d代表天 h代表时 m代表分 s代表秒
        //比如format = {ddddd},倒计时一天，这时输出 00001
        //比如format = {ddddd}:{hh}:{mm}:{ss},倒计时一天，这时输出 00001:00:00:00
        //{}内的字符数代表输出的位数，不足左边补0，{}最少有一个字符
        //一个倒计时格式串，format = "倒计时: {hh}:{mm}:{ss}"则天相关的信息不会输出
        //{hh} {mm} {ss} 还支持{hh+} {mm+} {ss+} 最后一个字符为+号的格式
        //比如倒计时2分钟，如果使用{ss} 则第一秒输出 00
        //如果使用{ss+} 则第一秒输出 120
        //{}的最后一个字符有+,代表直接输出有多少秒，多少分，多少小时(不对分，时，天进行 % 运算)，
        //{dd}不支持这个格式
        //比如需求是这样倒计时，"倒计时: 02:00,还剩120秒时间"
        //format可以这样写，"倒计时: {mm}:{ss},还剩{ss+}秒时间"
        private string         format = "{hh}:{mm}:{ss}"; 

        public Action OnFinish { get; set; }
        public Action<string> OnOutput
        {
            get { return onOutput; }
            set
            {
                if (value == null)
                {
                    onOutput = DefaultOutput;
                    return;
                }
                onOutput = value;
            }
        }

        public Countdown(int seconds, string format = null, Action<string> onOutputAction = null,Action onFinishAction = null)
        {
            this.seconds = seconds;
            this.OnOutput = onOutputAction;
            this.OnFinish = onFinishAction;
            if (format != null)
            {
                this.format = format;
            }

            FormatCountdown();

            var timerManagerModule = ModuleManager.Instance.GetGameModule(TimerManageModule.Name) as TimerManageModule;
            if (seconds == 0)
            {
                if (OnFinish != null)
                {
                    OnFinish();
                }
            }
            else
            {
                var timerManager = timerManagerModule.GeneralTimerMgr;
                timerManager.CacheTimerNode(timerNode);
                timerNode = timerManager.AddTimer(1000, 1000, OnTick, true);
            }
        }

        public void Reset(int seconds, string format = null)
        {
            this.seconds = seconds;
            if (format != null)
            {
                this.format = format;
            }

            FormatCountdown();

            var timeManagerModule = ModuleManager.Instance.GetGameModule(TimerManageModule.Name) as TimerManageModule;
            if (seconds == 0)
            {
                if (OnFinish != null)
                {
                    OnFinish();
                }
            }
            else
            {
                var timerManager = timeManagerModule.GeneralTimerMgr;
                timerManager.CacheTimerNode(timerNode);
                timerNode = timerManager.AddTimer(1000, 1000, OnTick, true);
            }
        }

        public void Cancel()
        {
            var timeManagerModule = ModuleManager.Instance.GetGameModule(TimerManageModule.Name) as TimerManageModule;
            var timerManager = timeManagerModule.GeneralTimerMgr;
            timerManager.CacheTimerNode(timerNode);
            timerNode.RemoveTimer();
        }

        public void FormatCountdown()
        {
            OnOutput(CountdownFormat.Format(format, seconds));
        }

        private void DefaultOutput(string result)
        {

        }

        public int GetLastSeconds()
        {
            return seconds;
        }

        private void OnTick()
        {
            --seconds;
            FormatCountdown();
            if (seconds <= 0)
            {
                Cancel();
                if (OnFinish != null)
                {
                    OnFinish();
                }
            }
        }
    }
}