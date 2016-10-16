using System;
using System.Reflection;
using Data.GameData;
using UnityEngine;

namespace GameLogic.Components
{
    public class GameDataLoader
    {
        private GameDataSyncParser m_GameDataSyncParser;
        private GameDataAsyncParser m_GameDataAsyncParser;

        public GameDataLoader()
        {
            m_GameDataSyncParser = new GameDataSyncParser();
            m_GameDataAsyncParser = new GameDataAsyncParser();
        }

        public void LoadGameDataAsync(byte[] bytes,string typeName,Action action)
        {
            var type = Type.GetType("Data.GameData."+typeName);
            if (type == null)
            {
                Debug.LogWarning(string.Format("Data.GameData.{0} can not be found", typeName));
                return;
            }

            var getInstanceMethod = type.GetMethod("GetInstance", BindingFlags.NonPublic | BindingFlags.Static);
            var gameDataInstance = getInstanceMethod.Invoke(null,null) as GameDataBase;

            if (!gameDataInstance.IsLoadDone())
            {
                m_GameDataAsyncParser.GetGameDataAsync(bytes, typeName, gameDataInstance, action);
            }
        }


        public void LoadGameDataSync(byte[] bytes, string typeName)
        {
            if (m_GameDataAsyncParser.CurrentLoadTypeName == typeName)
            {
                m_GameDataAsyncParser.LoadDoneImmediately();
                return;
            }

            var type = Type.GetType(string.Format("Data.GameData.{0}", typeName));
            if (type == null)
            {
                Debug.LogWarning(string.Format("Data.GameData.{0} can not be found", typeName));
                return;
            }

            var getInstanceMethod = type.GetMethod("GetInstance", BindingFlags.NonPublic | BindingFlags.Static);
            var gameDataInstance = getInstanceMethod.Invoke(null, null) as GameDataBase;

            if (!gameDataInstance.IsLoadDone())
            {
                m_GameDataSyncParser.GetGameDataSync(bytes, typeName, gameDataInstance);
            }
        }
    }
}
