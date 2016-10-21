//******************************
//
// 模块名   : IItemView
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : AbstractListView的Item实现接口
//
//******************************
using UnityEngine;

namespace UguiExtensions
{
    public interface IItemView
    {
        int ViewType { get; }
        RectTransform ItemRectTransform { get; set; }
    }
}