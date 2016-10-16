
namespace GameLogic.Components
{
    public static class GameDataConfig
    {
        public static int LoadMaxCountPerFrame = 3000;  //默认数据加载如果数据量过大会异步分帧加载，每一帧最大加载数量
    }
}