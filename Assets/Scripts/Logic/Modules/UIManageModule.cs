using System.Collections;
using Game.UI;
using GameLogic.Components;
using UguiExtensions;
using UGCore;
using UGCore.Utility;
using UnityEngine;

namespace GameLogic.LogicModules
{
    public class UIManageModule : GameModule
    {
        public GameModule OwnerModule { get { return this; } }

        public override IEnumerator LoadModuleAsync()
        {
            //配置半屏聊天处理
            PlatformMessageManageModule platformMsgMgr = ModuleManager.Instance.GetGameModule(typeof (PlatformMessageManageModule).Name) as PlatformMessageManageModule;
            platformMsgMgr.AddPlatformMessageListener(PlatformMessageManageModule.HalfScreen_CommitInput, HalfScreen_CommitInput);
            platformMsgMgr.AddPlatformMessageListener(PlatformMessageManageModule.HalfScreen_DialogHide, HalfScreen_DialogHide);

            bool loadDone = false;
            AssetManager.Instance.LoadAssetAsync("UI/UIRoot", (obj) =>
            {
                var uiRootOjb = Object.Instantiate(obj) as GameObject;
                uiRootOjb.transform.localEulerAngles = Vector3.zero;
                uiRootOjb.transform.localRotation = Quaternion.identity;
                uiRootOjb.transform.localScale = Vector3.one;
                Object.DontDestroyOnLoad(uiRootOjb);

                var canvasObj = uiRootOjb.transform.FindChild("Canvas");
                UIManager.Initialize(canvasObj);
                loadDone = true;
            });

            while (!loadDone)
            {
                yield return null;
            }

            //ui框架改成接口不用属性重载
            var loadingUIController = UIManager.Instance.OpenUI(typeof(LoadingUIController).Name, null,false);
            while (!loadingUIController.UILoadDone)
            {
                yield return null;
            }

            var messageUIController = UIManager.Instance.OpenUI(typeof(MessageUIController).Name, null, false);
            while (!messageUIController.UILoadDone)
            {
                yield return null;
            }
        }

        protected void HalfScreen_CommitInput(string text)
        {
            if (InputFieldEx.CurrentActiveInputFieldFx != null)
            {
                InputFieldEx.CurrentActiveInputFieldFx.text = text;
                InputFieldEx.CurrentActiveInputFieldFx.onEndEdit.Invoke(text);
                InputFieldEx.CurrentActiveInputFieldFx = null;
            }
        }

        protected void HalfScreen_DialogHide(string text)
        {
            if (InputFieldEx.CurrentActiveInputFieldFx != null)
            {
                InputFieldEx.CurrentActiveInputFieldFx.OnDeselect(null);
                InputFieldEx.CurrentActiveInputFieldFx.text = text;
                InputFieldEx.CurrentActiveInputFieldFx = null;
            }
        }

        protected override void OnApplicationPause()
        {
            base.OnApplicationPause();
            AndroidUtility.HideEditDialog();
            AndroidUtility.ImmersiveHideEditDialog();
        }

        protected void Update()
        {
            UIManager.Instance.Update();
        }

        protected void LateUpdate()
        {
            UIManager.Instance.LateUpdate();
        }        
    }
}