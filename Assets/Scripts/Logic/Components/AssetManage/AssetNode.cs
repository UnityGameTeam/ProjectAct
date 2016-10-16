using UGCore.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic.Components
{
    public class AssetNode
    {
        private static ObjectCachePool<AssetNode> s_AssetNodes = new ObjectCachePool<AssetNode>(InitializeAssetNode, null);

        public Object         Target           { get; set; }
        public string         Name             { get; set; }
        public int            ReferenceCount   { get; set; }
        public float          LastAccessTime   { get; set; }

        public static AssetNode GetAssetNode()
        {
            return s_AssetNodes.Get();
        }

        public static void ReleaseAssetNode(AssetNode assetNode)
        {
            assetNode.Target = null;
            s_AssetNodes.Release(assetNode);
        }

        private static void InitializeAssetNode(AssetNode assetNode)
        {
            assetNode.Target         = null;
            assetNode.Name           = string.Empty;
            assetNode.ReferenceCount = 1;
            assetNode.LastAccessTime = Time.unscaledTime;
        }
    }
}