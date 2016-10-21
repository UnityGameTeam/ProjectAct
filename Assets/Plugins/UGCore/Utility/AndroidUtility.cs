using System.IO;
using UnityEngine;

namespace UGCore.Utility
{
    public static class AndroidUtility
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaClass  _javaClass;
        private static AndroidJavaClass  _lbsJavaClass;
        private static AndroidJavaObject _mainActivityObj;
        private static AndroidJavaClass  _broadcastReveiverMgrJavaClass;
        private static AndroidJavaClass  _deviceInfoJavaClass;

        static AndroidUtility()
        {
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
            _mainActivityObj = jo;
            _javaClass = new AndroidJavaClass("com.uasdk.utility.AndroidUtility");
            _javaClass.CallStatic("setContext", jo);

            _lbsJavaClass = new AndroidJavaClass("com.uasdk.lbs.LocationHelper");
            _lbsJavaClass.CallStatic("setContext", jo);

            _broadcastReveiverMgrJavaClass = new AndroidJavaClass("com.uasdk.broadcast.BroadcastReceiverManager");
            _deviceInfoJavaClass = new AndroidJavaClass("com.uasdk.model.DeviceInfo");
        }
#endif

        public static void KeepScreenNeverSleep()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                _javaClass.CallStatic("keepScreenOn");
#endif
        }

        public static void RestartGame()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                _javaClass.CallStatic("restartGame");
#endif
        }

        public static void QuitGame()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                _javaClass.CallStatic("quitGame");
#endif
        }

        public static void GotoNetworkSetting()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                _javaClass.CallStatic("gotoNetworkSetting");
#endif
        }

        public static void InstallApk(string apkPath,bool exitGame = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                _javaClass.CallStatic("installApk", apkPath, exitGame);
#endif
        }

        /// <summary>
        /// 使用地图定位请求定位信息
        /// </summary>
        /// <param name="useCache">单次定位不推荐使用缓存</param>
        /// <param name="callbackInterval">定位回调周期</param>
        public static void RequestLocation(bool useCache = false, int callbackInterval = 0)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _lbsJavaClass.CallStatic("requestLocation", useCache, callbackInterval);
#endif
        }

        public static void StopRequestLocation()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _lbsJavaClass.CallStatic("stopRequestLocation");
#endif
        }

        public static void ShowExitDialog(string title, string content, string okText, string cancelText)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("showExitDialog", title, content, okText, cancelText);
#endif
        }

        public static void HideExitDialog()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("hideExitDialog");
#endif           
        }

        public static string GetPackageName()
        {
            return Directory.GetParent(Application.persistentDataPath).Name;
        }

        /// <summary>
        /// 打开二维码扫描
        /// </summary>
        /// <returns></returns>
        public static void StartQrCodeScanActivity()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("startQrCodeScanActivity");
#endif
        }

        /// <summary>
        /// 设置打开二维码扫描是相机初始化失败的弹出提示，比如
        /// title = "系统提示"
        /// content = "在设置-应用-{这里填应用或游戏的名称}-权限中开启相机权限，以正常使用扫一扫功能"
        /// okText = "确定"
        /// </summary>
        public static void SetQrCodeScanErrorInfo(string title, string content, string okText)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("setQrCodeScanErrorInfo",title,content,okText);
#endif
        }

        /// <summary>
        /// 注册网络变化接收器
        /// </summary>
        public static void RegisterNetworkChangedReceiver()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _broadcastReveiverMgrJavaClass.CallStatic("registerNetworkChangedReceiver", _mainActivityObj);
#endif
        }

        /// <summary>
        /// 取消注册的网络变化接收器
        /// </summary>
        public static void UnregisterNetworkChangedReceiver()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _broadcastReveiverMgrJavaClass.CallStatic("unregisterNetworkChangedReceiver", _mainActivityObj);
