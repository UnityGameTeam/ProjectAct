using System.Collections;
using GameLogic.Components;
using UGCore;

namespace GameLogic.LogicModules
{
    public class SoundManageModule : GameModule
    {
        public override IEnumerator LoadModuleAsync()
        {
            var uiSoundService = new UISoundService(gameObject);
            var bgSoundService = new BgSoundService(gameObject);
            var fxSoundService = new FxSoundService(gameObject);

            //本地存储决定可不可以播放
            SoundManager.Instance.AddSoundServie((int)SoundServiceId.UISound, uiSoundService);
            SoundManager.Instance.AddSoundServie((int)SoundServiceId.BGSound, bgSoundService);
            SoundManager.Instance.AddSoundServie((int)SoundServiceId.FxSound, fxSoundService);
            yield break;
        }

        protected void Update()
        {
            SoundManager.Instance.Update();
        }
    }
}
