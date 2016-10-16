using System;
using UGCore;
using UGCore.Components;
using UGCore.Modules;

public static class TimerHelper
{
    private static TimerManager s_MainTimerMgr; //上层模块一般使用这个定时器来定时

    static TimerHelper()
    {
        var timeManageModule = ModuleManager.Instance.GetGameModule(TimerManageModule.Name) as TimerManageModule;
        s_MainTimerMgr = timeManageModule.GeneralTimerMgr;
    }

    public static TimerNode AddTimer(uint startTime, int intervalTime, Action callback, bool continueInvoke = false)
    {
        return s_MainTimerMgr.AddTimer(startTime, intervalTime, callback, continueInvoke);
    }
}

