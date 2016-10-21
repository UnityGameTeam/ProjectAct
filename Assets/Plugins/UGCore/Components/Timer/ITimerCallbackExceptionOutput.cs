//******************************
//
// 模块名   : ITimerCallbackExceptionOutput
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : TimerManager的定时器回调异常的输出处理接口
//
//******************************
using System;

namespace UGCore.Components
{
    /// <summary>
    /// TimerManager的定时器回调异常的输出接口，外部如果需要知道TimerManager调用回调函数
    /// 发生了什么异常，可以实现此接口，注入到TimerManager中
    /// </summary>
    public interface ITimerCallbackExceptionOutput
    {
        void ProcessExceptionMsg(Exception exception);
    }
}
