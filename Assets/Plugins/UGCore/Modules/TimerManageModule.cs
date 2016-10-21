using System;
using UnityEngine;
using UGCore.Components;

namespace UGCore.Modules
{
    public class TimerManageModule : GameModule,ITimerCallbackExceptionOutput
    {
        public const string Name = "TimerManageModule";

        //用于游戏底层模块使用的定时器
        public TimerManager CoreTimerMgr      { get; set; }
        public TimerManager GeneralTimerMgr   { get; set; }
        public TimerManager FrameTimeTimerMgr { get; set; }
        public TimerManager FrameCountTimerMgr{ get; set; }

        protected void Awake()
        {
            GeneralTimerMgr = new GeneralTimerManager();
            CoreTimerMgr = new GeneralTimerManager();
            FrameTimeTimerMgr = new FrameTimerManager();
            FrameCountTimerMgr = new FrameCountTimerManager();

            GeneralTimerMgr.SetCallbackExceptionOutput(this);
            CoreTimerMgr.SetCallbackExceptionOutput(this);
            FrameTimeTimerMgr.SetCallbackExceptionOutput(this);
            FrameCountTimerMgr.SetCallbackExceptionOutput(this);

            InvokeRepeating("Tick", 0, 0.02f);
        }

 
        protected void Tick()
        {
            CoreTimerMgr.Tick();
            GeneralTimerMgr.Tick();
            FrameTimeTimerMgr.Tick();
        }

        protected void Update()
        {
            FrameCountTimerMgr.Tick();
        }

        public void ProcessExceptionMsg(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
