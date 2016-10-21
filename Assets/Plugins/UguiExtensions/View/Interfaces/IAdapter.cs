//******************************
//
// 模块名   : IAdapter
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : AbstractListView的适配器接口
//
//******************************

using System.Collections.Generic;

namespace UguiExtensions
{
    public interface IAdapter
    {
        AbstractListView Owner { get; set; }

        int GetCount();

        void ProcessView(int position, IItemView convertView, AbstractListView parent);

        int GetItemViewType(int position);

        int GetViewTypeCount();

        IItemView GetView(int position,AbstractListView parent);

        bool IsEmpty();

        /// <summary>
        /// 刷新当前所有的Item，从新开始从AbstractListView的第一个数据开始布局
        /// </summary>
        void RefreshAllItem();

        /// <summary>
        /// 刷新当前所有显示的Item，如果当前显示的Item在数据中的位置大于GetCount的数量，则从
        /// 数据的最后一个向前布局
        /// </summary>
        void RefreshCurrentItem();

        /// <summary>
        /// 用于ListView设置SetAdapter的时候可以预加载一些Item,如果不需要预加载，返回Null
        /// 这里的预加载一般都是直接生成多个对象，这样可能导致卡帧，实际中还可以单独生成一个
        /// 缓存池，再GetView或者PreloadItem的时候从缓存池中获取Item，这样缓存池的Item可以
        /// 在别的地方定时或者逐帧生成Item等
        /// 特别需要注意的是，如果使用的是ListView，那么在别的缓存池中拿出的时候，需要设置Item的
        /// 布局完成回调 比如：
        /// item.wrapChildrenFitter.layoutGroupComplete.AddListener(parent.ChildrenLayoutGroupComplete);
        /// </summary>
        List<IItemView> PreloadItem(AbstractListView parent);
    }
}
