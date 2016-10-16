using System.Collections;
using UnityEngine;

namespace GameLogic.Components
{
    public class AssetBundleRecycleBin : AssetRecycleBin
    {
        private AssetBundleCollector m_AssetBundleCollector;

        public AssetBundleRecycleBin(AssetBundleCollector assetBundleCollector)
        {
            m_AssetBundleCollector = assetBundleCollector;
        }

        protected override IEnumerator WaitUnloadDone()
        {
            yield return Resources.UnloadUnusedAssets();

            var checkNum = 0;
            for (int i = 0; i < m_RecycleAssetList.Count; ++i)
            {
                if (m_RecycleAssetList[i] != null && !m_RecycleAssetList[i].IsAlive())
                {
                    m_AssetBundleCollector.SubAssetBundleReference(m_RecycleAssetList[i].Name);
                    RemoveAsset(m_RecycleAssetList[i].Name);

                    ++checkNum;
                    if (checkNum >= AssetLoadConfig.CheckAssetNumPerFrame)
                    {
                        checkNum = 0;
                        yield return null;
                    }
                }
            }

            m_ReleasingAsset = false;
        }
    }
}