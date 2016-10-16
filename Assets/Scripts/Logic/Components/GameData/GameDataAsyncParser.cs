using System;
using System.Collections;
using System.Collections.Generic;
using Data.GameData;
using UGCore;
using UnityEngine;

namespace GameLogic.Components
{
    public class GameDataAsyncParser : GameDataParser
    {
        protected struct WaitLoadTask
        {
            public byte[]       DataBytes;
            public string       TypeName;
            public GameDataBase GameDataInstance;
            public Action       DoneAction;
        }

        private Queue<WaitLoadTask> m_WaitTaskQueue = new Queue<WaitLoadTask>();
        private bool      m_IsLoading;
        private byte[]    m_DataBytes;
        private string    m_TypeName;
        private Action    m_DoneAction;
        private int       m_ParseOffset;
        private int       m_HasLoadedCount;
        private Coroutine m_LoadCoroutine;

        public string CurrentLoadTypeName
        {
            get { return m_TypeName;}
        }

        public void GetGameDataAsync(byte[] bytes, string typeName, GameDataBase gameDataInstance, Action action)
        {
            if (!m_IsLoading)
            {
                m_IsLoading        = true;
                m_DataBytes        = bytes;
                m_TypeName         = typeName;
                m_GameDataInstance = gameDataInstance;
                m_DoneAction       = action;
                StartLoadData();
            }
            else
            {
                WaitLoadTask task = new WaitLoadTask();
                task.DataBytes        = bytes;
                task.TypeName         = typeName;
                task.GameDataInstance = gameDataInstance;
                task.DoneAction       = action;
                m_WaitTaskQueue.Enqueue(task);
            }
        }

        private void StartLoadData()
        {
            if (m_GameDataInstance.IsLoadDone())
            {
                if (m_WaitTaskQueue.Count > 0)
                {
                    var task = m_WaitTaskQueue.Dequeue();
                    m_DataBytes        = task.DataBytes;
                    m_TypeName         = task.TypeName;
                    m_GameDataInstance = task.GameDataInstance;
                    m_DoneAction       = task.DoneAction;
                    StartLoadData();
                    return;
                }
                m_IsLoading = false;
                return;
            }

            m_ParseOffset = 0;
            m_FieldTypeList.Clear();
            m_FieldNameList.Clear();
            m_StringBuffList.Clear();

            m_LoadCoroutine = GameCore.Instance.StartCoroutine(ParseGameDataAsync(m_DataBytes));
        }

        private IEnumerator ParseGameDataAsync(byte[] bytes)
        {
            m_ParseOffset = GetHeadInfo(bytes);
            m_ParseOffset = GetStringBuffInfo(bytes, m_ParseOffset);

            if (m_ItemCount > GameDataConfig.LoadMaxCountPerFrame)
            {
                yield return null;
            }

            var loadedCount = 0;
            for (int i = 0; i < m_ItemCount; i++)
            {
                m_ParseOffset = GetItems(bytes, m_ParseOffset);
                m_GameDataInstance.SaveData();
                ++loadedCount;

                m_HasLoadedCount = i + 1;
                if (m_HasLoadedCount < m_ItemCount)
                {
                    m_GameDataInstance = m_GameDataInstance.NewData();
                }

                if (loadedCount > GameDataConfig.LoadMaxCountPerFrame)
                {
                    loadedCount = 0;
                    yield return null;
                }
            }
            m_GameDataInstance.LoadDone();
            SafeInvokeAction();
            m_DataBytes = null;
            m_ParseOffset = 0;
            m_HasLoadedCount = 0;
            m_TypeName = "";
            m_DoneAction = null;

            if (m_WaitTaskQueue.Count > 0)
            {
                var task = m_WaitTaskQueue.Dequeue();
                m_DataBytes = task.DataBytes;
                m_TypeName = task.TypeName;
                m_GameDataInstance = task.GameDataInstance;
                m_DoneAction = task.DoneAction;
                StartLoadData();
            }
            else
            {
                m_IsLoading = false;
            }
        }

        void SafeInvokeAction()
        {
            try
            {
                if (m_DoneAction != null)
                    m_DoneAction();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public void LoadDoneImmediately()
        {
            GameCore.Instance.StopCoroutine(m_LoadCoroutine);

            for (int i = m_HasLoadedCount; i < m_ItemCount; i++)
            {
                m_ParseOffset = GetItems(m_DataBytes, m_ParseOffset);
                m_GameDataInstance.SaveData();

                m_HasLoadedCount = i + 1;
                if (m_HasLoadedCount < m_ItemCount)
                {
                    m_GameDataInstance = m_GameDataInstance.NewData();
                }
            }
            m_GameDataInstance.LoadDone();
            SafeInvokeAction();
            m_DataBytes = null;
            m_ParseOffset = 0;
            m_HasLoadedCount = 0;
            m_TypeName = "";
            m_DoneAction = null;

            if (m_WaitTaskQueue.Count > 0)
            {
                var task = m_WaitTaskQueue.Dequeue();
                m_DataBytes = task.DataBytes;
                m_TypeName = task.TypeName;
                m_GameDataInstance = task.GameDataInstance;
                m_DoneAction = task.DoneAction;
                StartLoadData();
            }
            else
            {
                m_IsLoading = false;
            }
        }
    }
}
