using System;
using System.Collections;
using System.Collections.Generic;
using UGCore.Modules;

namespace UGCore
{
    public static class CoreModuleLoader
    {
        public static IEnumerator LoadCoreGameModule()
        {
            var gameModulesList = new List<Type>();

            LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(1));
            LoadingUI.Instance.PushLoadTaskProgressDelta(0.2f);
            LoadingUI.Instance.SetLoadingBarProgress(0);

            gameModulesList.Add(typeof(LogManageModule));
            gameModulesList.Add(typeof(DeviceLaunchModule));
            gameModulesList.Add(typeof(TimerManageModule));
            //gameModulesList.Add(typeof(VersionControlModule));
            gameModulesList.Add(typeof(ScriptsLoadModule));

            for (int i = 0; i < gameModulesList.Count; ++i)
            {           
                var module = ModuleManager.Instance.AddGameModule(gameModulesList[i]);
                yield return module.LoadModuleAsync();
            }
        }
    }
}
