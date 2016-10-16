using System;
using System.Collections;
using System.Collections.Generic;
using GameLogic.LogicModules;
using GameLogic.Modules;
using UGCore;
using UnityEngine;

namespace GameLogic.Core
{
    public class LogicModuleLoader : MonoBehaviour
    {
        private IEnumerator Start()
        {
            var gameModulesList = new List<Type>();

            //gameModulesList.Add(typeof(GameStartConfigModule));
            gameModulesList.Add(typeof(LocalStorageModule));
            gameModulesList.Add(typeof(AssetLoadModule));
            gameModulesList.Add(typeof(GameDataLoadModule));

            gameModulesList.Add(typeof(PlatformMessageManageModule));
            gameModulesList.Add(typeof(GameStartModule));

            gameModulesList.Add(typeof(UIManageModule));
            gameModulesList.Add(typeof(InitializeBallModule));

            for (int i = 0; i < gameModulesList.Count; ++i)
            {
                var module  = ModuleManager.Instance.AddGameModule(gameModulesList[i]);
                yield return module.LoadModuleAsync();
            }

            GameObject.Destroy(gameObject);
        }
    }
}