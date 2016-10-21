using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UguiExtensions
{
    [Serializable]
    public struct EmojiInfo         
    {
        public int width;
        public int height;
        public int paddingLeft;
        public int paddingRight;
        public int paddingTop;
        public int paddingBottom;
        public bool animation;
        public int frameRate;
        public List<Sprite> spriteList;

        public int GetWidth()
        {
            return width + paddingLeft + paddingRight;
        }

        public int GetHeight()
        {
            return height + paddingTop + paddingBottom;
        }
    }

    public struct UrlRegion
    {
        public string url;
        public int verticeIndex;
        public int urlIndex;
        public Vector2 bottomLeft;
        public Vector2 topRight;
    }

    public struct AnimationEmoji
    {
        public int emojiIndex;
        public float timeDelta;
        public int playEmojiIndex;
        public int vertextStartIndex;
    }

    /// <summary>
    /// 
    /// 1、目前支持5.4版本，去除对4.x版本的支持，精简代码量，其他5.x版本未测试
    /// 2、动画表情除了需要配置表情信息支持动画外，还需要添加
    /// 3、TextEx不同通过继承Text来实现的，原因是Ugui的Text的FontTextureChanged无法重载，导致无法调用自定义TextGeneratorEx的处理
    ///
    /// 不足：
    /// 1、使用步骤还是多了点，没有添加扩展表情库的功能
    /// 
    /// </summary>
    [AddComponentMenu("UI/TextEx", 11)]
    public class TextEx : MaskableGraphic, ILayoutElement, IPointerClickHandler
    {
        [Serializable]
        public class UrlClickEvent : UnityEvent<string> { }

        [SerializeField]
        private FontData m_FontData = FontData.defaultFontData;

#if UNITY_EDITOR
        // needed to track font changes from the inspector
        private Font m_LastTrackedFont;
#endif

        [TextArea(3, 10)][SerializeField] protected string m_Text = String.Empty;

        private TextGeneratorEx m_TextCache;
        private TextGeneratorEx m_TextCacheForLayout;

        static protected Material s_DefaultText = null;

        private AnimationEmojiPlay _mAnimationEmojiPlay;
        protected List<UrlRegion> m_UrlRegionList;
 
        [SerializeField] protected bool  m_ColorInfluenceEmoji;
        [SerializeField] protected bool  m_ParseEmoji = true;
        [SerializeField] protected bool  m_ParseColor = true;
        [SerializeField] protected bool  m_ParseBold = true;
        [SerializeField] protected bool  m_ParseItatic = true;
        [SerializeField] protected bool  m_ParseUnderline = true;
        [SerializeField] protected bool  m_ParseStrikethrough = true;
        [SerializeField] protected bool  m_ParseUrl = true;
        [SerializeField] protected bool  m_ParseSub = true;
        [SerializeField] protected bool  m_ParseSup = true;
        [SerializeField] protected bool  m_ParseSize = true;
        [SerializeField] protected float m_SpacingX;
        [SerializeField] protected bool  m_EllipsizeEnd; //如果文本无法显示完全，是否在最后显示3个省略号
        [SerializeField] protected UrlClickEvent m_UrlClickEvent = new UrlClickEvent();
        [SerializeField] protected List<EmojiConfig> m_EmojiConfigList;

        protected TextEx()
        {
            useLegacyMeshGeneration = false;
        }

        public bool ColorInfluenceEmoji
        {
            get { return m_ColorInfluenceEmoji; }
            set
            {
                if (m_ColorInfluenceEmoji != value)
                {
                    m_ColorInfluenceEmoji = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseEmoji
        {
            get { return m_ParseEmoji; }
            set
            {
                if (m_ParseEmoji != value)
                {
                    m_ParseEmoji = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseColor
        {
            get { return m_ParseColor; }
            set
            {
                if (m_ParseColor != value)
                {
                    m_ParseColor = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseBold
        {
            get { return m_ParseBold; }
            set
            {
                if (m_ParseBold != value)
                {
                    m_ParseBold = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseItatic
        {
            get { return m_ParseItatic; }
            set
            {
                if (m_ParseItatic != value)
                {
                    m_ParseItatic = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseUnderline
        {
            get { return m_ParseUnderline; }
            set
            {
                if (m_ParseUnderline != value)
                {
                    m_ParseUnderline = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseStrikethrough
        {
            get { return m_ParseStrikethrough; }
            set
            {
                if (m_ParseStrikethrough != value)
                {
                    m_ParseStrikethrough = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseUrl
        {
            get { return m_ParseUrl; }
            set
            {
                if (m_ParseUrl != value)
                {
                    m_ParseUrl = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseSub
        {
            get { return m_ParseSub; }
            set
            {
                if (m_ParseSub != value)
                {
                    m_ParseSub = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseSup
        {
            get { return m_ParseSup; }
            set
            {
                if (m_ParseSup != value)
                {
                    m_ParseSup = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool ParseSize
        {
            get { return m_ParseSize; }
            set
            {
                if (m_ParseSize != value)
                {
                    m_ParseSize = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public List<EmojiConfig> EmojiConfigList
        {
            get { return m_EmojiConfigList ?? (m_EmojiConfigList = new List<EmojiConfig>(0)); }
            set
            {
                m_EmojiConfigList = value;
                if (_mAnimationEmojiPlay != null)
                {
                    _mAnimationEmojiPlay.enabled = false;
                }
                cachedTextGenerator.Invalidate();
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public float SpacingX
        {
            get { return m_SpacingX; }
            set
            {
                if (m_SpacingX != value)
                {
                    m_SpacingX = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public bool EllipsizeEnd
        {
            get { return m_EllipsizeEnd; }
            set
            {
                if (m_EllipsizeEnd != value)
                {
                    m_EllipsizeEnd = value;
                    cachedTextGenerator.Invalidate();
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        public UrlClickEvent UrlClickNotify
        {
            get
            {
                return m_UrlClickEvent;
            }
            set
            {
                m_UrlClickEvent = value;
            }
        }

        protected List<UrlRegion> UrlRegionList
        {
            get
            {
                if (m_UrlRegionList == null)
                {
                    m_UrlRegionList = new List<UrlRegion>();
                }
                return m_UrlRegionList;
            }
        }

        /// <summary>
        /// Get or set the material used by this Text.
        /// </summary>

        public TextGeneratorEx cachedTextGenerator
        {
            get { return m_TextCache ?? (m_TextCache = m_Text.Length != 0 ? new TextGeneratorEx(m_Text.Length) : new TextGeneratorEx()); }
        }

        public TextGeneratorEx cachedTextGeneratorForLayout
        {
            get { return m_TextCacheForLayout ?? (m_TextCacheForLayout = new TextGeneratorEx()); }
        }

        /// <summary>
        /// Text's texture comes from the font.
        /// </summary>
        public override Texture mainTexture
        {
            get
            {
                if (font != null && font.material != null && font.material.mainTexture != null)
                    return font.material.mainTexture;

                if (m_Material != null)
                    return m_Material.mainTexture;

                return base.mainTexture;
            }
        }

        public void FontTextureChanged()
        {
            // Only invoke if we are not destroyed.
            if (!this)
            {
                FontUpdateTrackerEx.UntrackText(this);
                return;
            }

            cachedTextGenerator.Invalidate();

            if (!IsActive())
                return;

            // this is a bit hacky, but it is currently the
            // cleanest solution....
            // if we detect the font texture has changed and are in a rebuild loop
            // we just regenerate the verts for the new UV's
            if (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
                UpdateGeometry();
            else
                SetAllDirty();
        }

        public Font font
        {
            get
            {
                return m_FontData.font;
            }
            set
            {
                if (m_FontData.font == value)
                    return;

                FontUpdateTrackerEx.UntrackText(this);

                m_FontData.font = value;

                FontUpdateTrackerEx.TrackText(this);

#if UNITY_EDITOR
                // needed to track font changes from the inspector
                m_LastTrackedFont = value;
#endif

                SetAllDirty();
            }
        }

        /// <summary>
        /// Text that's being displayed by the Text.
        /// </summary>

        public virtual string text
        {
            get
            {
                return m_Text;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    if (String.IsNullOrEmpty(m_Text))
                        return;
                    m_Text = "";
                    SetVerticesDirty();
                }
                else if (m_Text != value)
                {
                    m_Text = value;
                    SetVerticesDirty();
                    SetLayoutDirty();
                }
            }
        }

        /// <summary>
        /// Whether this Text will support rich text.
        /// </summary>

        public bool supportRichText
        {
            get
            {
                return m_FontData.richText;
            }
            set
            {
                if (m_FontData.richText == value)
                    return;
                m_FontData.richText = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Wrap mode used by the text.
        /// </summary>

        public bool resizeTextForBestFit
        {
            get
            {
                return m_FontData.bestFit;
            }
            set
            {
                if (m_FontData.bestFit == value)
                    return;
                m_FontData.bestFit = value;
                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public int resizeTextMinSize
        {
            get
            {
                return m_FontData.minSize;
            }
            set
            {
                if (m_FontData.minSize == value)
                    return;
                m_FontData.minSize = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public int resizeTextMaxSize
        {
            get
            {
                return m_FontData.maxSize;
            }
            set
            {
                if (m_FontData.maxSize == value)
                    return;
                m_FontData.maxSize = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        /// <summary>
        /// Alignment anchor used by the text.
        /// </summary>

        public TextAnchor alignment
        {
            get
            {
                return m_FontData.alignment;
            }
            set
            {
                if (m_FontData.alignment == value)
                    return;
                m_FontData.alignment = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public bool alignByGeometry
        {
            get
            {
                return m_FontData.alignByGeometry;
            }
            set
            {
                if (m_FontData.alignByGeometry == value)
                    return;
                m_FontData.alignByGeometry = value;

                SetVerticesDirty();
            }
        }

        public int fontSize
        {
            get
            {
                return m_FontData.fontSize;
            }
            set
            {
                if (m_FontData.fontSize == value)
                    return;
                m_FontData.fontSize = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public HorizontalWrapMode horizontalOverflow
        {
            get
            {
                return m_FontData.horizontalOverflow;
            }
            set
            {
                if (m_FontData.horizontalOverflow == value)
                    return;
                m_FontData.horizontalOverflow = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public VerticalWrapMode verticalOverflow
        {
            get
            {
                return m_FontData.verticalOverflow;
            }
            set
            {
                if (m_FontData.verticalOverflow == value)
                    return;
                m_FontData.verticalOverflow = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public float lineSpacing
        {
            get
            {
                return m_FontData.lineSpacing;
            }
            set
            {
                if (m_FontData.lineSpacing == value)
                    return;
                m_FontData.lineSpacing = value;

                SetVerticesDirty();
                SetLayoutDirty();
            }
        }

        public float pixelsPerUnit
        {
            get
            {
                var localCanvas = canvas;
                if (!localCanvas)
                    return 1;
                // For dynamic fonts, ensure we use one pixel per pixel on the screen.
                if (!font || font.dynamic)
                    return localCanvas.scaleFactor;
                // For non-dynamic fonts, calculate pixels per unit based on specified font size relative to font object's own font size.
                if (m_FontData.fontSize <= 0 || font.fontSize <= 0)
                    return 1;
                return font.fontSize / (float)m_FontData.fontSize;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _mAnimationEmojiPlay = GetComponent<AnimationEmojiPlay>();
            if (_mAnimationEmojiPlay != null)
            {
                _mAnimationEmojiPlay.enabled = false;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            cachedTextGenerator.Invalidate();
            FontUpdateTrackerEx.TrackText(this);
        }

        protected override void OnDisable()
        {
            FontUpdateTrackerEx.UntrackText(this);
            base.OnDisable();

            for (int i = 0, count = EmojiConfigList.Count; i < count; ++i)
            {
                var emojiConfig = m_EmojiConfigList[i];
                if (emojiConfig.target != null)
                {
                    emojiConfig.target.Clear();
                    emojiConfig.target.MarkVerticesDirty();
                }
            }

            if (m_UrlRegionList != null)
            {
                m_UrlRegionList.Clear();
            }
        }

        protected override void UpdateGeometry()
        {
            if (font != null)
            {
                base.UpdateGeometry();
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            AssignDefaultFont();
        }

#endif
        internal void AssignDefaultFont()
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        protected override void OnRectTransformDimensionsChange()
        {
            for (int i = 0, count = EmojiConfigList.Count; i < count; ++i)
            {
                var emojiConfig = m_EmojiConfigList[i];
                if (emojiConfig.target != null)
                {
                    emojiConfig.target.ResetRectTransform(rectTransform);
                }
            }
            base.OnRectTransformDimensionsChange();
        }

        public TextGenerationSettingsEx GetGenerationSettings(Vector2 extents)
        {
            var settings = new TextGenerationSettingsEx();
            settings.generationExtents = extents;
            settings.fontSize = m_FontData.fontSize;

            //只支持动态字体
            settings.resizeTextMinSize = m_FontData.minSize;
            settings.resizeTextMaxSize = m_FontData.maxSize;

            settings.parseBold = m_ParseBold;
            settings.parseColor = m_ParseColor;
            settings.parseEmoji = m_ParseEmoji;
            settings.parseItatic = m_ParseItatic;
            settings.parseStrikethrough = m_ParseStrikethrough;
            settings.parseSub = m_ParseSub;
            settings.parseSup = m_ParseSup;
            settings.parseUnderline = m_ParseUnderline;
            settings.parseUrl = m_ParseUrl;
            settings.parseSize = m_ParseSize;

            settings.textAnchor = m_FontData.alignment;
            settings.scaleFactor = pixelsPerUnit;
            settings.color = color;
            settings.font = font;
            settings.pivot = rectTransform.pivot;
            settings.richText = m_FontData.richText;
            settings.SpacingX = m_SpacingX;
            settings.lineSpacing = m_FontData.lineSpacing;
            settings.fontStyle = m_FontData.fontStyle;
            settings.resizeTextForBestFit = m_FontData.bestFit;
            settings.horizontalOverflow = m_FontData.horizontalOverflow;
            settings.verticalOverflow = m_FontData.verticalOverflow;
            settings.elipsizeEnd = m_EllipsizeEnd;
            return settings;
        }

        static public Vector2 GetTextAnchorPivot(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.LowerLeft: return new Vector2(0, 0);
                case TextAnchor.LowerCenter: return new Vector2(0.5f, 0);
                case TextAnchor.LowerRight: return new Vector2(1, 0);
                case TextAnchor.MiddleLeft: return new Vector2(0, 0.5f);
                case TextAnchor.MiddleCenter: return new Vector2(0.5f, 0.5f);
                case TextAnchor.MiddleRight: return new Vector2(1, 0.5f);
                case TextAnchor.UpperLeft: return new Vector2(0, 1);
                case TextAnchor.UpperCenter: return new Vector2(0.5f, 1);
                case TextAnchor.UpperRight: return new Vector2(1, 1);
                default: return Vector2.zero;
            }
        }

        readonly UIVertex[] m_TempVerts = new UIVertex[4];

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (font == null)
                return;

            Vector2 extents = rectTransform.rect.size;
            var settings = GetGenerationSettings(extents);
            bool newVertices = cachedTextGenerator.Populate(m_Text, settings, m_EmojiConfigList);

            if (m_UrlRegionList != null)
            {
                m_UrlRegionList.Clear();
            }

            Rect inputRect = rectTransform.rect;

            // get the text alignment anchor point for the text in local space
            Vector2 textAnchorPivot = GetTextAnchorPivot(m_FontData.alignment);
            Vector2 refPoint = Vector2.zero;
            refPoint.x = Mathf.Lerp(inputRect.xMin, inputRect.xMax, textAnchorPivot.x);
            refPoint.y = Mathf.Lerp(inputRect.yMin, inputRect.yMax, textAnchorPivot.y);

            // Determine fraction of pixel to offset text mesh.
            Vector2 roundingOffset = PixelAdjustPoint(refPoint) - refPoint;


            for (int i = 0, count = EmojiConfigList.Count; i < count; ++i)
            {
                var emojiConfig = m_EmojiConfigList[i];
                if (emojiConfig.target != null)
                {
                    emojiConfig.target.Clear();
                    emojiConfig.target.EmojiList = emojiConfig.emojiList;
                }
            }

            // Apply the offset to the vertices
            IList<UIVertexEx> verts = cachedTextGenerator.verts;
            float unitsPerPixel = 1/pixelsPerUnit;
            float urlIndex = 0;
            toFill.Clear();
            if (roundingOffset != Vector2.zero)
            {
                for (int i = 0, vertCount = verts.Count; i < vertCount; ++i)
                {
                    UIVertexEx uivex = verts[i];

                    if (uivex.url != null && i >= urlIndex)
                    {
                        uivex.position = uivex.position*unitsPerPixel;
                        uivex.position.x += roundingOffset.x;
                        uivex.position.y += roundingOffset.y;
                        if (UrlRegionList.Count > 0)
                        {
                            var existUrlRegion = UrlRegionList[UrlRegionList.Count - 1];
                            if (existUrlRegion.url == uivex.url && existUrlRegion.urlIndex == uivex.urlIndex &&
                                Mathf.RoundToInt(existUrlRegion.bottomLeft.y) == Mathf.RoundToInt(uivex.position.y))
                            {
                                uivex = verts[i + 2];
                                uivex.position = uivex.position*unitsPerPixel;
                                uivex.position.x += roundingOffset.x;
                                uivex.position.y += roundingOffset.y;
                                existUrlRegion.topRight = uivex.position;

                                existUrlRegion.verticeIndex = i;
                                UrlRegionList.RemoveAt(UrlRegionList.Count - 1);
                                UrlRegionList.Add(existUrlRegion);

                                urlIndex = i + 4;
                                continue;
                            }
                        }

                        var urlRegion = new UrlRegion();
                        urlRegion.bottomLeft = uivex.position;

                        uivex = verts[i + 2];
                        uivex.position = uivex.position*unitsPerPixel;
                        uivex.position.x += roundingOffset.x;
                        uivex.position.y += roundingOffset.y;
                        urlRegion.topRight = uivex.position;

                        urlRegion.verticeIndex = i;
                        urlRegion.urlIndex = uivex.urlIndex;
                        urlRegion.url = uivex.url;
                        UrlRegionList.Add(urlRegion);

                        urlIndex = i + 4;
                        continue;
                    }

                    if (uivex.url != null)
                    {
                        continue;
                    }

                    UIVertex uiv = UIVertex.simpleVert;
                    uiv.position = uivex.position*unitsPerPixel;
                    uiv.position.x += roundingOffset.x;
                    uiv.position.y += roundingOffset.y;
                    uiv.color = uivex.color;

                    if (uivex.emojiIndex >= 0)
                    {
                        var emojiConfigIndex = TextGeneratorEx.GetEmojiConfigIndex(m_EmojiConfigList, uivex.emojiIndex);
                        uiv.uv1 = uivex.uv1;
                        if (!m_ColorInfluenceEmoji)
                        {
                            uiv.color = Color.white;
                        }
                        m_EmojiConfigList[emojiConfigIndex].target.Vertices.Add(uiv);

                        if (uivex.animation)
                        {
                            var animationEmoji = new AnimationEmoji();
                            animationEmoji.vertextStartIndex =
                                m_EmojiConfigList[emojiConfigIndex].target.Vertices.Count - 1;
                            animationEmoji.vertextStartIndex = TextGeneratorEx.GetEmojiIndex(m_EmojiConfigList,
                                emojiConfigIndex, uivex.emojiIndex);
                            m_EmojiConfigList[emojiConfigIndex].target.AnimitonEmojiList.Add(animationEmoji);
                        }
                    }
                    else
                    {
                        uiv.uv0 = uivex.uv0;
                        int tempVertsIndex = i & 3;
                        m_TempVerts[tempVertsIndex] = uiv;
                        if (tempVertsIndex == 3)
                            toFill.AddUIVertexQuad(m_TempVerts);
                    }
                }
            }
            else
            {
                for (int i = 0, vertCount = verts.Count; i < vertCount; ++i)
                {
                    UIVertexEx uivex = verts[i];
                    if (uivex.url != null && i >= urlIndex)
                    {
                        uivex.position = uivex.position*unitsPerPixel;
                        if (UrlRegionList.Count > 0)
                        {
                            var existUrlRegion = UrlRegionList[UrlRegionList.Count - 1];
                            if (existUrlRegion.url == uivex.url && existUrlRegion.urlIndex == uivex.urlIndex &&
                                Mathf.RoundToInt(existUrlRegion.bottomLeft.y) == Mathf.RoundToInt(uivex.position.y))
                            {
                                uivex = verts[i + 2];
                                uivex.position = uivex.position*unitsPerPixel;
                                uivex.position.x += roundingOffset.x;
                                uivex.position.y += roundingOffset.y;
                                existUrlRegion.topRight = uivex.position;

                                existUrlRegion.verticeIndex = i;
                                UrlRegionList.RemoveAt(UrlRegionList.Count - 1);
                                UrlRegionList.Add(existUrlRegion);

                                urlIndex = i + 4;
                                continue;
                            }
                        }

                        var urlRegion = new UrlRegion();
                        urlRegion.bottomLeft = uivex.position;

                        uivex = verts[i + 2];
                        uivex.position = uivex.position*unitsPerPixel;
                        urlRegion.topRight = uivex.position;

                        urlRegion.verticeIndex = i;
                        urlRegion.urlIndex = uivex.urlIndex;
                        urlRegion.url = uivex.url;
                        UrlRegionList.Add(urlRegion);

                        urlIndex = i + 4;
                        continue;
                    }

                    if (uivex.url != null)
                    {
                        continue;
                    }

                    UIVertex uiv = UIVertex.simpleVert;

                    uiv.position = uivex.position*unitsPerPixel;
                    uiv.color = uivex.color;

                    if (uivex.emojiIndex >= 0)
                    {
                        var emojiConfigIndex = TextGeneratorEx.GetEmojiConfigIndex(m_EmojiConfigList, uivex.emojiIndex);
                        uiv.uv1 = uivex.uv1;
                        if (!m_ColorInfluenceEmoji)
                        {
                            uiv.color = Color.white;
                        }
                        m_EmojiConfigList[emojiConfigIndex].target.Vertices.Add(uiv);

                        if (uivex.animation)
                        {
                            var animationEmoji = new AnimationEmoji();
                            animationEmoji.vertextStartIndex =
                                m_EmojiConfigList[emojiConfigIndex].target.Vertices.Count - 1;
                            animationEmoji.emojiIndex = TextGeneratorEx.GetEmojiIndex(m_EmojiConfigList,
                                emojiConfigIndex, uivex.emojiIndex);
                            m_EmojiConfigList[emojiConfigIndex].target.AnimitonEmojiList.Add(animationEmoji);
                        }
                    }
                    else
                    {
                        uiv.uv0 = uivex.uv0;
                        int tempVertsIndex = i & 3;
                        m_TempVerts[tempVertsIndex] = uiv;
                        if (tempVertsIndex == 3)
                            toFill.AddUIVertexQuad(m_TempVerts);
                    }
                }
            }

            if (newVertices)
            {
                if (_mAnimationEmojiPlay != null)
                {
                    _mAnimationEmojiPlay.Clear();
                }

                bool hasAnimationEmoji = false;
                for (int i = 0, count = EmojiConfigList.Count; i < count; ++i)
                {
                    var emojiConfig = m_EmojiConfigList[i];
                    if (emojiConfig.target != null)
                    {
                        emojiConfig.target.MarkVerticesDirty();
                        if (_mAnimationEmojiPlay != null && emojiConfig.target.AnimitonEmojiList != null &&
                            emojiConfig.target.AnimitonEmojiList.Count > 0)
                        {
                            hasAnimationEmoji = true;
                            _mAnimationEmojiPlay.EmojiImageList.Add(emojiConfig.target);
                        }
                    }
                }

                if (hasAnimationEmoji && _mAnimationEmojiPlay != null)
                {
                    _mAnimationEmojiPlay.enabled = hasAnimationEmoji;
                }
            }
        }

        public virtual void CalculateLayoutInputHorizontal() { }
        public virtual void CalculateLayoutInputVertical() { }

        public virtual float minWidth
        {
            get { return 0; }
        }

        public virtual float preferredWidth
        {
            get
            {
                var settings = GetGenerationSettings(Vector2.zero);
                return cachedTextGeneratorForLayout.GetPreferredWidth(m_Text, settings, m_EmojiConfigList) / pixelsPerUnit;
            }
        }

        public virtual float flexibleWidth { get { return -1; } }

        public virtual float minHeight
        {
            get { return 0; }
        }

        public virtual float preferredHeight
        {
            get
            {
                var settings = GetGenerationSettings(new Vector2(rectTransform.rect.size.x, 0.0f));
                return cachedTextGeneratorForLayout.GetPreferredHeight(m_Text, settings, m_EmojiConfigList) / pixelsPerUnit;
            }
        }

        public virtual float flexibleHeight { get { return -1; } }

        public virtual int layoutPriority { get { return 0; } }

#if UNITY_EDITOR
        public override void OnRebuildRequested()
        {
            // After a Font asset gets re-imported the managed side gets deleted and recreated,
            // that means the delegates are not persisted.
            // so we need to properly enforce a consistent state here.
            FontUpdateTrackerEx.UntrackText(this);
            FontUpdateTrackerEx.TrackText(this);

            // Also the textgenerator is no longer valid.
            cachedTextGenerator.Invalidate();

            base.OnRebuildRequested();
        }

        // The Text inspector editor can change the font, and we need a way to track changes so that we get the appropriate rebuild callbacks
        // We can intercept changes in OnValidate, and keep track of the previous font reference
        protected override void OnValidate()
        {
            if (m_FontData.font != m_LastTrackedFont)
            {
                Font newFont = m_FontData.font;
                m_FontData.font = m_LastTrackedFont;
                FontUpdateTrackerEx.UntrackText(this);
                m_FontData.font = newFont;
                FontUpdateTrackerEx.TrackText(this);

                m_LastTrackedFont = newFont;
            }
            base.OnValidate();

            if (_mAnimationEmojiPlay != null)
            {
                _mAnimationEmojiPlay.enabled = false;
            }
            cachedTextGenerator.Invalidate();
        }

        private void OnDrawGizmos()
        {
            GameObject go = UnityEditor.Selection.activeGameObject;
            bool isSelected = ((go != null) && (go.GetComponent<TextEx>() == this) &&
                               UnityEditor.Selection.activeGameObject == gameObject);
            Gizmos.matrix = transform.localToWorldMatrix;

            if (isSelected && m_UrlRegionList != null)
            {
                Gizmos.color = new Color(0f, 0.75f, 1f);
                for (int i = 0, count = m_UrlRegionList.Count; i < count; ++i)
                {
                    var urlRegion = m_UrlRegionList[i];
                    Gizmos.DrawWireCube(
                        new Vector3((urlRegion.topRight.x + urlRegion.bottomLeft.x)*0.5f,
                            (urlRegion.topRight.y + urlRegion.bottomLeft.y)*0.5f),
                        new Vector3(urlRegion.topRight.x - urlRegion.bottomLeft.x,
                            urlRegion.topRight.y - urlRegion.bottomLeft.y));
                }
            }
        }
#endif // if UNITY_EDITOR

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_UrlRegionList != null)
            {
                var localCursor = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor);
                for (int i = 0, count = m_UrlRegionList.Count; i < count; ++i)
                {
                    var urlRegion = m_UrlRegionList[i];
                    if (localCursor.x >= urlRegion.bottomLeft.x && localCursor.x <= urlRegion.topRight.x
                        && localCursor.y <= urlRegion.topRight.y && localCursor.y >= urlRegion.bottomLeft.y)
                    {
                        m_UrlClickEvent.Invoke(urlRegion.url);
                        break;
                    }
                }
            }
        }
    }
}