using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.Serialization.JsonFx;
using UGCore;

namespace GameLogic.Components
{
    public class HttpManager : GameModule
    {
        private static HttpManager _instance;
        public static HttpManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ModuleManager.Instance.AddGameModule(typeof (HttpManager)) as HttpManager;
                }
                return _instance;
            }
        }

        /// <summary>
        /// 这里应该通过网络配置传输
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetBaiduVoiceAccessToken()
        {
            var www =
                new WWW(
                    "https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id=qgOC3snLblrignUTxHda2LFq&client_secret=31513db7b64a38f5384b9d8c294b0b8a");
            yield return www;

            Debug.LogError(www.text);
            var result = JsonReader.Deserialize<Dictionary<string, string>>(www.text);
            Debug.LogError(result["access_token"]);
        }

        //测试代码，后期完善
        public IEnumerator UploadVoiceFile()
        {
            var www =
                new WWW(
                    "https://openapi.baidu.com/oauth/2.0/token?grant_type=client_credentials&client_id=qgOC3snLblrignUTxHda2LFq&client_secret=31513db7b64a38f5384b9d8c294b0b8a");
            yield return www;

            Debug.LogError(www.error);
            var result = JsonReader.Deserialize<Dictionary<string, string>>(www.text);
            Debug.LogError(result["access_token"]);

            var wwwForm = new WWWForm();
            wwwForm.AddField("format", "speex");
            wwwForm.AddField("rate", "8000");
            wwwForm.AddField("channel", "1");
            wwwForm.AddField("token", result["access_token"]);
            wwwForm.AddField("cuid", "baiduvoice");
            wwwForm.AddField("len", "4096");
            wwwForm.AddField("speech", "qwerqwrwqwqrwqrwfearsgeareareqqwerqweq3");
            www = new WWW("http://vop.baidu.com/server_api", wwwForm);
            yield return www;
            Debug.LogError(www.text);
        }

        //测试代码，后期完善
        public IEnumerator UploadImageFile()
        {
            var wwwForm = new WWWForm();
            wwwForm.AddField("enctype", "multipart/form-data");
            wwwForm.AddBinaryData("uploadfile",null);
            var www = new WWW("http://vop.baidu.com/server_api", wwwForm);
            yield return www;
        }
    }
}