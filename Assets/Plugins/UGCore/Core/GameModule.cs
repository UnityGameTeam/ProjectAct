using System.Collections;
using UnityEngine;

namespace UGCore
{
    public abstract class GameModule : MonoBehaviour
    {
        protected GameModule()
        {
            
        }

        public virtual IEnumerator LoadModuleAsync()
        {
            yield break;
        }
 
        protected virtual void OnApplicationQuit()
        {

        }

        protected virtual void OnApplicationFocus()
        {

        }

        protected virtual void OnApplicationPause()
        {

        }
    }
}
