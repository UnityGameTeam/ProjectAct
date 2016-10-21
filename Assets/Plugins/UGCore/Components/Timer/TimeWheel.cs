//******************************
//
// 模块名   : TimeWheel
// 开发者   : 曾德烺
// 开发日期 : 2016-2-21
// 模块描述 : 用于定时器设计的TimeWheel(时间轮)结构
//
//******************************

using System;
using System.Collections.Generic;

namespace UGCore.Components
{
    /// <summary>
    /// 简单时间轮结构，使用hash算法的思想设计
    /// 可以说就是一个Dictionary<LinkedList<TimerNode>>
    /// 的时间，不过针对时间轮的特点优化了设计
    /// </summary>
    public class TimeWheel
    {
        private struct TimeWheelEntry
        {
            public int next;                  // 拉链法中用于指引下一个Entry在entries数组中的索引, 如果是链表的最后一个则为-1
            public int key;
            public LinkedList<TimerNode> value;
        }
   
        private int currentSpokeIndex = 0;   //当前时间轮的插槽索引
        private int maxSpokes;               //时间轮的最大插槽数目

        private int[] buckets;
        private TimeWheelEntry[] entries;
        private int count;
 
        private int freeList;
        private int freeCount;

        public int Count
        {
            get { return count - freeCount; }
        }

        public uint CurrentSpokeIndex
        {
            get { return (uint)currentSpokeIndex; }
            set { currentSpokeIndex = (int)value; }
        }

        public uint MaxSpokes
        {
            get { return (uint)maxSpokes; }      
        }

        public TimeWheel(int maxSpokes) : this(maxSpokes,0)
        {
            
        }

        public TimeWheel(int maxSpokes, int capacity)
        {
            if (maxSpokes <= 0) throw new ArgumentOutOfRangeException("maxSpokes");
            this.maxSpokes = maxSpokes;

            if (capacity < 0) new ArgumentOutOfRangeException("capacity", "Index was out of range. Must be non-negative and less than the size of the collection.");
            if (capacity > 0) Initialize(capacity);
        }

        private void Initialize(int capacity)
        {
            if (capacity > maxSpokes)
            {
                capacity = maxSpokes;
            }

            int size = capacity;
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new TimeWheelEntry[size];
            freeList = -1;
        }

        private void Insert(int key, LinkedList<TimerNode> value, bool add)
        {
            if (buckets == null) Initialize(2);

            int targetBucket = key % buckets.Length;
            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].key == key)
                {
                    if (add)
                    {
                        throw new Exception("An item with the same key has already been added.");
                    }
                    entries[i].value = value; 
                    return;
                }
            }

            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = key % buckets.Length;
                }
                index = count;
                count++;
            }

            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
        }

        private void Resize()
        {
            int newSize = count * 2;
            if (count < maxSpokes && count * 2 > maxSpokes)
            {
                newSize = maxSpokes;
            }

            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            TimeWheelEntry[] newEntries = new TimeWheelEntry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            for (int i = 0; i < count; i++)
            {
                int bucket = newEntries[i].key % newSize;
                newEntries[i].next = newBuckets[bucket];
                newBuckets[bucket] = i;
            }
            buckets = newBuckets;
            entries = newEntries;
        }

        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++)
                {
                    buckets[i] = -1;
                    if (entries[i].value != null)
                    {
                        entries[i].value .Clear();
                    }
                }
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
            }
        }

        public bool Remove(int key)
        {
            if (buckets != null)
            {
                int bucket = key % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
                {
                    if (entries[i].key == key)
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = entries[i].next;
                        }
                        else
                        {
                            entries[last].next = entries[i].next;
                        }
 
                        entries[i].next = freeList;
                        entries[i].key = 0;
                        entries[i].value = null;
                        freeList = i;
                        freeCount++;
                        return true;
                    }
                }
            }
            return false;
        }

        public bool TryGetSpoke(int key, out LinkedList<TimerNode> value)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                value = entries[i].value;
                return true;
            }
            value = null;
            return false;
        }

        private int FindEntry(int key)
        {
            if (buckets != null)
            {
                for (int i = buckets[key % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].key == key)
                        return i;
                }
            }
            return -1;
        }

        public void AddTimerNode(int spokeIndex,TimerNode timerNode)
        {
            LinkedList<TimerNode> value;
            if (!TryGetSpoke(spokeIndex,out value))
            {
                value = new LinkedList<TimerNode>();
                Insert(spokeIndex, value, true);
            }

            LinkedListNode<TimerNode> timerNodeHost = new LinkedListNode<TimerNode>(timerNode);
            timerNode.TimerNodeHost = timerNodeHost;
            value.AddLast(timerNodeHost);
        }
    }
}
