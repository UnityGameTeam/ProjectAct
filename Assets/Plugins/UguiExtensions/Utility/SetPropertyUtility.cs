//******************************
//
// 模块名   : SetPropertyUtility
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : SetPropertyUtility拷贝自Ugui源码，由于Ugui某些接口不暴露，在不修改源码情况下拷贝Ugui代码
//
//******************************
using UnityEngine;

namespace UguiExtensions
{
    public static class SetPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
        {
            if (currentValue.Equals(newValue))
                return false;

            currentValue = newValue;
            return true;
        }

        public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;

            currentValue = newValue;
            return true;
        }
    }
}
