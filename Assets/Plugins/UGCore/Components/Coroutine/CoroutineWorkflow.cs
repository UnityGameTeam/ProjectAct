using System.Collections;
using UGCore;
using UGFoundation.Collections.Generic;

namespace UGCore.Components
{
    public class CoroutineWorkflow
    {
        public delegate IEnumerator CoroutineAcion();

        protected LinkedDictionary<string, CoroutineAcion> m_CoroutineMap =
            new LinkedDictionary<string, CoroutineAcion>();

        public IEnumerator ExecuteTasksAsync()
        {
            while (m_CoroutineMap.Count > 0)
            {
                var task = m_CoroutineMap.RemoveFirst();
                if (task.Value != null)
                {
                    yield return GameCore.Instance.StartCoroutine(task.Value());
                }
            }
        }

        /// <summary>
        /// 增加到工作流的头部,moveTask用于控制当已经存在key的时候,是否将它移动到头部
        /// </summary>
        public void AddFirst(string key, CoroutineAcion coroutineAcion, bool replaceAction = true, bool moveTask = false)
        {
            if (m_CoroutineMap.ContainsKey(key))
            {
                if (replaceAction)
                {
                    m_CoroutineMap[key] = coroutineAcion;
                }

                if (moveTask)
                {
                    coroutineAcion = m_CoroutineMap[key];
                    m_CoroutineMap.Remove(key);
                    m_CoroutineMap.AddFirst(key, coroutineAcion);
                }
            }
            else
            {
                m_CoroutineMap.AddFirst(key, coroutineAcion);
            }
        }

        /// <summary>
        /// 增加到工作流的尾部,moveTask用于控制当已经存在key的时候,是否将它移动到尾部
        /// </summary>
        public void AddLast(string key, CoroutineAcion coroutineAcion, bool replaceAction = true, bool moveTask = false)
        {
            if (m_CoroutineMap.ContainsKey(key))
            {
                if (replaceAction)
                {
                    m_CoroutineMap[key] = coroutineAcion;
                }

                if (moveTask)
                {
                    coroutineAcion = m_CoroutineMap[key];
                    m_CoroutineMap.Remove(key);
                    m_CoroutineMap.AddLast(key, coroutineAcion);
                }
            }
            else
            {
                m_CoroutineMap.AddLast(key, coroutineAcion);
            }
        }

        public void Remove(string key)
        {
            m_CoroutineMap.Remove(key);
        }

        public void RemoveAll()
        {
            m_CoroutineMap.Clear();
        }
    }
}