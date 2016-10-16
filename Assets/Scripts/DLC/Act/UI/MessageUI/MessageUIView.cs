using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using GameLogic.Components;
using UguiExtensions;

namespace Game.UI
{
    class MessageItem
    {
        public static float ItemShowTime = 2f;
        public static float FadeOutTime = 1f;

        private NmImage    itemBg;
        private NmText     itemText;
        private GameObject itemRoot;
        private Queue<MessageItem> messageQueue;

        public MessageItem(GameObject itemRootGo,Queue<MessageItem> msgQueue)
        {
            messageQueue = msgQueue;
            itemRoot = itemRootGo;
            var itemRootTransform = itemRoot.transform;
            itemText = itemRootTransform.FindChild("Text").GetComponent<NmText>();
            itemBg   = itemRootTransform.GetComponent<NmImage>();
        }

        public void ShowMsg(string msg)
        {
            itemText.text = msg;

            var textColor = itemText.color;
            textColor.a = 1;
            itemText.color = textColor;

            var bgColor = itemBg.color;
            bgColor.a = 1;
            itemBg.color   = bgColor;
            itemRoot.transform.SetAsLastSibling();
            itemRoot.SetActive(true);

            textColor.a = 0;
            var tweener = itemText.DOColor(textColor, FadeOutTime);
            tweener.SetLoops(1);
            tweener.SetDelay(ItemShowTime);

            bgColor.a = 0;
            tweener = itemBg.DOColor(bgColor, FadeOutTime);
            tweener.SetLoops(1);
            tweener.OnComplete(FodeOutComplete);
            tweener.SetDelay(ItemShowTime);
        }

        private void FodeOutComplete()
        {
            itemRoot.SetActive(false);
            messageQueue.Enqueue(this);
        }
    }

    public class MessageUIView : UIView
    {
        public override string LayerName { get { return UILayer.TopLayer.ToString(); } }

        public override string PrefabPath
        {
            get { return "DLC/Act/UI/MessageUI/MessageUI"; }
        }

        private GameObject         m_OriginalMessageItem;
        private Queue<MessageItem> m_MessageItemQueue = new Queue<MessageItem>();

        protected override void InitUI()
        {
            m_OriginalMessageItem = m_UIRootObj.transform.FindChild("View/Item").gameObject;
            m_MessageItemQueue.Enqueue(new MessageItem(m_OriginalMessageItem, m_MessageItemQueue));
        }

        protected override void ProcessIntent(Bundle bundle)
        {
            base.ProcessIntent(bundle);
            var msg = bundle.GetString("ShowMsg", "");
            if (!string.IsNullOrEmpty(msg))
            {
                if (m_MessageItemQueue.Count == 0)
                {
                    var item = Object.Instantiate(m_OriginalMessageItem) as GameObject;
                    item.transform.SetParent(m_OriginalMessageItem.transform.parent);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                    item.transform.localScale = Vector3.one;
                    m_MessageItemQueue.Enqueue(new MessageItem(item, m_MessageItemQueue));
                }
                m_MessageItemQueue.Dequeue().ShowMsg(msg);
            }

            Bundle.CacheBundle(bundle);
        }
    }
}

