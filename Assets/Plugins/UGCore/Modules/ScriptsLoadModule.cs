using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UGCore.Components;
using UGCore.Utility;
using UnityEngine;

namespace UGCore.Modules
{
    public class ScriptsLoadModule : GameModule
    {
        protected CoroutineWorkflow m_CoroutineWorkflow = new CoroutineWorkflow();
        protected bool m_UseRuntimeScripts;

        public override IEnumerator LoadModuleAsync()
        {
            LoadingUI.Instance.ShowLoadingTip(LoadingLanguageData.Instance.GetString(8));
            m_CoroutineWorkflow.AddLast("LoadLogicModuleLoader", LoadLogicModuleLoader);
            yield return GameCore.Instance.StartCoroutine(m_CoroutineWorkflow.ExecuteTasksAsync());
        }

        protected IEnumerator LoadLogicModuleLoader()
        {
            GetCoreConfigInfo();
            if (Application.platform == RuntimePlatform.Android)
            {
                if (m_UseRuntimeScripts)
                {
                    m_CoroutineWorkflow.AddFirst("LoadRuntimeScripts", LoadRuntimeScripts);
                }
                else
                {
                    var logicLoaderGo = GameObject.Instantiate(Resources.Load("ReadonlyData/LogicModuleLoader")) as GameObject;
                    logicLoaderGo.transform.parent = GameCore.Instance.transform;
                    //Type type = Type.GetType("LogicModuleLoader");
                    //GameCore.Instance.gameObject.AddComponent(type);
                }
            }
            else
            {
                var logicLoaderGo = GameObject.Instantiate(Resources.Load("ReadonlyData/LogicModuleLoader")) as GameObject;
                logicLoaderGo.transform.parent = GameCore.Instance.transform;
                //Type type = Type.GetType("LogicModuleLoader");
                //GameCore.Instance.gameObject.AddComponent(type);
            }
            yield break;
        }

        protected void GetCoreConfigInfo()
        {
            var coreConfigAsset = Resources.Load("ReadonlyData/CoreReadonlyConfig") as TextAsset;
            var coreConfigInfo = JsonReader.Deserialize<Dictionary<string,System.Object>>(coreConfigAsset.text);
            Resources.UnloadAsset(coreConfigAsset);

            if (coreConfigInfo.ContainsKey("UseRuntimeScripts"))
            {
                m_UseRuntimeScripts = (bool)coreConfigInfo["UseRuntimeScripts"];
            }
        }

        protected IEnumerator LoadRuntimeScripts()
        {
            var www = new WWW(UGCoreConfig.GetAssetBundlePath("RuntimeScripts.pkg"));
            yield return www;

            if (www.error != null)
            {
                Debug.LogError("ScriptsLoadModule - load runtime scripts error : "+www.error);
                m_CoroutineWorkflow.AddFirst("ShowLoadRumtimeScriptsError", ShowLoadRumtimeScriptsError);
            }
            else
            {
                var assetBundleRequest = www.assetBundle.LoadAssetAsync("Assets/Resources/RuntimeScripts", typeof(UnityEngine.Object));
                yield return assetBundleRequest;

                try
                {
                    var assembly = System.Reflection.Assembly.Load((assetBundleRequest.asset as TextAsset).bytes);
                    var logicModuleLoaderType = assembly.GetType("GameLogic.LogicCore.LogicModuleLoader");
                    if (logicModuleLoaderType != null)
                    {
                        GameCore.Instance.gameObject.AddComponent(logicModuleLoaderType);
                    }
                    else
                    {
                        Debug.LogError("ScriptsLoadModule - logicModuleLoaderType is null");
                        m_CoroutineWorkflow.AddFirst("ShowLoadRumtimeScriptsError", ShowLoadRumtimeScriptsError);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("criptsLoadModule - load runtime scripts error : "+e);
                    m_CoroutineWorkflow.AddFirst("ShowLoadRumtimeScriptsError", ShowLoadRumtimeScriptsError);
                }
            }
        }

        protected IEnumerator ShowLoadRumtimeScriptsError()
        {
            yield return GameCore.Instance.StartCoroutine(LoadingUI.Instance.
                        ShowConfirmPanel(LoadingLanguageData.Instance.GetString(21),
                            LoadingLanguageData.Instance.GetString(10),
                            () =>
                            {
                                GameUtility.QuitGame();
                            },
                            LoadingLanguageData.Instance.GetString(9),
                            () =>
                            {
                                m_CoroutineWorkflow.AddFirst("LoadLogicModuleLoader", LoadLogicModuleLoader);
                            }));
        }
    }
}
