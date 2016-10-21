//******************************
//
// 模块名   : VelocityTracker
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : 速度追踪，用于获取平滑的ListView滑动速度
//
//******************************
using System;
using UnityEngine;

namespace UguiExtensions
{
    public class VelocityTracker
    {
        private static readonly int NUM_PAST = 10;
        private static readonly int LONGEST_PAST_TIME = 200;
        public static readonly float FLING_MIN_VELOCITY = 50;

        private static VelocityTracker _pool = new VelocityTracker();

        private float[] m_PastX = new float[NUM_PAST];
        private float[] m_PastY = new float[NUM_PAST];
        private long[]  m_PastTime = new long[NUM_PAST];
        private int[]   m_PastFrame = new int[NUM_PAST];

        private float m_YVelocity;
        private float m_XVelocity;

        public static VelocityTracker Obtain()
        {
            if (_pool != null)
            {
                _pool.Clear();
                return _pool;
            }
            return new VelocityTracker();
        }

        public void Recycle()
        {
            _pool = this;
        }

        private VelocityTracker()
        {
        }

        public void Clear()
        {
            m_PastTime[0] = 0;
        }


        public void AddMovement(Vector2 eventData)
        {
            AddPoint(eventData.x, eventData.y, (long)(Time.unscaledTime * 1000),Time.frameCount);
        }

        private void AddPoint(float x, float y, long time,int frame)
        {
            int drop = -1;
            int i;
            
            var pastTime = m_PastTime;
            for (i = 0; i < NUM_PAST; i++)
            {
                if (pastTime[i] == 0)
                {
                    break;
                }
                if (pastTime[i] < time - LONGEST_PAST_TIME)
                {
                    drop = i;
                }
            }

            if (i == NUM_PAST && drop < 0)
            {
                drop = 0;
            }

            if (drop == i) drop--;
            var pastX = m_PastX;
            var pastY = m_PastY;
            var pastFrame = m_PastFrame;
            if (drop >= 0)
            {
                int start = drop + 1;
                int count = NUM_PAST - drop - 1;
                Array.Copy(pastX, start, pastX, 0, count);
                Array.Copy(pastY, start, pastY, 0, count);
                Array.Copy(pastTime, start, pastTime, 0, count);
                Array.Copy(pastFrame, start, pastFrame, 0, count);
                i -= (drop + 1);
            }

            if (i <= 0 || pastFrame[i - 1] != frame)
            {
                pastX[i] = x;
                pastY[i] = y;
                pastTime[i] = time;
                pastFrame[i] = frame;
            }

            i++;
            if (i < NUM_PAST)
            {
                pastTime[i] = 0;
            }
        }

        public void ComputeCurrentVelocity(int units)
        {
            var pastX = m_PastX;
            var pastY = m_PastY;
            var pastTime = m_PastTime;

            float oldestX = pastX[0];
            float oldestY = pastY[0];
            long  oldestTime = pastTime[0];
            float accumX = 0;
            float accumY = 0;
            int N = 0;
            while (N < NUM_PAST)
            {
                if (pastTime[N] == 0)
                {
                    break;
                }
                N++;
            }

            //跳过最新的事件，很可能是噪声
            if (N > 3) N--;

            for (int i = 1; i < N; i++)
            {
                int dur = (int) (pastTime[i] - oldestTime);
                if (dur == 0) continue;
                float dist = pastX[i] - oldestX;
                float vel = (dist/dur)*units; // pixels/frame.
                if (accumX == 0) accumX = vel;
                else accumX = (accumX + vel)*.5f;

                dist = pastY[i] - oldestY;
                vel = (dist/dur)*units; // pixels/frame.
                if (accumY == 0) accumY = vel;
                else accumY = (accumY + vel)*.5f;
            }
            m_XVelocity = accumX;
            m_YVelocity = accumY;
        }

        public float GetXVelocity()
        {
            return m_XVelocity;
        }

        public float GetYVelocity()
        {
            return m_YVelocity;
        }
    }
}
