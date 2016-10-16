
namespace GameLogic.Components
{
    public class AssetLoadConfig
    {
        public static int   CheckAssetReferenceDeltaTime = 30000;  //检查资源引用的时间(毫秒)，用于将长期未访问的Asset的引用减1
        public static float AssetExpirationTime          = 45;     //资源长期未访问减少引用的过期时间(秒)
        public static float AssetReleaseExpirationTime   = 180;    //资源长期未访问移除到回收站时间(秒)
        public static int   CheckAssetNumPerFrame        = 10;     //AssetCachePool内部协程每帧检查的资源数
    }
}
