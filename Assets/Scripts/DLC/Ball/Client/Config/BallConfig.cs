using System.Collections.Generic;
using GameLogic.Components;

namespace DLC.Ball.Config
{
    public class BallConfig
    {
        public static Dictionary<string, LocalStorageUnit> BallLocalStorageConfig = new Dictionary<string, LocalStorageUnit>()
        {
            {"BallQuality", new IntStorageUnit(1)}
        };

        public static int BallQuality
        {
            get { return LocalStorage.Instance.GetValue<int>("BallQuality"); }
            set { LocalStorage.Instance.SetValue("BallQuality", value); }
        }
    }
}
