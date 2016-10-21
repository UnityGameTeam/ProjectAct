using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Pathfinding.Serialization.JsonFx;
using UGCore;

public class VoiceInfo
{
    public string format;
    public int rate;
    public int channel;
    public string token;
    public string cuid;
    public int len;
    public string speech;
}

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

            var result = JsonReader.Deserialize<Dictionary<string, string>>(www.text);
            Debug.LogError(result["access_token"]);

/*            var wwwForm = new WWWForm();
            wwwForm.AddField("enctype", "application/json");*/

            /*            VoiceInfo vi = new VoiceInfo();
                        vi.format = "opus";
                        vi.rate = 16000;
                        vi.channel = 1;
                        vi.token = result["access_token"];
                        vi.cuid = "baiduvoiceafwewfwfwfqw";


                        vi.len = data.Length;
                        vi.speech = Convert.ToBase64String(data);
                        var json = JsonWriter.Serialize(vi);*/
            byte[] data = File.ReadAllBytes("C:\\Users\\11472\\Desktop\\ttt.spx");
            string getTextUrl = "http://vop.baidu.com/server_api?lan=" + "zh" + "&cuid=" + "12434fewewwgrfeffewfssewwe" + "&token=" + result["access_token"];
            HttpWebRequest getTextRequst = WebRequest.Create(getTextUrl) as HttpWebRequest;
            getTextRequst.Method = "POST"; //这里必须大写，坑
            getTextRequst.ContentType = "audio/speex;rate=8000";
           getTextRequst.ContentLength = data.Length;

                getTextRequst.Accept = "*/*";
            getTextRequst.KeepAlive = true;
            getTextRequst.Timeout = 30000;//30秒连接不成功就中断 

            using (Stream writeStream = getTextRequst.GetRequestStream())
            {
                writeStream.Write(data, 0, data.Length);
            }

         

            HttpWebResponse getTextResponse = getTextRequst.GetResponse() as HttpWebResponse;
            using (StreamReader strHttpText = new StreamReader(getTextResponse.GetResponseStream(), Encoding.UTF8))
            {
                Debug.LogError(strHttpText.ReadToEnd());
            }


            //  byte[] data = File.ReadAllBytes("C:\\Users\\11472\\Desktop\\test.opus");
            //   vi.len =  data.Length;
            //  vi.speech = Convert.ToBase64String(data);
            //     var json = JsonWriter.Serialize(vi);
            //    wwwForm.AddField("Content-length", json.Length);

            /* WWWForm wwwForm = new WWWForm();
             wwwForm

             string getTextUrl = "http://vop.baidu.com/server_api?lan=" + "zh" + "&cuid=" + "12434fewewwewwe" + "&token=" + result["access_token"];
             www = new WWW("http://vop.baidu.com/server_api", wwwForm);

             yield return www;
             Debug.LogError(www.text);*/
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