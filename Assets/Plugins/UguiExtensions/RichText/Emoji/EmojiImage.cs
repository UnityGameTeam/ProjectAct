using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace UguiExtensions
{
    [Serializable]
    public struct EmojiConfig
    {
        public EmojiImage target;
        public List<EmojiInfo> emojiList;
    }

    /// <summary>
    /// EmojiImage用于专门显示表情图片
    /// </summary>
    public class EmojiImage : MaskableGraphic
    {
        [NonSerialized] protected static Mesh                s_ImageMesh;
        [NonSerialized] private static readonly VertexHelper s_ImageVertexHelper = new VertexHelper();

        [NonSerialized] protected List<EmojiInfo>    m_EmojiList;
        [NonSerialized] protected List<UIVertex>       m_Vertices;
        [NonSerialized] protected List<AnimationEmoji> m_AnimitonEmojiList;

        public List<EmojiInfo> EmojiList
        {
            get { return m_EmojiList; }
            set { m_EmojiList = value; }
        }

        public List<UIVertex> Vertices
        {
            get { return m_Vertices ?? (m_Vertices = new List<UIVertex>(16)); }
        }

        public List<AnimationEmoji> AnimitonEmojiList
        {
            get { return m_AnimitonEmojiList ?? (m_AnimitonEmojiList = new List<AnimationEmoji>(8)); }
        }

        protected static Mesh ImageMesh
        {
            get
            {
                if (s_ImageMesh == null)
                {
                    s_ImageMesh = new Mesh();
                    s_ImageMesh.name = "EmojiImage UI Mesh";
                    s_ImageMesh.hideFlags = HideFlags.HideAndDontSave;
                }
                return s_ImageMesh;
            }
        }

        public void Clear()
        {
            if (m_Vertices != null)
            {
                m_Vertices.Clear();
            }

            if (m_AnimitonEmojiList != null)
            {
                m_AnimitonEmojiList.Clear();
            }
        }

        public void ResetRectTransform(RectTransform ownerRectTransform)
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(1, 1);

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = ownerRectTransform.pivot;
        }

        public void MarkVerticesDirty()
        {
            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                UpdateGeometry();
            }
            else
            {
                SetVerticesDirty();
            }
        }

        public void UpdateAnimationEmoji()
        {
            for (int i = 0, count = AnimitonEmojiList.Count; i < count; ++i)
            {
                var animationEmoji = AnimitonEmojiList[i];
                var emoji = EmojiList[animationEmoji.emojiIndex];
                var delta = animationEmoji.timeDelta + Time.deltaTime;
                float rate = emoji.frameRate > 0 ? 1f/emoji.frameRate : 0;

                if (delta > rate)
                {
                    delta = (rate > 0f) ? delta - rate : 0f;
                    var emojiPlayIndex = animationEmoji.playEmojiIndex;
                    ++emojiPlayIndex;

                    if (emojiPlayIndex >= emoji.spriteList.Count)
                    {
                        emojiPlayIndex = 0;
                    }

                    var uv = DataUtility.GetOuterUV(emoji.spriteList[emojiPlayIndex]);

                    var vertex1Index = animationEmoji.vertextStartIndex;
                    var vertex1 = Vertices[vertex1Index];
                    vertex1.uv1 = new Vector2(uv.x, uv.y);
                    ;
                    Vertices[vertex1Index] = vertex1;

                    vertex1Index = animationEmoji.vertextStartIndex + 1;
                    var vertex2 = Vertices[vertex1Index];
                    vertex2.uv1 = new Vector2(uv.x, uv.w);
                    Vertices[vertex1Index] = vertex2;

                    vertex1Index = animationEmoji.vertextStartIndex + 2;
                    var vertex3 = Vertices[vertex1Index];
                    vertex3.uv1 = new Vector2(uv.z, uv.w);
                    Vertices[vertex1Index] = vertex3;

                    vertex1Index = animationEmoji.vertextStartIndex + 3;
                    var vertex4 = Vertices[vertex1Index];
                    vertex4.uv1 = new Vector2(uv.z, uv.y);
                    Vertices[vertex1Index] = vertex4;

                    animationEmoji.playEmojiIndex = emojiPlayIndex;
                }

                animationEmoji.timeDelta = delta;
                AnimitonEmojiList[i] = animationEmoji;
            }

            OnPopulateMeshImmediately(s_ImageVertexHelper);
            s_ImageVertexHelper.FillMesh(ImageMesh);
            canvasRenderer.SetMesh(ImageMesh);
        }

        protected override void UpdateGeometry()
        {
            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                OnPopulateMeshImmediately(s_ImageVertexHelper);
                s_ImageVertexHelper.FillMesh(ImageMesh);
                canvasRenderer.SetMesh(ImageMesh);
            }
            else
            {
                base.UpdateGeometry();
            }
        }

        private readonly UIVertex[] m_TempVerts = new UIVertex[4];

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            toFill.Clear();
            if (Vertices == null || Vertices.Count == 0)
            {
                return;
            }

            for (int i = 0; i < Vertices.Count; i++)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = Vertices[i];
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }

        protected void OnPopulateMeshImmediately(VertexHelper toFill)
        {
            toFill.Clear();
            if (Vertices == null || Vertices.Count == 0)
            {
                return;
            }

            for (int i = 0; i < Vertices.Count; i++)
            {
                int tempVertsIndex = i & 3;
                m_TempVerts[tempVertsIndex] = Vertices[i];
                if (tempVertsIndex == 3)
                    toFill.AddUIVertexQuad(m_TempVerts);
            }
        }
    }
}