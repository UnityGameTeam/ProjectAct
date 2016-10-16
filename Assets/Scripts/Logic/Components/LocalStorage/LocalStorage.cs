using System;
using System.Collections.Generic;
using System.IO;
using UGCore;
using UGCore.Components;
using UGCore.Modules;
using UnityEngine;

namespace GameLogic.Components
{
    /// <summary>
    /// 解析和保存本地存储数据部分
    /// </summary>
    public partial class LocalStorage
    {
        protected delegate KeyValuePair<string, LocalStorageUnit> ParseDataAction(byte[] dataBuff, ref int offset);
        protected string m_LocalStoragePath;
        protected byte[] m_WriteBuffer;
        protected byte[] m_StorageBytes;

        protected Dictionary<short, ParseDataAction> DataParseMap = new Dictionary<short, ParseDataAction>()
        {
            { IntStorageUnit.ValueType,     IntStorageUnit.ParseData    },
            { BoolStorageUnit.ValueType,    BoolStorageUnit.ParseData   },
            { StringStorageUnit.ValueType,  StringStorageUnit.ParseData },
            { MapS2SStorageUnit.ValueType,  MapS2SStorageUnit.ParseData },
            { MapI2SStorageUnit.ValueType,  MapI2SStorageUnit.ParseData },
            { MapUL2SStorageUnit.ValueType, MapUL2SStorageUnit.ParseData},
            { MapI2BStorageUnit.ValueType,  MapI2BStorageUnit.ParseData },
            { MapUI2BStorageUnit.ValueType, MapUI2BStorageUnit.ParseData}
        };
   
        protected void LoadLocalData()
        {
            try
            {
#if !UNITY_WEBPLAYER
                if (File.Exists(m_LocalStoragePath))
#endif
                {
#if !UNITY_WEBPLAYER
                    m_StorageBytes = File.ReadAllBytes(m_LocalStoragePath);
#else
                    var dataStr = PlayerPrefs.GetString("LocalStorage");
                    if (string.IsNullOrEmpty(dataStr))
                    {
                        return;
                    }
                    m_StorageBytes = Convert.FromBase64String(dataStr);
#endif

                    var offset = 0;
                    var count = BitConverter.ToInt32(m_StorageBytes, offset);
                    offset += 4;

                    for (int i = 0; i < count; ++i)
                    {
                        var type = BitConverter.ToInt16(m_StorageBytes, offset);
                        offset += 2;

                        var dataItem = DataParseMap[type](m_StorageBytes, ref offset);
                        if (m_LocalData.ContainsKey(dataItem.Key) && m_LocalData[dataItem.Key].StorageValueType == type)
                        {
                            m_LocalData[dataItem.Key] = dataItem.Value;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("LoadLocalData parse error :" + e);
            }
        }

        protected void SaveLocalData()
        {
            if (m_WriteBuffer == null)
            {
                m_WriteBuffer = new byte[4096];
            }

            var offset = 0;
            LocalStorageUnit.AddDataToBuffer(m_WriteBuffer, offset, BitConverter.GetBytes(m_LocalData.Count));
            offset += 4;

            var enumerator = m_LocalData.GetEnumerator();
            while (enumerator.MoveNext())
            {
                offset = enumerator.Current.Value.ToBinaryData(enumerator.Current.Key, ref m_WriteBuffer, offset);
            }

#if !UNITY_WEBPLAYER
            var parentRoot = Path.GetDirectoryName(m_LocalStoragePath);
            if (!Directory.Exists(parentRoot))
            {
                Directory.CreateDirectory(parentRoot);
            }
            File.WriteAllBytes(m_LocalStoragePath, m_WriteBuffer);
#else
            var dataStr = Convert.ToBase64String(m_WriteBuffer);
            PlayerPrefs.SetString("LocalStorage",dataStr);
            PlayerPrefs.Save();
#endif
            m_StorageBytes = m_WriteBuffer;
        }

        public void AddLocalData(Dictionary<string, LocalStorageUnit> data)
        {
            try
            {
                var enumerator = data.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    m_LocalData.Add(enumerator.Current.Key, enumerator.Current.Value);
                }

                if (m_StorageBytes == null)
                {
                    return;
                }

                var offset = 0;
                var count = BitConverter.ToInt32(m_StorageBytes, offset);
                offset += 4;

                for (int i = 0; i < count; ++i)
                {
                    var type = BitConverter.ToInt16(m_StorageBytes, offset);
                    offset += 2;

                    var dataItem = DataParseMap[type](m_StorageBytes, ref offset);
                    if (data.ContainsKey(dataItem.Key) && data[dataItem.Key].StorageValueType == type)
                    {
                        m_LocalData[dataItem.Key] = dataItem.Value;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("AddLocalData error :" + e);
            }
        }
    }

    public partial class LocalStorage
    {
        private static LocalStorage _instance;
        public static LocalStorage Instance
        {
            get { return _instance;}
        }

        protected Dictionary<string, LocalStorageUnit> m_LocalData;
        protected TimerNode m_TimerNode;

        protected LocalStorage(string path, Dictionary<string, LocalStorageUnit> defaultData)
        {
            m_LocalStoragePath = path;
            m_LocalData        = defaultData;
            LoadLocalData();
        }

        public static void LoadLocalStorage(string path, Dictionary<string, LocalStorageUnit> defaultData)
        {
            if (_instance == null)
            {
                _instance = new LocalStorage(path, defaultData);
            }
        }

        public T GetValue<T>(string key)
        {
            return (T)m_LocalData[key].Value;
        }

        public bool ContainsKey(string key)
        {
            return m_LocalData.ContainsKey(key);
        }

        public void SetValue<T>(string key,T value, bool immediatelySave = false)
        {
            m_LocalData[key].Value = value;

            if (immediatelySave)
            {
                SaveLocalData();
            }

            if (m_TimerNode == null)
            {
                TimerManager generalTimerMgr = (ModuleManager.Instance.GetGameModule(TimerManageModule.Name) as TimerManageModule).GeneralTimerMgr;
                generalTimerMgr.CacheTimerNode(m_TimerNode);
                m_TimerNode = generalTimerMgr.AddTimer(200, 0, () =>
                {
                    SaveLocalData();
                    generalTimerMgr.CacheTimerNode(m_TimerNode);
                    m_TimerNode = null;
                });
            }
        }
    }
}

