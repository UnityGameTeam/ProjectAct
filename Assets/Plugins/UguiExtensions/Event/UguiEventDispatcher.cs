//******************************
//
// 模块名   : UguiEventDispatcher
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : 向上派发Drag相关事件
//
//******************************

using UnityEngine.EventSystems;

namespace UguiExtensions.Event
{
    /// <summary>
    /// 当某些控件的设计捕获了Drag相关事件的时候，如果实际不想处理相关Drag事件，但是又和ListView
    /// 的事件冲突的时候，导致ListView无法处理Drag相关事件的时候，可以在相应控件上绑定该类，来向
    /// 上派发Drag事件
    /// </summary>
    public class UguiEventDispatcher : UIBehaviour,IInitializePotentialDragHandler,IDragHandler
    {
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.pointerDrag = ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData,
                ExecuteEvents.initializePotentialDrag);
        }

        public void OnDrag(PointerEventData eventData)
        {

        }
    }
}