#endif
        }

        /// <summary>
        /// 拍照或者选择相册来获取照片
        /// </summary>
        /// <param name="type">takePhoto 为启动拍照，其他目前为选择照片</param>
        /// <param name="width">最终要裁剪的图片宽度</param>
        /// <param name="height">最终要裁剪的图片高度</param>
        /// <param name="savePath">裁剪后图片的保存路径</param>
        /// <param name="fileName">保存的文件名(image.jpg)</param>
        public static void TakePhoto(string type, int width, int height, string savePath, string fileName)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("takePhoto", type, width, height, savePath, fileName);
#endif 
        }

        /// <summary>
        /// 搜索指定经纬度为中心searchRadius为半径的圆的范围内的兴趣点
        /// 这里使用的是腾讯地图的Poi搜索，腾讯地图的poi搜索必须有关键字
        /// 不能使用没有关键字来搜索周围所有的poi(高德地图可以)
        /// 建议的做法，使用"路"作为关键字一般可以搜索大部分的poi
        /// 还可以提供一个输入关键字(比如银行)来搜索poi的功能，用于较精确
        /// 搜索周围的poi相关信息（比如地址，经纬度等）
        /// </summary>
        /// <param name="latitude">纬度</param>
        /// <param name="longitude">经度</param>
        /// <param name="keyword">关键字</param>
        /// <param name="pageIndex">搜索结果页码</param>
        /// <param name="pageSize">搜索结果单页数量</param>
        /// <param name="searchRadius">搜索范围默认2000米半径的圆内</param>
        public static void GetPoiSearch(float latitude, float longitude, string keyword, int pageIndex = 1, int pageSize = 10, int searchRadius = 2000)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("getPoiSearch", latitude, longitude, keyword, pageIndex, pageSize, searchRadius);
#endif
        }

        /// <summary>
        /// 初始化腾讯bugly
        /// </summary>
        /// <param name="appId">申请的应用id</param>
        /// <param name="debugMode">是否是调试模式，如果是则有更详细的信息</param>
        public static void InitTencentBugly(string appId, bool debugMode = false)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("initTencentBugly", appId, debugMode);
#endif
        }

        public static string GetDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getDevice");
#else
            return "";
#endif
        }

        //设备型号
        public static string GetDeviceModel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getDeviceModel");
#else
            return "";
#endif
        }

        //Sdk版本，比如安卓的4.4版本的数字字符串
        public static string GetSdkVersion()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getSdkVersion");
#else
            return "";
#endif
        }

        //Sdk的Api等级 比如19
        public static int GetApiLevel()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<int>("getApiLevel");
#else
            return -1;
#endif
        }

        public static string GetManufacturer()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getManufacturer");
#else
            return "";
#endif
        }

        public static string GetProduct()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getProduct");
#else
            return "";
#endif
        }

        public static string GetTelephoneNumber()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getTelephoneNumber",_mainActivityObj);
#else
            return "";
#endif
        }

        public static string GetSimOperator()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getSimOperator",_mainActivityObj);
#else
            return "";
#endif
        }

        /// <summary>
        /// 得到sim卡的运营商
        /// </summary>
        /// <returns></returns>
        public static string GetSimProvider()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getSimProvider",_mainActivityObj);
#else
            return "";
#endif
        }

        /// <summary>
        /// 得到网络类型
        /// </summary>
        /// <returns></returns>
        public static string GetCurrentNetworkType()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _deviceInfoJavaClass.CallStatic<string>("getCurrentNetworkType",_mainActivityObj);
#else
            return "";
#endif
        }

        public static void HideEditDialog()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("hideEditDialog");
#endif
        }

        public static bool EditDialogIsShowing()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return _javaClass.CallStatic<bool>("editDialogIsShowing");
#else
            return false;
#endif
        }

        public static void SetEditText(string text, string hintText)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("setEditText",text,hintText);
#endif
        }

        public static void ShowEditDialog(string text, string hintText, int inputFlag, int inputMode,
            int inputReturn, string titleText, string buttonCancelText, string buttonOkText)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _javaClass.CallStatic("showEditDialog",text,hintText,inputFlag,inputMode,inputReturn,titleText,buttonCancelText,buttonOkText);
#endif
        }

    }
}
 