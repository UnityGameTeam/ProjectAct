//******************************
//
// 模块名   : LayoutRebuilderUtility
// 开发者   : 曾德烺
// 开发日期 : 2016-4-5
// 模块描述 : 布局工具类，用于处理Ugui布局相关的处理
//
//******************************
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UguiExtensions
{
    internal static class LayoutRebuilderUtility 
    {
        private struct LayoutRebuilder : ICanvasElement
        {
            private readonly RectTransform m_ToRebuild;
            private readonly int m_CachedHashFromTransform;

            public LayoutRebuilder(RectTransform controller)
            {
                m_ToRebuild = controller;
                m_CachedHashFromTransform = m_ToRebuild.GetHashCode();
            }

            public Transform transform { get { return m_ToRebuild; } }

            public void GraphicUpdateComplete() { }

            public bool IsDestroyed() { return m_ToRebuild == null; }

            void ICanvasElement.Rebuild(CanvasUpdate executing) { }
            public void LayoutComplete() { }

            public override int GetHashCode()
            {
                return m_CachedHashFromTransform;
            }
        }

        public static ICanvasElement GetLayoutRebuilder(RectTransform rectTransform)
        {
            return new LayoutRebuilder(rectTransform);
        }

        public static bool IsRectTransformLayoutRebuild(RectTransform rectTransform)
        {
            return !CanvasUpdateRegistry.TryRegisterCanvasElementForLayoutRebuild(LayoutRebuilderUtility.GetLayoutRebuilder(rectTransform));
        }

        public static void ForceRebuildLayoutImmediate(RectTransform rectTransform)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }

        public static void MarkLayoutForImmediateRebuild(RectTransform rect)
        {
            if (rect == null)
                return;

            RectTransform layoutRoot = rect;
            while (true)
            {
                var parent = layoutRoot.parent as RectTransform;
                if (!ValidLayoutGroup(parent))
                    break;
                layoutRoot = parent;
            }

            // We know the layout root is valid if it's not the same as the rect,
            // since we checked that above. But if they're the same we still need to check.
            if (layoutRoot == rect && !ValidController(layoutRoot))
                return;

            ForceRebuildLayoutImmediate(layoutRoot);
        }

        private static bool ValidController(RectTransform layoutRoot)
        {
            if (layoutRoot == null)
                return false;

            var comps = ComponentListPool.Get();
            layoutRoot.GetComponents(typeof(ILayoutController), comps);
            StripDisabledBehavioursFromList(comps);
            var valid = comps.Count > 0;
            ComponentListPool.Release(comps);
            return valid;
        }

        private static bool ValidLayoutGroup(RectTransform parent)
        {
            if (parent == null)
                return false;
            var comps = ComponentListPool.Get();
            parent.GetComponents(typeof(ILayoutGroup), comps);
            StripDisabledBehavioursFromList(comps);
            var validCount = comps.Count > 0;
            ComponentListPool.Release(comps);
            return validCount;
        }

        static void StripDisabledBehavioursFromList(List<Component> components)
        {
            components.RemoveAll(e => e is Behaviour && (!(e as Behaviour).enabled || !(e as Behaviour).isActiveAndEnabled));
        }
    }
}
