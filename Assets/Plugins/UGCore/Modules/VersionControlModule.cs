using System.Collections;
using UGCore.Components;
using UnityEngine;

namespace UGCore.Modules
{
    public class VersionControlModule : GameModule
    {
        public override IEnumerator LoadModuleAsync()
        {
            VersionControl versionControl = new VersionControl();

            if (Application.platform == RuntimePlatform.Android)
            {
#if UNITY_ANDROID
                versionControl = new AndroidVersionControl();
#endif
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor ||
                     Application.platform == RuntimePlatform.WindowsPlayer)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                versionControl = new WindowsVersionControl();
#endif
            }

            yield return StartCoroutine(versionControl.CheckVersion());
        }       
    }
}