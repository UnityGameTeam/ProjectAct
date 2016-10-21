using UnityEngine;

namespace UGCore
{
    public class GameCore : MonoBehaviour
    {
        protected static GameCore _instance;
        public static GameCore Instance
        {
            get { return _instance; }
        }

        protected GameCore()
        {
            
        }

        protected void Awake()
        {
            DontDestroyOnLoad(this);
            _instance = this;

            StartCoroutine(CoreModuleLoader.LoadCoreGameModule());
        }
    }
}
