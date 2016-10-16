using System.Collections;
using System.Collections.Generic;
using GameLogic.Components;
using GameLogic.Model;
using UGCore;

namespace GameLogic.LogicModules
{
    public class LocalStorageModule : GameModule
    {
        private Dictionary<string,LocalStorageUnit> mLocalStorageDefaultData = new Dictionary<string, LocalStorageUnit>()
        {
           
        };

        public override IEnumerator LoadModuleAsync()
        {
            LocalStorage.LoadLocalStorage(UGCoreConfig.GetExternalConfigFolder()+ GameLogicConfig.LocalStorageFileName, mLocalStorageDefaultData);
            yield break;
        }
    }
}