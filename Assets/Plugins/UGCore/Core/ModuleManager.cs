using System;
using System.Collections.Generic;
using UnityEngine;

namespace UGCore
{
    public sealed class ModuleManager
    {
        private static ModuleManager s_instance;
        public static ModuleManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new ModuleManager();
                }
                return s_instance;
            }        
        }

        private Dictionary<string, GameModule> m_AllGameModules = new Dictionary<string, GameModule>();

        private ModuleManager()
        {

        }

        public GameModule GetGameModule(string moduleName)
        {
            if (m_AllGameModules.ContainsKey(moduleName))
            {
                return m_AllGameModules[moduleName];
            }
            return null;
        }

        public GameModule AddGameModule(Type moduleType)
        {
            var moduleName = moduleType.Name;
            if (!m_AllGameModules.ContainsKey(moduleName))
            {
                var moduleGo = new GameObject(moduleName);
                moduleGo.transform.parent = GameCore.Instance.transform;
                var module = moduleGo.AddComponent(moduleType) as GameModule;
                if (module != null)
                {
                    m_AllGameModules.Add(moduleName, module);
                    return module;
                }
                return null;
            }
            Debug.LogError("Exist same module name :" + moduleName);
            return null;
        }
    }
}
