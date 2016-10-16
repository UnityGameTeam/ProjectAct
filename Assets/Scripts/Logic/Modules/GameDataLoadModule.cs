using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GameLogic.Components;
using UGCore;

namespace GameLogic.LogicModules
{
    public class GameDataLoadModule : GameModule
    {
        private List<List<string>> m_LoadPriorityList;
        private GameDataLoader     m_GameDataLoader;

        private int                m_LoadDoneCount;
        private int                m_BackgroundLoadCount;
        private int                m_BackgroundHasLoadedCount;

        private bool m_ModuleLoadDone;
        private bool m_HasDataConfig = true;

        public override IEnumerator LoadModuleAsync()
        {
            m_GameDataLoader = new GameDataLoader();

            var assetName = "Data/GameData/GameDataConfig";
            AssetManager.Instance.LoadAssetAsync(assetName, (asset) =>
            {
                if (asset == null)
                {
                    m_HasDataConfig = false;
                    return;
                }

                var configBytes = (asset as TextAsset).bytes;
                var gameDataConfigParser = new GameDataConfigParser(configBytes);
                m_LoadPriorityList = gameDataConfigParser.GetLoadPriorityList();
                m_ModuleLoadDone = true;
                AssetManager.Instance.SubAssetReference(assetName);
            });

            while (!m_ModuleLoadDone)
            {
                if (!m_HasDataConfig)
                {
                    Debug.LogWarning("GameData配置文件无法被找到，GameData相关功能无法使用");
                    m_HasDataConfig = true;
                    yield break;
                }
                yield return null;
            }

            LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(25));
            var count = m_LoadPriorityList[0].Count;
            for (int i = 0; i < count; ++i)
            {
                var gameDataTypeName = m_LoadPriorityList[0][i];
                assetName = string.Format("Data/GameData/{0}", gameDataTypeName);
                AssetManager.Instance.LoadAssetAsync(assetName, (asset) =>
                {
                    var bytes = (asset as TextAsset).bytes;
                    m_GameDataLoader.LoadGameDataAsync(bytes, gameDataTypeName, () =>
                    {
                        ++m_LoadDoneCount;
                        AssetManager.Instance.SubAssetReference(assetName);
                        LoadingUI.Instance.SetLoadingBarProgressDelta(m_LoadDoneCount / (float)count);
                    });
                });
            }

            while (m_LoadDoneCount < count)
            {
                yield return null;
            }

            if(count == 0)
                LoadingUI.Instance.SetLoadingBarProgressDelta(1);

            LoadOtherGameDataAync();
        }

        private void LoadOtherGameDataAync()
        {
            for (int i = 1; i < m_LoadPriorityList.Count - 1; ++i)
            {
                var count = m_LoadPriorityList[i].Count;
                m_BackgroundLoadCount += count;
                for (int j = 0; j < count; ++j)
                {
                    var gameDataTypeName = m_LoadPriorityList[i][j];
                    var assetName = string.Format("Data/GameData/{0}", gameDataTypeName);
                    AssetManager.Instance.LoadAssetAsync(assetName, (asset) =>
                    {
                        var bytes = (asset as TextAsset).bytes;
                        m_GameDataLoader.LoadGameDataAsync(bytes, gameDataTypeName, () =>
                        {
                            AssetManager.Instance.SubAssetReference(assetName);
                            ++m_BackgroundHasLoadedCount;
                            if (m_BackgroundHasLoadedCount == m_BackgroundLoadCount)
                            {
                                AssetManager.Instance.ReleaseAssetBundle("Data/GameData/GameDataConfig");
                            }
                        });

                    },(AssetLoadPriority)(i - 1));
                }
            }

            if (m_BackgroundLoadCount <= 0)
            {
                AssetManager.Instance.ReleaseAssetBundle("Data/GameData/GameDataConfig");
            }
        }

        public void LoadGameDataSync(string gameDataTypeName)
        {
            AssetManager.Instance.LoadAssetSync(string.Format("Data/GameData/{0}", gameDataTypeName), (asset) =>
            {
                var bytes = (asset as TextAsset).bytes;
                m_GameDataLoader.LoadGameDataSync(bytes, gameDataTypeName);
            });
        }
    }
}