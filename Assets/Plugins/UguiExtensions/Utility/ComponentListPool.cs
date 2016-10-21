//******************************
//
// 模块名   : ComponentListPool
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : ComponentListPool拷贝自Ugui源码，由于Ugui某些接口不暴露，在不修改源码情况下拷贝Ugui代码
//
//******************************
using UnityEngine;
using System.Collections.Generic;

namespace UguiExtensions
{
    public class ComponentListPool : MonoBehaviour
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool<List<Component>> s_ComponentListPool = new ObjectPool<List<Component>>(null, l => l.Clear());

        public static List<Component> Get()
        {
            return s_ComponentListPool.Get();
        }

        public static void Release(List<Component> toRelease)
        {
            s_ComponentListPool.Release(toRelease);
        }
    }
}
