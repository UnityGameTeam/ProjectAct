using System;
using System.Collections.Generic;
using System.Threading;
using UGCore;
using UnityEngine;

namespace GameLogic.Components
{
    public class ActionShcheduler : GameModule
    {
        public static  int  _maxThreads = 10;
        private static int  _numThreads;
  
        private int           m_Count;
        private Queue<Action> m_Actions = new Queue<Action>();
        private Queue<Action> m_AsyncActions = new Queue<Action>();

        private static ActionShcheduler _instance;
        public static ActionShcheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ModuleManager.Instance.AddGameModule(typeof(ActionShcheduler)) as ActionShcheduler;
                    Initialize();
                }
                return _instance;
            }
        }

        private static void Initialize()
        {
            ThreadPool.SetMaxThreads(20, 20);
            ThreadPool.SetMinThreads(10, 10);
        }

        public void QueueOnMainThread(Action action)
        {
            lock (m_Actions)
                m_Actions.Enqueue(action);
        }

        public void RunAsync(Action a)
        {
            if (_numThreads < _maxThreads)
            {
                Interlocked.Increment(ref _numThreads);
                ThreadPool.QueueUserWorkItem(RunAction, a);
            }
            else
            {
                m_AsyncActions.Enqueue(a);
            }
        }

        private void RunAction(object action)
        {
            try
            {
                ((Action) action)();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                Interlocked.Decrement(ref _numThreads);
            }
        }

        public void Clear()
        {
            m_Actions.Clear();
        }

        protected void Update()
        {
            if (m_Actions.Count > 0)
            {
                lock (m_Actions)
                {
                    m_Count = m_Actions.Count;
                    while (m_Count-- > 0)
                        m_Actions.Dequeue()();
                }
            }

            while (m_AsyncActions.Count > 0 && _numThreads < _maxThreads)
            {
                Interlocked.Increment(ref _numThreads);
                ThreadPool.QueueUserWorkItem(RunAction, m_AsyncActions.Dequeue());
            }
        }
    }
}