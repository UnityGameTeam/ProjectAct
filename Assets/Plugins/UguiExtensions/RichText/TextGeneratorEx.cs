using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Sprites;

namespace UguiExtensions
{
    public struct UIVertexEx
    {
        private static readonly Color32 s_DefaultColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue);

        private static readonly Vector4 s_DefaultTangent = new Vector4(1f, 0.0f, 0.0f, -1f);

        public static UIVertexEx simpleVert = new UIVertexEx()
        {
            position = Vector3.zero,
            normal = Vector3.back,
            tangent = UIVertexEx.s_DefaultTangent,
            color = UIVertexEx.s_DefaultColor,
            uv0 = Vector2.zero,
            uv1 = Vector2.zero,
            emojiIndex = -1,
        };

        public Vector3 position;
        public Vector3 normal;
        public Color32 color;
        public Vector2 uv0;
        public Vector2 uv1;
        public Vector4 tangent;
        public int emojiIndex;
        public bool animation;
        public string url;
        public int urlIndex;
    }

    public class GlyphInfo
    {
        public Vector2 v0;
        public Vector2 v1;
        public Vector2 u0;
        public Vector2 u1;
        public Vector2 u2;
        public Vector2 u3;
        public float advance = 0f;
    }

    public struct TextElementInfo
    {
        public int lineNumber;
        public short fontSize;
        public int ch;

        public byte subscriptMode;

        public bool emoji;
        public bool underline;
        public bool strikethrough;
        public bool bold;
        public bool italic;

        public Color color;
        public string url;
        public int urlIndex;

        public string GetChar()
        {
            return ((char) ch).ToString();
        }
    }

    public struct LineInfo
    {
        public int maxEmojiHeight; //当前行表情的最大高度
        public int maxTextHeight; //当前行文字的最大高度
        public bool hasBold; //是否有粗体
    }

    /// <summary>
    /// 仅支持动态字体的文本生成
    /// </summary>
    public class TextGeneratorEx
    {
        private TextGenerationSettingsEx m_LastSettings;
        private string m_LastString;
        private bool m_HasGenerated;
        private Vector2 m_PrintedSize;

        private readonly List<UIVertexEx> m_Verts;

        private static List<TextElementInfo> m_TextElementList = new List<TextElementInfo>();
        private static List<LineInfo> m_LineInfoList = new List<LineInfo>(32);

        private static CharacterInfo m_TempChar;
        private static GlyphInfo m_GlyphInfo = new GlyphInfo();

        private static float finalSpacingX;
        private static float lineFactor;

        private static int _version = 0;
        private static List<EmojiConfig> s_EmojiConfigList;
        private static List<Color> mColors = new List<Color>();
        private static List<Vector2> m_TempUVs = new List<Vector2>(16);
        private static List<Vector3> m_TempVertices = new List<Vector3>(16);
        private static float sizeShrinkage = 0.67f;

        private static float emojiBaseline;
        private static float m_Baseline = 0f;
        private static float m_BaselineOffset = 0f;

        public IList<UIVertexEx> verts
        {
            get { return m_Verts; }
        }

        static TextGeneratorEx()
        {
            for (int i = 0; i < 16; i++)
            {
                m_TempUVs.Add(Vector2.one);
                m_TempVertices.Add(Vector3.one);
            }
        }

        public TextGeneratorEx() : this(-1)
        {

        }

        public TextGeneratorEx(int initialCapacity)
        {
            m_Verts = new List<UIVertexEx>((initialCapacity + 1)*4);
        }

        public void Invalidate()
        {
            m_HasGenerated = false;
        }

        public float GetPreferredWidth(string text, TextGenerationSettingsEx settings,
            List<EmojiConfig> emojiConfigList)
        {
            GetPreferredSize(text, settings, emojiConfigList);
            return m_PrintedSize.x;
        }

        public float GetPreferredHeight(string text, TextGenerationSettingsEx settings,
            List<EmojiConfig> emojiConfigList)
        {
            GetPreferredSize(text, settings, emojiConfigList);
            return m_PrintedSize.y;
        }

        private void GetPreferredSize(string text, TextGenerationSettingsEx settings,
            List<EmojiConfig> emojiConfigList)
        {
            if (m_HasGenerated && text == m_LastString && settings.Equals(m_LastSettings))
                return;

            s_EmojiConfigList = emojiConfigList;
            m_LastString = text;
            m_HasGenerated = true;
            m_LastSettings = ValidatedSettings(settings);

            int maxFontSize = m_LastSettings.fontSize;
            if (m_LastSettings.resizeTextForBestFit)
            {
                maxFontSize = m_LastSettings.resizeTextMaxSize;
            }

            var regionWidth = m_LastSettings.generationExtents.x*m_LastSettings.scaleFactor;
            lineFactor = 1.135f*m_LastSettings.lineSpacing*m_LastSettings.scaleFactor;
            finalSpacingX = m_LastSettings.SpacingX*m_LastSettings.scaleFactor;

            if (m_LastSettings.generationExtents.x < 1)
            {
                regionWidth = 100000000;
            }

            m_PrintedSize = CalculateWrapSize(text, maxFontSize, regionWidth);
        }

        private TextGenerationSettingsEx ValidatedSettings(TextGenerationSettingsEx settings)
        {
            if (settings.font != null && settings.font.dynamic)
                return settings;

            if (settings.fontSize != 0 || settings.fontStyle != FontStyle.Normal)
            {
                Debug.LogWarning("Font size and style overrides are only supported for dynamic fonts.");
                settings.fontSize = 0;
                settings.fontStyle = FontStyle.Normal;
            }

            if (settings.resizeTextForBestFit)
            {
                Debug.LogWarning("BestFit is only suppoerted for dynamic fonts.");
                settings.resizeTextForBestFit = false;
            }

            return settings;
        }

        public bool Populate(string text, TextGenerationSettingsEx settings, List<EmojiConfig> emojiConfigList)
        {
            if (m_HasGenerated && text == m_LastString && settings.Equals(m_LastSettings))
                return false;

            s_EmojiConfigList = emojiConfigList;
            m_LastString = text;
            m_HasGenerated = true;
            m_LastSettings = ValidatedSettings(settings);
            verts.Clear();

            int minFontSize = m_LastSettings.fontSize;
            int maxFontSize = m_LastSettings.fontSize;
            if (m_LastSettings.resizeTextForBestFit)
            {
                minFontSize = m_LastSettings.resizeTextMinSize;
                maxFontSize = m_LastSettings.resizeTextMaxSize;
            }

            var regionWidth = m_LastSettings.generationExtents.x*m_LastSettings.scaleFactor;
            var regionHeight = Mathf.RoundToInt(m_LastSettings.generationExtents.y*m_LastSettings.scaleFactor);
            lineFactor = 1.135f*m_LastSettings.lineSpacing*m_LastSettings.scaleFactor;
            finalSpacingX = m_LastSettings.SpacingX*m_LastSettings.scaleFactor;

            if (m_LastSettings.horizontalOverflow == HorizontalWrapMode.Overflow)
            {
                regionWidth = 10000000;
            }

            if (m_LastSettings.verticalOverflow == VerticalWrapMode.Overflow)
            {
                regionHeight = 10000000;
            }

            for (int i = maxFontSize; i >= minFontSize; --i)
            {
                int result = WrapText(text, i, regionWidth, regionHeight);

                if (result == 2)
                {
                    return false;
                }

                if (result == 1 && m_LastSettings.resizeTextForBestFit && i != minFontSize)
                {
                    continue;
                }

                m_PrintedSize = CalculatePrintedSize(m_TextElementList, m_TextElementList.Count);
                return Print();
            }
            return false;
        }

        private int WrapText(string text, int fontSize, float regionWidth, int regionHeight)
        {
            m_TextElementList.Clear();
            if (regionWidth < 1 || regionHeight < 1)
            {
                return 1;
            }

            if (string.IsNullOrEmpty(text))
                text = " ";

            float remainingWidth = regionWidth;
            float remainingHeight = regionHeight;

            int start = 0, offset = 0, lineCount = 1;
            bool lineIsEmpty = true;
            bool eastern = false;
            bool hasSpace = false;
            int sub = 0;

            mColors.Clear();
            mColors.Add(Color.white);
            Color32 uc = m_LastSettings.color;

            bool bold = false;
            bool italic = false;
            bool underline = false;
            bool strikethrough = false;
            int curFontSize = fontSize;

            int emojiSymbol = int.MinValue;
            int currentVersion = _version;
            bool supportEllipsizeEnd = true;
            string url = null;
            int urlIndex = 0;

            for (int textLength = text.Length, index = 0; index < textLength; ++index)
            {
                char ch = text[index];
                if (m_LastSettings.richText &&
                    ParseSymbol(text, m_LastSettings, ref index, mColors, ref sub, ref bold, ref italic, ref underline,
                        ref strikethrough, fontSize, ref curFontSize, ref emojiSymbol, ref url, ref urlIndex))
                {
                    --index;
                    uc = mColors.Count > 0 ? m_LastSettings.color*mColors[mColors.Count - 1] : m_LastSettings.color;
                    if (emojiSymbol != int.MinValue)
                    {
                        var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, emojiSymbol);
                        var emojiSymbolElement = GetEmoji(s_EmojiConfigList, emojiConfigIndex, emojiSymbol);
                        var emojiHeight = emojiSymbolElement.GetHeight();
                        var emojiInfo = new TextElementInfo();
                        emojiInfo.ch = emojiSymbol;
                        emojiInfo.fontSize = (short) emojiHeight;
                        emojiInfo.emoji = true;
                        emojiInfo.color = uc;
                        emojiInfo.bold = bold;
                        emojiInfo.underline = underline;
                        if (url != null)
                        {
                            emojiInfo.url = url;
                            emojiInfo.urlIndex = urlIndex;
                        }
                        m_TextElementList.Add(emojiInfo);
                    }
                    if (curFontSize != fontSize)
                    {
                        supportEllipsizeEnd = false;
                    }
                    continue;
                }

                if (ch == '\n')
                {
                    var newLineCharInfo = new TextElementInfo();
                    newLineCharInfo.ch = ch;
                    newLineCharInfo.fontSize = (short) curFontSize;
                    m_TextElementList.Add(newLineCharInfo);
                    continue;
                }

                if (ch < ' ')
                {
                    continue;
                }

                var fontStyle = GetGlyphFontStyle(italic, bold);
                Prepare(ch.ToString(), curFontSize, fontStyle);
                //这段代码用于解决，如果在生成字体时候字体纹理变化导致先生成的字体uv错乱
                //但是似乎字体纹理变化函数的调用是异步的，如果这里调用Prepare导致字体纹理变化，
                //会先调用字体纹理变化回调，重新生成顶点，也会执行到这
                //问题是当前的顶点还没生成完成，函数就被中断执行
                //然后生成新的顶点，新的顶点生成完成后，又继续执行从这里开始执行，导致出现问题
                //不太理解Unity到底是怎么处理函数执行的，这里为了安全，采用了版本控制的方式
                //避免中断调用带来的字体uv错乱的问题，以下调用类似
                if (currentVersion != _version)
                {
                    return 2;
                }

                float w = GetGlyphWidth(ch, curFontSize, fontStyle);
                if (w <= 0f)
                {
                    continue;
                }

                if (underline)
                {
                    Prepare("_", curFontSize, bold ? FontStyle.Bold : FontStyle.Normal);
                    if (currentVersion != _version)
                    {
                        return 2;
                    }
                }

                if (strikethrough)
                {
                    Prepare("-", curFontSize, bold ? FontStyle.Bold : FontStyle.Normal);
                    if (currentVersion != _version)
                    {
                        return 2;
                    }
                }

                var textElementInfo = new TextElementInfo();
                textElementInfo.fontSize = (short) curFontSize;
                textElementInfo.bold = bold;
                textElementInfo.color = uc;
                textElementInfo.italic = italic;
                textElementInfo.strikethrough = strikethrough;
                textElementInfo.subscriptMode = (byte) sub;
                textElementInfo.underline = underline;
                textElementInfo.ch = ch;
                if (url != null)
                {
                    textElementInfo.url = url;
                    textElementInfo.urlIndex = urlIndex;
                }
                m_TextElementList.Add(textElementInfo);
            }
            ++_version;

            int textCount = m_TextElementList.Count;
            int maxEmojiHeight = int.MinValue;
            int maxTextHeight = int.MinValue;
            float glyphWidth = 0f;

            int spaceBeforeMaxTextHeight = int.MinValue;
            int spaceBeforeMaxEmojiHeight = int.MinValue;

            for (; offset < textCount; ++offset)
            {
                var textElementInfo = m_TextElementList[offset];
                if (!textElementInfo.emoji)
                {
                    char ch = (char) textElementInfo.ch;
                    if (ch > 12287) eastern = true;

                    if (ch == '\n')
                    {
                        textElementInfo.lineNumber = lineCount;
                        m_TextElementList[offset] = textElementInfo;

                        if (maxTextHeight == int.MinValue)
                        {
                            maxTextHeight = textElementInfo.fontSize;
                        }

                        bool result = true;
                        var height = GetPrintLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize,
                            ref result);
                        if (!result)
                        {
                            return 2;
                        }

                        remainingHeight -= height;
                        if (Mathf.RoundToInt(remainingHeight) < 0)
                        {
                            start = CheckRemoveLine(lineCount, offset);
                            break;
                        }

                        remainingWidth = regionWidth;
                        ++lineCount;
                        maxEmojiHeight = int.MinValue;
                        maxTextHeight = int.MinValue;
                        eastern = false;
                        hasSpace = false;
                        continue;
                    }

                    var fontStyle = GetGlyphFontStyle(textElementInfo.italic, textElementInfo.bold);
                    glyphWidth = GetGlyphWidth(ch, textElementInfo.fontSize, textElementInfo.subscriptMode,
                        finalSpacingX, fontStyle);
                }
                else
                {
                    eastern = true;
                    var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, textElementInfo.ch);
                    glyphWidth = finalSpacingX +
                                 GetEmoji(s_EmojiConfigList, emojiConfigIndex, textElementInfo.ch).GetWidth()*
                                 m_LastSettings.scaleFactor;
                }

                if (glyphWidth > regionWidth)
                {
                    start = CheckRemoveLine(lineCount, offset - 1);
                    remainingHeight = float.MinValue;
                    break;
                }

                remainingWidth -= glyphWidth;
                textElementInfo.lineNumber = lineCount;
                m_TextElementList[offset] = textElementInfo;

                if (!textElementInfo.emoji && IsSpace(textElementInfo.ch))
                {
                    start = offset;
                    hasSpace = true;
                    eastern = false;

                    spaceBeforeMaxTextHeight = maxTextHeight;
                    spaceBeforeMaxEmojiHeight = maxEmojiHeight;
                    if (!textElementInfo.emoji && textElementInfo.fontSize > spaceBeforeMaxTextHeight)
                    {
                        spaceBeforeMaxTextHeight = textElementInfo.fontSize;
                    }

                    if (textElementInfo.emoji && textElementInfo.fontSize > spaceBeforeMaxEmojiHeight)
                    {
                        spaceBeforeMaxEmojiHeight = textElementInfo.fontSize;
                    }
                }

                if (Mathf.RoundToInt(remainingWidth) < 0)
                {
                    if (!textElementInfo.emoji && IsSpace(textElementInfo.ch))
                    {
                        hasSpace = false;
                        eastern = true;
                    }
                    else
                    {
                        if (hasSpace)
                        {
                            if (eastern)
                            {
                                eastern = textElementInfo.emoji || textElementInfo.ch > 12287;
                            }
                            else
                            {
                                lineIsEmpty = false;
                                for (var i = offset + 1; i < textCount; ++i)
                                {
                                    var c = m_TextElementList[i].ch;
                                    if (m_TextElementList[i].emoji || c > 12287)
                                    {
                                        lineIsEmpty = true;
                                        break;
                                    }

                                    if (IsSpace(c))
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        hasSpace = false;
                    }

                    if (lineIsEmpty)
                    {
                        bool result = true;
                        var height = GetPrintLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize,
                            ref result);
                        if (!result)
                        {
                            return 2;
                        }
                        remainingHeight -= height;
                        if (Mathf.RoundToInt(remainingHeight) < 0)
                        {
                            start = CheckRemoveLine(lineCount, offset);
                            break;
                        }

                        ++lineCount;
                        --offset;
                        maxEmojiHeight = int.MinValue;
                        maxTextHeight = int.MinValue;
                        lineIsEmpty = true;
                        remainingWidth = regionWidth;
                    }
                    else
                    {
                        bool result = true;
                        var height = GetPrintLineHeight(spaceBeforeMaxTextHeight, spaceBeforeMaxEmojiHeight,
                            m_LastSettings.fontSize, ref result);
                        if (!result)
                        {
                            return 2;
                        }
                        remainingHeight -= height;

                        if (Mathf.RoundToInt(remainingHeight) < 0)
                        {
                            start = CheckRemoveLine(lineCount, offset);
                            break;
                        }

                        offset = start;
                        ++lineCount;
                        lineIsEmpty = true;
                        maxEmojiHeight = int.MinValue;
                        maxTextHeight = int.MinValue;
                        remainingWidth = regionWidth;
                    }
                }
                else
                {
                    if (!textElementInfo.emoji && textElementInfo.fontSize > maxTextHeight)
                    {
                        maxTextHeight = textElementInfo.fontSize;
                    }

                    if (textElementInfo.emoji && textElementInfo.fontSize > maxEmojiHeight)
                    {
                        maxEmojiHeight = textElementInfo.fontSize;
                    }
                }
            }

            if (Mathf.RoundToInt(remainingHeight) >= 0)
            {
                bool result = true;
                var height = GetPrintLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize, ref result);
                if (!result)
                {
                    return 2;
                }
                remainingHeight -= height;
                start = m_TextElementList.Count;
                if (Mathf.RoundToInt(remainingHeight) < 0)
                {
                    start = CheckRemoveLine(lineCount, offset);
                }
            }

            ///检查是否是单词分词导致，最后一行可能有一部分的留白，将剩余的文字符合条件的文字补充到留白处
            var endIndex = start - 1;
            if (endIndex > 0 && endIndex < m_TextElementList.Count)
            {
                var lastElementInfo = m_TextElementList[endIndex];
                if (!lastElementInfo.emoji && IsSpace(lastElementInfo.ch))
                {
                    var lineNumber = lastElementInfo.lineNumber;
                    var startIndex = 0;
                    for (int i = endIndex - 1; i >= 0; --i)
                    {
                        if (m_TextElementList[i].lineNumber != lineNumber)
                        {
                            startIndex = i + 1;
                            break;
                        }
                    }

                    var size = CalculatePrintedSize(m_TextElementList, startIndex);
                    remainingHeight = regionHeight - size.y;
                    remainingWidth = regionWidth;
                    lineCount = lineNumber;
                    maxEmojiHeight = int.MinValue;
                    maxTextHeight = int.MinValue;

                    for (; startIndex < textCount; ++startIndex)
                    {
                        var textElementInfo = m_TextElementList[startIndex];
                        if (!textElementInfo.emoji)
                        {
                            char ch = (char) textElementInfo.ch;
                            if (ch == '\n')
                            {
                                start = startIndex;
                                break;
                            }

                            var fontStyle = GetGlyphFontStyle(textElementInfo.italic, textElementInfo.bold);
                            glyphWidth = GetGlyphWidth(ch, textElementInfo.fontSize, textElementInfo.subscriptMode,
                                finalSpacingX, fontStyle);
                        }
                        else
                        {
                            var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, textElementInfo.ch);
                            glyphWidth = finalSpacingX +
                                         GetEmoji(s_EmojiConfigList, emojiConfigIndex, textElementInfo.ch).GetWidth()*
                                         m_LastSettings.scaleFactor;
                        }

                        if (glyphWidth > regionWidth)
                        {
                            start = startIndex;
                            break;
                        }

                        remainingWidth -= glyphWidth;
                        if (Mathf.RoundToInt(remainingWidth) < 0)
                        {
                            start = startIndex;
                            break;
                        }

                        if (!textElementInfo.emoji && textElementInfo.fontSize > maxTextHeight)
                        {
                            maxTextHeight = textElementInfo.fontSize;
                        }

                        if (textElementInfo.emoji && textElementInfo.fontSize > maxEmojiHeight)
                        {
                            maxEmojiHeight = textElementInfo.fontSize;
                        }

                        bool result = true;
                        var height = GetPrintLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize,
                            ref result);
                        if (!result)
                        {
                            return 2;
                        }
                        if (Mathf.RoundToInt(remainingHeight - height) < 0)
                        {
                            start = startIndex;
                            break;
                        }

                        textElementInfo.lineNumber = lineCount;
                        m_TextElementList[startIndex] = textElementInfo;
                    }
                }
            }

            RemoveLine(start);

            if ((offset == textCount) && (m_TextElementList.Count == textCount))
            {
                return 0;
            }

            //省略号,只支持所有文字尺寸相同的情况下
            if (m_LastSettings.elipsizeEnd && m_TextElementList.Count > 0 && supportEllipsizeEnd)
            {
                currentVersion = _version;
                Prepare(".", m_LastSettings.fontSize, m_LastSettings.fontStyle);
                if (currentVersion != _version)
                {
                    return 2;
                }

                float w = GetGlyphWidth((int) '.', m_LastSettings.fontSize, m_LastSettings.fontStyle);
                if (w > 0f)
                {
                    float totalWidth = (w + finalSpacingX)*3;
                    if (totalWidth <= regionWidth)
                    {
                        textCount = m_TextElementList.Count;
                        var lineNumber = m_TextElementList[textCount - 1].lineNumber;
                        var lastLineWidth = 0f;
                        for (int i = textCount - 1; i >= 0; --i)
                        {
                            var textElementInfo = m_TextElementList[i];
                            if (lineNumber != textElementInfo.lineNumber)
                            {
                                break;
                            }

                            if (textElementInfo.emoji)
                            {
                                var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, textElementInfo.ch);
                                lastLineWidth += finalSpacingX +
                                                 GetEmoji(s_EmojiConfigList, emojiConfigIndex, textElementInfo.ch)
                                                     .GetWidth()*m_LastSettings.scaleFactor;
                            }
                            else
                            {
                                var fontStyle = GetGlyphFontStyle(textElementInfo.italic, textElementInfo.bold);
                                w = GetGlyphWidth((char) textElementInfo.ch, textElementInfo.fontSize,
                                    textElementInfo.subscriptMode, finalSpacingX, fontStyle);
                                if (w > 0)
                                {
                                    lastLineWidth += w;
                                }
                            }
                        }

                        //有足够空间
                        if (regionWidth - lastLineWidth >= totalWidth)
                        {
                            if (m_LastSettings.elipsizeEnd)
                            {
                                //这里实际还有一个问题如果全部是表情，使用默认的字体size,这个size可能比所有的表情都大,可能导致超出显示区域，不过这个问题可以通过修改字体的size来解决，这个就不考虑了
                                var elipsizeElementInfo = new TextElementInfo();
                                elipsizeElementInfo.fontSize = (short) m_LastSettings.fontSize;
                                elipsizeElementInfo.ch = '.';
                                elipsizeElementInfo.color = m_LastSettings.color;
                                elipsizeElementInfo.lineNumber = m_TextElementList[textCount - 1].lineNumber;
                                m_TextElementList.Add(elipsizeElementInfo);
                                m_TextElementList.Add(elipsizeElementInfo);
                                m_TextElementList.Add(elipsizeElementInfo);
                            }
                        }
                        else
                        {
                            lastLineWidth = 0;
                            start = int.MaxValue;
                            for (int i = textCount - 1; i >= 0; --i)
                            {
                                var textElementInfo = m_TextElementList[i];
                                if (lineNumber != textElementInfo.lineNumber)
                                {
                                    break;
                                }

                                if (textElementInfo.emoji)
                                {
                                    var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, textElementInfo.ch);
                                    lastLineWidth += finalSpacingX +
                                                     GetEmoji(s_EmojiConfigList, emojiConfigIndex, textElementInfo.ch)
                                                         .GetWidth()*m_LastSettings.scaleFactor;
                                }
                                else
                                {
                                    var fontStyle = GetGlyphFontStyle(textElementInfo.italic, textElementInfo.bold);
                                    w = GetGlyphWidth((char) textElementInfo.ch, textElementInfo.fontSize,
                                        textElementInfo.subscriptMode, finalSpacingX, fontStyle);
                                    if (w > 0)
                                    {
                                        lastLineWidth += w;
                                    }
                                }

                                if (lastLineWidth >= totalWidth)
                                {
                                    start = i;
                                    break;
                                }
                            }

                            if (start != int.MaxValue)
                            {
                                RemoveLine(start);
                                textCount = m_TextElementList.Count;
                                var elipsizeElementInfo = new TextElementInfo();
                                elipsizeElementInfo.fontSize = (short) m_LastSettings.fontSize;
                                elipsizeElementInfo.ch = '.';
                                elipsizeElementInfo.color = m_LastSettings.color;
                                elipsizeElementInfo.lineNumber = m_TextElementList[textCount - 1].lineNumber;
                                m_TextElementList.Add(elipsizeElementInfo);
                                m_TextElementList.Add(elipsizeElementInfo);
                                m_TextElementList.Add(elipsizeElementInfo);
                            }
                        }
                    }
                }
            }
            return 1;
        }

        private int CheckRemoveLine(int lineCount, int endIndex)
        {
            var start = endIndex < 0 ? 0 : endIndex;
            for (int i = m_TextElementList.Count - 1; i >= 0; --i)
            {
                if (i > endIndex)
                {
                    continue;
                }

                if (m_TextElementList[i].lineNumber != lineCount)
                {
                    break;
                }
                start = i;
            }
            return start;
        }

        private void RemoveLine(int index)
        {
            for (int i = m_TextElementList.Count - 1; i >= index; --i)
            {
                m_TextElementList.RemoveAt(i);
            }
        }

        private Vector2 CalculatePrintedSize(List<TextElementInfo> textPrintList, int textCount)
        {
            Vector2 size = Vector2.zero;
            if (textPrintList != null && textPrintList.Count > 0)
            {
                float x = 0f, y = 0f, maxX = 0f;
                int maxEmojiHeight = int.MinValue;
                int maxTextHeight = int.MinValue;
                int lineNumber = 1;
                for (int i = 0; i < textCount; ++i)
                {
                    var textElementInfo = textPrintList[i];
                    if (textElementInfo.lineNumber != lineNumber)
                    {
                        if (x > maxX) maxX = x;
                        x = 0f;

                        y += GetLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize);
                        maxEmojiHeight = int.MinValue;
                        maxTextHeight = int.MinValue;
                        lineNumber = textElementInfo.lineNumber;
                    }

                    if (textElementInfo.emoji)
                    {
                        var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, textElementInfo.ch);
                        var emojiSymbolElement = GetEmoji(s_EmojiConfigList, emojiConfigIndex, textElementInfo.ch);
                        var emojiHeight = emojiSymbolElement.GetHeight();
                        x += finalSpacingX + emojiSymbolElement.GetWidth()*m_LastSettings.scaleFactor;

                        if (emojiHeight > maxEmojiHeight)
                        {
                            maxEmojiHeight = emojiHeight;
                        }
                        continue;
                    }

                    if (textElementInfo.ch == '\n')
                    {
                        if (maxTextHeight == int.MinValue)
                        {
                            maxTextHeight = textElementInfo.fontSize;
                        }
                        continue;
                    }

                    var fontStyle = GetGlyphFontStyle(textElementInfo.italic, textElementInfo.bold);
                    float glyphWidth = GetGlyphWidth((char) textElementInfo.ch, textElementInfo.fontSize,
                        textElementInfo.subscriptMode, finalSpacingX, fontStyle);
                    if (glyphWidth <= 0)
                    {
                        continue;
                    }

                    x += glyphWidth;

                    if (textElementInfo.fontSize > maxTextHeight)
                    {
                        maxTextHeight = textElementInfo.fontSize;
                    }
                }

                if (x > maxX) maxX = x;
                y += GetLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize);

                size.x = maxX;
                size.y = y;
            }

            return size;
        }

        private bool Print()
        {
            if (m_TextElementList.Count == 0)
                return false;

            int indexOffset = 0, textCount = m_TextElementList.Count;

            float x = 0f, y = 0;

            m_LineInfoList.Clear();
            var currentLineIndex = 1;

            var currentMaxEmojiHeight = int.MinValue;
            var currentMaxTextHeight = int.MinValue;
            for (int i = 0; i < textCount; ++i)
            {
                var textInfo = m_TextElementList[i];
                if (textInfo.lineNumber != currentLineIndex)
                {
                    var curLineInfo = new LineInfo();
                    curLineInfo.maxEmojiHeight = currentMaxEmojiHeight;
                    curLineInfo.maxTextHeight = currentMaxTextHeight;
                    m_LineInfoList.Add(curLineInfo);

                    currentLineIndex = textInfo.lineNumber;
                    currentMaxEmojiHeight = textInfo.emoji ? textInfo.fontSize : int.MinValue;
                    currentMaxTextHeight = textInfo.emoji ? int.MinValue : textInfo.fontSize;
                }
                else if (textInfo.ch == '\n')
                {
                    if (currentMaxTextHeight < 0)
                        currentMaxTextHeight = textInfo.fontSize;
                }
                else
                {
                    if (textInfo.emoji)
                    {
                        if (textInfo.fontSize > currentMaxEmojiHeight)
                        {
                            currentMaxEmojiHeight = textInfo.fontSize;
                        }
                    }
                    else
                    {
                        if (textInfo.fontSize > currentMaxTextHeight)
                        {
                            currentMaxTextHeight = textInfo.fontSize;
                        }
                    }
                }
            }

            var lineInfo = new LineInfo();
            lineInfo.maxEmojiHeight = currentMaxEmojiHeight;
            lineInfo.maxTextHeight = currentMaxTextHeight;
            m_LineInfoList.Add(lineInfo);

            float v0x;
            float v1x;
            float v1y;
            float v0y;
            float prevX = 0;

            int currentLine = 1;
            lineInfo = m_LineInfoList[0];
            bool result = true;
            float currentLineHeight = GetPrintLineHeight(lineInfo.maxTextHeight, lineInfo.maxEmojiHeight,
                m_LastSettings.fontSize, ref result);

            if (!result)
            {
                return false;
            }

            float yOffsetFactor = 0.04f * m_LastSettings.scaleFactor;
            float yOffset = lineInfo.maxTextHeight < 0
                ? m_LastSettings.fontSize * 0.0675f * m_LastSettings.scaleFactor
                : lineInfo.maxTextHeight * yOffsetFactor;

            yOffsetFactor = 0.05675f * m_LastSettings.scaleFactor;
            yOffset = Mathf.Floor(yOffset);

            for (int i = 0; i < textCount; ++i)
            {
                var textInfo = m_TextElementList[i];

                if (textInfo.lineNumber != currentLine)
                {
                    if (!IsLeftPivot(m_LastSettings.textAnchor))
                    {
                        Align(indexOffset, x - finalSpacingX);
                        indexOffset = verts.Count;
                    }

                    x = 0;
                    y += currentLineHeight;

                    lineInfo = m_LineInfoList[currentLine];
                    result = true;
                    currentLineHeight = GetPrintLineHeight(lineInfo.maxTextHeight, lineInfo.maxEmojiHeight,
                        m_LastSettings.fontSize, ref result);
                    if (!result)
                    {
                        return false;
                    }

                    yOffset = lineInfo.maxTextHeight < 0
                        ? m_LastSettings.fontSize*yOffsetFactor
                        : lineInfo.maxTextHeight*yOffsetFactor;
                    yOffset = Mathf.Floor(yOffset);
                    currentLine = textInfo.lineNumber;
                }
                prevX = x;

                if (!textInfo.emoji)
                {
                    if (textInfo.ch == '\n')
                    {
                        continue;
                    }

                    var fontStyle = GetGlyphFontStyle(textInfo.italic, textInfo.bold);
                    GlyphInfo glyph = GetGlyph(textInfo.ch, textInfo.fontSize, fontStyle);
                    if (glyph == null)
                    {
                        continue;
                    }

                    if (textInfo.subscriptMode != 0)
                    {
                        glyph.v0.x *= sizeShrinkage;
                        glyph.v0.y *= sizeShrinkage;
                        glyph.v1.x *= sizeShrinkage;
                        glyph.v1.y *= sizeShrinkage;

                        if (textInfo.subscriptMode == 1)
                        {
                            glyph.v0.y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) +
                                           textInfo.fontSize*0.075f)*m_LastSettings.scaleFactor;
                            glyph.v1.y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) +
                                           textInfo.fontSize*0.075f)*m_LastSettings.scaleFactor;
                        }
                        else
                        {
                            glyph.v0.y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) - textInfo.fontSize*0.33f)*
                                          m_LastSettings.scaleFactor;
                            glyph.v1.y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) - textInfo.fontSize*0.33f)*
                                          m_LastSettings.scaleFactor;
                        }
                    }

                    v0x = glyph.v0.x + x;
                    v0y = glyph.v0.y - (y + yOffset);
                    v1x = glyph.v1.x + x;
                    v1y = glyph.v1.y - (y + yOffset);

                    if (IsSpace(textInfo.ch))
                    {
                        if (textInfo.underline)
                        {
                            textInfo.ch = '_';
                        }
                        else if (textInfo.strikethrough)
                        {
                            textInfo.ch = '-';
                        }
                    }

                    x += (textInfo.subscriptMode == 0)
                        ? finalSpacingX + glyph.advance
                        : (finalSpacingX + glyph.advance)*sizeShrinkage;

                    if (IsSpace(textInfo.ch))
                    {
                        if (textInfo.url != null)
                        {
                            m_TempVertices[0] = new Vector3(prevX, -y - currentLineHeight);
                            m_TempVertices[1] = new Vector3(prevX, -y);
                            m_TempVertices[2] = new Vector3(x, -y);
                            m_TempVertices[3] = new Vector3(x, -y - currentLineHeight);

                            for (int j = 0; j < 4; ++j)
                            {
                                var uiVertext = UIVertexEx.simpleVert;
                                uiVertext.position = m_TempVertices[j];
                                uiVertext.url = textInfo.url;
                                uiVertext.urlIndex = textInfo.urlIndex;
                                verts.Add(uiVertext);
                            }
                        }
                        continue;
                    }

                    //纹理坐标
                    m_TempUVs[0] = glyph.u0;
                    m_TempUVs[1] = glyph.u1;
                    m_TempUVs[2] = glyph.u2;
                    m_TempUVs[3] = glyph.u3;

                    m_TempVertices[0] = new Vector3(v0x, v0y);
                    m_TempVertices[1] = new Vector3(v0x, v1y);
                    m_TempVertices[2] = new Vector3(v1x, v1y);
                    m_TempVertices[3] = new Vector3(v1x, v0y);

                    for (int j = 0; j < 4; ++j)
                    {
                        var uiVertext = UIVertexEx.simpleVert;
                        uiVertext.uv0 = m_TempUVs[j];
                        uiVertext.color = textInfo.color;
                        uiVertext.position = m_TempVertices[j];
                        verts.Add(uiVertext);
                    }

                    if (textInfo.strikethrough)
                    {
                        GlyphInfo dash = GetGlyph('-', textInfo.fontSize,
                            textInfo.bold ? FontStyle.Bold : FontStyle.Normal);
                        if (dash == null)
                            continue;

                        float cx = (dash.u0.x + dash.u2.x)*0.5f;
                        m_TempUVs[0] = new Vector2(cx, dash.u0.y);
                        m_TempUVs[1] = new Vector2(cx, dash.u2.y);
                        m_TempUVs[2] = new Vector2(cx, dash.u2.y);
                        m_TempUVs[3] = new Vector2(cx, dash.u0.y);

                        v0y = dash.v0.y;
                        v1y = dash.v1.y;
                        if (textInfo.subscriptMode != 0)
                        {
                            v0y *= sizeShrinkage;
                            v1y *= sizeShrinkage;

                            if (textInfo.subscriptMode == 1)
                            {
                                v0y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) + textInfo.fontSize*0.075f)*
                                       m_LastSettings.scaleFactor;
                                v1y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) + textInfo.fontSize*0.075f)*
                                       m_LastSettings.scaleFactor;
                            }
                            else
                            {
                                v0y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) - textInfo.fontSize*0.33f)*
                                       m_LastSettings.scaleFactor;
                                v1y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) - textInfo.fontSize*0.33f)*
                                       m_LastSettings.scaleFactor;
                            }
                        }
                        v0y = (v0y - (y + yOffset));
                        v1y = (v1y - (y + yOffset));

                        m_TempVertices[0] = new Vector3(prevX, v0y);
                        m_TempVertices[1] = new Vector3(prevX, v1y);
                        m_TempVertices[2] = new Vector3(x, v1y);
                        m_TempVertices[3] = new Vector3(x, v0y);

                        for (int j = 0; j < 4; ++j)
                        {
                            var uiVertext = UIVertexEx.simpleVert;
                            uiVertext.uv0 = m_TempUVs[j];
                            uiVertext.color = textInfo.color;
                            uiVertext.position = m_TempVertices[j];
                            verts.Add(uiVertext);
                        }
                    }
                }
                else
                {
                    var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, textInfo.ch);
                    var emoji = GetEmoji(s_EmojiConfigList, emojiConfigIndex, textInfo.ch);
                    v0x = x + emoji.paddingLeft*m_LastSettings.scaleFactor;
                    v0y = -emojiBaseline*m_LastSettings.scaleFactor - (y + yOffset);
                    v1x = emoji.width*m_LastSettings.scaleFactor + v0x;
                    v1y = v0y + emoji.height*m_LastSettings.scaleFactor;
                    x += finalSpacingX + emoji.GetWidth()*m_LastSettings.scaleFactor;

                    var uv = DataUtility.GetOuterUV(emoji.spriteList[0]);

                    //纹理坐标
                    m_TempUVs[0] = new Vector2(uv.x, uv.y);
                    ;
                    m_TempUVs[1] = new Vector2(uv.x, uv.w);
                    m_TempUVs[2] = new Vector2(uv.z, uv.w);
                    m_TempUVs[3] = new Vector2(uv.z, uv.y);

                    m_TempVertices[0] = new Vector3(v0x, v0y);
                    m_TempVertices[1] = new Vector3(v0x, v1y);
                    m_TempVertices[2] = new Vector3(v1x, v1y);
                    m_TempVertices[3] = new Vector3(v1x, v0y);

                    for (int j = 0; j < 4; ++j)
                    {
                        var uiVertext = UIVertexEx.simpleVert;
                        uiVertext.uv1 = m_TempUVs[j];
                        uiVertext.color = textInfo.color;
                        uiVertext.position = m_TempVertices[j];
                        uiVertext.emojiIndex = textInfo.ch;
                        if (emoji.animation && j == 0)
                        {
                            uiVertext.animation = true;
                        }
                        verts.Add(uiVertext);
                    }
                }

                if (textInfo.url != null)
                {
                    m_TempVertices[0] = new Vector3(v0x, -y - currentLineHeight);
                    m_TempVertices[1] = new Vector3(v0x, -y);
                    m_TempVertices[2] = new Vector3(v1x, -y);
                    m_TempVertices[3] = new Vector3(v1x, -y - currentLineHeight);

                    for (int j = 0; j < 4; ++j)
                    {
                        var uiVertext = UIVertexEx.simpleVert;
                        uiVertext.position = m_TempVertices[j];
                        uiVertext.url = textInfo.url;
                        uiVertext.urlIndex = textInfo.urlIndex;
                        verts.Add(uiVertext);
                    }
                }

                if (textInfo.underline)
                {
                    var dashSize = lineInfo.maxTextHeight;
                    if (textInfo.subscriptMode == 1)
                    {
                        dashSize = textInfo.fontSize;
                    }
                    GlyphInfo dash = GetGlyph('_', dashSize, textInfo.bold ? FontStyle.Bold : FontStyle.Normal);
                    if (dash == null)
                        continue;

                    float cx = (dash.u0.x + dash.u2.x)*0.5f;
                    m_TempUVs[0] = new Vector2(cx, dash.u0.y);
                    m_TempUVs[1] = new Vector2(cx, dash.u2.y);
                    m_TempUVs[2] = new Vector2(cx, dash.u2.y);
                    m_TempUVs[3] = new Vector2(cx, dash.u0.y);

                    v0y = dash.v0.y;
                    v1y = dash.v1.y;
                    if (textInfo.subscriptMode == 1)
                    {
                        v0y *= sizeShrinkage;
                        v1y *= sizeShrinkage;
                        v0y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) + textInfo.fontSize*0.075f)*
                               m_LastSettings.scaleFactor;
                        v1y -= ((1 - sizeShrinkage)*(m_Baseline - m_BaselineOffset) + textInfo.fontSize*0.075f)*
                               m_LastSettings.scaleFactor;
                    }
                    v0y = (-y - yOffset + v0y);
                    v1y = (-y - yOffset + v1y);

                    m_TempVertices[0] = new Vector3(prevX, v0y);
                    m_TempVertices[1] = new Vector3(prevX, v1y);
                    m_TempVertices[2] = new Vector3(x, v1y);
                    m_TempVertices[3] = new Vector3(x, v0y);

                    for (int j = 0; j < 4; ++j)
                    {
                        var uiVertext = UIVertexEx.simpleVert;
                        uiVertext.uv0 = m_TempUVs[j];
                        uiVertext.color = textInfo.color;
                        uiVertext.position = m_TempVertices[j];
                        verts.Add(uiVertext);
                    }
                }
            }

            if (!IsLeftPivot(m_LastSettings.textAnchor) && indexOffset < verts.Count)
            {
                Align(indexOffset, x - finalSpacingX);
            }

            var widthOffset = m_LastSettings.generationExtents.x*m_LastSettings.pivot.x*m_LastSettings.scaleFactor;
            var heightOffset = (m_LastSettings.generationExtents.y -
                                m_LastSettings.generationExtents.y*m_LastSettings.pivot.y)*m_LastSettings.scaleFactor;
            Vector2 anchorPivot = GetTextAnchorPivot(m_LastSettings.textAnchor);
            float pivotOffsetY =
                Mathf.Lerp(m_PrintedSize.y - m_LastSettings.generationExtents.y*m_LastSettings.scaleFactor, 0f,
                    anchorPivot.y);
            for (int i = 0; i < verts.Count; ++i)
            {
                var uiVertex = m_Verts[i];
                uiVertex.position.x -= widthOffset;
                uiVertex.position.y += heightOffset + pivotOffsetY;
                verts[i] = uiVertex;
            }

            m_TextElementList.Clear();
            m_LineInfoList.Clear();
            return true;
        }

        private float GetPrintLineHeight(int maxFontHeight, int maxEmojiHeight, int defaultFontHeight, ref bool result)
        {
            int currentVersion = _version;
            var height = GetLineHeight(maxFontHeight, maxEmojiHeight, defaultFontHeight);
            if (currentVersion != _version)
            {
                result = false;
                return 0;
            }

            result = true;
            return height;
        }

        private float GetLineHeight(int maxFontHeight, int maxEmojiHeight, int defaultFontHeight)
        {
            var height = 0f;
            if (maxFontHeight < 0 && maxEmojiHeight > 0)
            {
                height = maxEmojiHeight*m_LastSettings.scaleFactor +
                         0.135f* m_LastSettings.scaleFactor*m_LastSettings.lineSpacing*defaultFontHeight;
                emojiBaseline = maxEmojiHeight;
                return height;
            }

            if (maxFontHeight < 0)
            {
                maxFontHeight = defaultFontHeight;
            }

            UpdateBaseLine(maxFontHeight);

            if (m_Baseline < maxEmojiHeight)
            {
                height = maxEmojiHeight*m_LastSettings.scaleFactor +
                         0.15f*m_LastSettings.scaleFactor*m_LastSettings.lineSpacing*maxFontHeight +
                         m_LastSettings.scaleFactor*m_LastSettings.lineSpacing*
                         (maxFontHeight - m_Baseline + m_BaselineOffset);
                m_Baseline = maxEmojiHeight + m_BaselineOffset;
                emojiBaseline = maxEmojiHeight;
            }
            else
            {
                height = lineFactor*maxFontHeight;
                emojiBaseline = m_Baseline - m_BaselineOffset;
            }

            return height;
        }

        private FontStyle GetGlyphFontStyle(bool italic, bool bold)
        {
            FontStyle fontStyle = m_LastSettings.fontStyle;
            if (italic && bold)
            {
                fontStyle = FontStyle.BoldAndItalic;
            }
            else
            {
                if (italic)
                {
                    fontStyle = FontStyle.Italic;
                }
                else if (bold)
                {
                    fontStyle = FontStyle.Bold;
                }
            }
            return fontStyle;
        }

        private float GetGlyphWidth(char ch, int fontSize, int sub, float spacingX, FontStyle fontStyle)
        {
            float w = GetGlyphWidth(ch, fontSize, fontStyle);
            if (w <= 0f)
            {
                return float.MinValue;
            }

            float glyphWidth = spacingX + w;
            if (sub != 0)
            {
                glyphWidth *= sizeShrinkage;
            }
            return glyphWidth;
        }

        /// <summary>
        /// 不复用WrapText,由于编辑器启动的时候，WrapText内部存储字符数据的List会无缘无故的变为0
        /// </summary>
        /// <returns></returns>
        public Vector2 CalculateWrapSize(string text, int fontSize, float regionWidth)
        {
            Vector2 size = Vector2.zero;
            int sub = 0;
            bool bold = false;
            bool italic = false;
            bool underline = false;
            bool strikethrough = false;
            int curFontSize = fontSize;
            string url = null;

            float x = 0f, y = 0f, maxX = 0f;
            int emojiSymbol = 0;
            int urlIndex = 0;

            int maxEmojiHeight = int.MinValue;
            int maxTextHeight = int.MinValue;

            for (int textLength = text.Length, index = 0; index < textLength; ++index)
            {
                char ch = text[index];
                if (m_LastSettings.richText &&
                    ParseSymbol(text, m_LastSettings, ref index, null, ref sub, ref bold, ref italic, ref underline,
                        ref strikethrough, fontSize, ref curFontSize, ref emojiSymbol, ref url, ref urlIndex))
                {
                    --index;
                    if (emojiSymbol != int.MinValue)
                    {
                        var emojiConfigIndex = GetEmojiConfigIndex(s_EmojiConfigList, emojiSymbol);
                        var emojiSymbolElement = GetEmoji(s_EmojiConfigList, emojiConfigIndex, emojiSymbol);
                        var emojiHeight = emojiSymbolElement.GetHeight();
                        var emojiWidth = finalSpacingX + emojiSymbolElement.GetWidth() * m_LastSettings.scaleFactor;

                        if (x + emojiWidth > regionWidth)
                        {
                            if (x > maxX) maxX = x;
                            x = emojiWidth;
                            y += GetLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize);
                            maxTextHeight = int.MinValue;
                            maxEmojiHeight = emojiHeight;
                        }
                        else
                        {
                            x += emojiWidth;
                            if (emojiHeight > maxEmojiHeight)
                            {
                                maxEmojiHeight = (int)emojiHeight;
                            }
                        }
                    }
                    continue;
                }

                if (ch == '\n')
                {
                    if (x > maxX) maxX = x;
                    x = 0f;

                    if (maxTextHeight == int.MinValue)
                    {
                        maxTextHeight = curFontSize;
                    }

                    y += GetLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize);
                    maxEmojiHeight = int.MinValue;
                    maxTextHeight = int.MinValue;
                    continue;
                }

                if (ch < ' ')
                {
                    continue;
                }

                var fontStyle = GetGlyphFontStyle(italic, bold);
                Prepare(ch.ToString(), curFontSize, fontStyle);

                float glyphWidth = GetGlyphWidth(ch, curFontSize, sub, finalSpacingX, fontStyle);
                if (glyphWidth <= 0)
                {
                    continue;
                }

                if (x + glyphWidth > regionWidth)
                {
                    if (x > maxX) maxX = x;
                    x = glyphWidth;
                    y += GetLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize);
                    maxTextHeight = curFontSize;
                    maxEmojiHeight = int.MinValue;
                }
                else
                {
                    x += glyphWidth;
                    if (curFontSize > maxTextHeight)
                    {
                        maxTextHeight = curFontSize;
                    }
                }
            }

            if (text.Length > 0 && text[text.Length - 1] != '\n')
            {
                if (x > maxX) maxX = x;
                y += GetLineHeight(maxTextHeight, maxEmojiHeight, m_LastSettings.fontSize);
            }

            size.x = maxX;
            size.y = y;
            return size;
        }

        private void UpdateBaseLine(int fontSize)
        {
            var finalSize = fontSize;
            var dynamicFont = m_LastSettings.font;

            if (dynamicFont != null)
            {
                dynamicFont.RequestCharactersInTexture("X_-", finalSize, m_LastSettings.fontStyle);
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_7_1
                if (!dynamicFont.GetCharacterInfo('X', out m_TempChar, finalSize, m_LastSettings.fontStyle) ||
                    m_TempChar.vert.height == 0f)
                {
                    dynamicFont.RequestCharactersInTexture("A", finalSize, m_LastSettings.fontStyle);
                    if (!dynamicFont.GetCharacterInfo('A', out m_TempChar, finalSize, m_LastSettings.fontStyle))
                    {
                        m_Baseline = 0f;
                    }
                }

                float y0 = m_TempChar.vert.yMax;
                float y1 = m_TempChar.vert.yMin;
                m_BaselineOffset = y0;
#else
            if (!dynamicFont.GetCharacterInfo('X', out m_TempChar, finalSize, m_LastSettings.fontStyle) || m_TempChar.maxY == 0f)
            {
                dynamicFont.RequestCharactersInTexture("A", finalSize, m_LastSettings.fontStyle);
                if (!dynamicFont.GetCharacterInfo('A', out m_TempChar, finalSize, m_LastSettings.fontStyle))
                {
                    m_Baseline = 0f;
                }
            }

			float y0 = m_TempChar.maxY;
			float y1 = m_TempChar.minY;
            m_BaselineOffset = 0;
#endif
                m_Baseline = y0 + (finalSize - y0 + y1)*0.5f;
            }
        }


        public GlyphInfo GetGlyph(int ch, int fontSize, FontStyle fontStyle)
        {
            var dynamicFont = m_LastSettings.font;
            if (dynamicFont != null)
            {
                if (dynamicFont.GetCharacterInfo((char) ch, out m_TempChar, fontSize, fontStyle))
                {
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
                    m_GlyphInfo.v0.x = m_TempChar.vert.xMin;
                    m_GlyphInfo.v1.x = m_GlyphInfo.v0.x + m_TempChar.vert.width;
                    m_GlyphInfo.v0.y = m_TempChar.vert.yMax - m_Baseline;
                    m_GlyphInfo.v1.y = m_GlyphInfo.v0.y - m_TempChar.vert.height;

                    m_GlyphInfo.u0.x = m_TempChar.uv.xMin;
                    m_GlyphInfo.u0.y = m_TempChar.uv.yMin;

                    m_GlyphInfo.u2.x = m_TempChar.uv.xMax;
                    m_GlyphInfo.u2.y = m_TempChar.uv.yMax;

                    if (m_TempChar.flipped)
                    {
                        m_GlyphInfo.u1 = new Vector2(m_GlyphInfo.u2.x, m_GlyphInfo.u0.y);
                        m_GlyphInfo.u3 = new Vector2(m_GlyphInfo.u0.x, m_GlyphInfo.u2.y);
                    }
                    else
                    {
                        m_GlyphInfo.u1 = new Vector2(m_GlyphInfo.u0.x, m_GlyphInfo.u2.y);
                        m_GlyphInfo.u3 = new Vector2(m_GlyphInfo.u2.x, m_GlyphInfo.u0.y);
                    }

                    m_GlyphInfo.advance = m_TempChar.width;
#else
				m_GlyphInfo.v0.x = m_TempChar.minX;
				m_GlyphInfo.v1.x = m_TempChar.maxX;

				m_GlyphInfo.v0.y = m_TempChar.maxY - m_Baseline;
				m_GlyphInfo.v1.y = m_TempChar.minY - m_Baseline;

				m_GlyphInfo.u0 = m_TempChar.uvTopLeft;
				m_GlyphInfo.u1 = m_TempChar.uvBottomLeft;
				m_GlyphInfo.u2 = m_TempChar.uvBottomRight;
				m_GlyphInfo.u3 = m_TempChar.uvTopRight;

				m_GlyphInfo.advance = m_TempChar.advance;
#endif

                    m_GlyphInfo.v0.x = Mathf.Round(m_GlyphInfo.v0.x);
                    m_GlyphInfo.v0.y = Mathf.Round(m_GlyphInfo.v0.y);
                    m_GlyphInfo.v1.x = Mathf.Round(m_GlyphInfo.v1.x);
                    m_GlyphInfo.v1.y = Mathf.Round(m_GlyphInfo.v1.y);

                    float pd = m_LastSettings.scaleFactor;
                    if (pd != 1f)
                    {
                        m_GlyphInfo.v0 *= pd;
                        m_GlyphInfo.v1 *= pd;
                        m_GlyphInfo.advance *= pd;
                    }
                    return m_GlyphInfo;
                }
            }
            return null;
        }

        public void Align(int indexOffset, float printedWidth)
        {
            var rectWidth = m_LastSettings.generationExtents.x;
            switch (m_LastSettings.textAnchor)
            {
                case TextAnchor.LowerRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                {
                    float padding = rectWidth*m_LastSettings.scaleFactor - printedWidth;

                    for (int i = indexOffset; i < verts.Count; ++i)
                    {
                        var uiVertex = m_Verts[i];
                        uiVertex.position.x += padding;
                        verts[i] = uiVertex;
                    }
                    break;
                }

                case TextAnchor.UpperCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.LowerCenter:
                {
                    float padding = (rectWidth*m_LastSettings.scaleFactor - printedWidth)*0.5f;
                    int diff = Mathf.RoundToInt(rectWidth - printedWidth);
                    int intWidth = Mathf.RoundToInt(rectWidth);

                    bool oddDiff = (diff & 1) == 1;
                    bool oddWidth = (intWidth & 1) == 1;
                    if ((oddDiff && !oddWidth) || (!oddDiff && oddWidth))
                        padding += 0.5f;

                    for (int i = indexOffset; i < verts.Count; ++i)
                    {
                        var uiVertex = m_Verts[i];
                        uiVertex.position.x += padding;
                        verts[i] = uiVertex;
                    }
                    break;
                }
            }
        }

        public static bool IsLeftPivot(TextAnchor anchor)
        {
            return anchor == TextAnchor.LowerLeft || anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft;
        }

        public static Vector2 GetTextAnchorPivot(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                    return new Vector2(0, 0);
                case TextAnchor.LowerCenter:
                    return new Vector2(0.5f, 0);
                case TextAnchor.LowerRight:
                    return new Vector2(1, 0);
                case TextAnchor.MiddleLeft:
                    return new Vector2(0, 0.5f);
                case TextAnchor.MiddleCenter:
                    return new Vector2(0.5f, 0.5f);
                case TextAnchor.MiddleRight:
                    return new Vector2(1, 0.5f);
                case TextAnchor.UpperLeft:
                    return new Vector2(0, 1);
                case TextAnchor.UpperCenter:
                    return new Vector2(0.5f, 1);
                case TextAnchor.UpperRight:
                    return new Vector2(1, 1);
                default:
                    return Vector2.zero;
            }
        }

        private void Prepare(string text, int fontSize, FontStyle fontStyle)
        {
            if (m_LastSettings.font != null)
                m_LastSettings.font.RequestCharactersInTexture(text, fontSize, fontStyle);
        }

        private float GetGlyphWidth(int ch, int fontSize, FontStyle fontStyle)
        {
            if (m_LastSettings.font != null)
            {
                if (m_LastSettings.font.GetCharacterInfo((char) ch, out m_TempChar, fontSize, fontStyle))
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_7_1
                    return m_TempChar.width*m_LastSettings.scaleFactor;
#else
                return m_TempChar.advance * m_LastSettings.scaleFactor;
#endif
            }
            return 0f;
        }

        private bool IsSpace(int ch)
        {
            return (ch == ' ' || ch == 0x200a || ch == 0x200b);
        }


        /// <summary>
        /// Parse a RrGgBb color encoded in the string.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static Color ParseColor24(string text, int offset)
        {
            int r = (NGUIMath.HexToDecimal(text[offset]) << 4) | NGUIMath.HexToDecimal(text[offset + 1]);
            int g = (NGUIMath.HexToDecimal(text[offset + 2]) << 4) | NGUIMath.HexToDecimal(text[offset + 3]);
            int b = (NGUIMath.HexToDecimal(text[offset + 4]) << 4) | NGUIMath.HexToDecimal(text[offset + 5]);
            float f = 1f/255f;
            return new Color(f*r, f*g, f*b);
        }

        /// <summary>
        /// Parse a RrGgBbAa color encoded in the string.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static Color ParseColor32(string text, int offset)
        {
            int r = (NGUIMath.HexToDecimal(text[offset]) << 4) | NGUIMath.HexToDecimal(text[offset + 1]);
            int g = (NGUIMath.HexToDecimal(text[offset + 2]) << 4) | NGUIMath.HexToDecimal(text[offset + 3]);
            int b = (NGUIMath.HexToDecimal(text[offset + 4]) << 4) | NGUIMath.HexToDecimal(text[offset + 5]);
            int a = (NGUIMath.HexToDecimal(text[offset + 6]) << 4) | NGUIMath.HexToDecimal(text[offset + 7]);
            float f = 1f/255f;
            return new Color(f*r, f*g, f*b, f*a);
        }

        /// <summary>
        /// The reverse of ParseColor24 -- encodes a color in RrGgBb format.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static string EncodeColor24(Color c)
        {
            int i = 0xFFFFFF & (NGUIMath.ColorToInt(c) >> 8);
            return NGUIMath.DecimalToHex24(i);
        }

        /// <summary>
        /// The reverse of ParseColor32 -- encodes a color in RrGgBb format.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static string EncodeColor32(Color c)
        {
            int i = NGUIMath.ColorToInt(c);
            return NGUIMath.DecimalToHex32(i);
        }


        public static int GetEmojiConfigIndex(List<EmojiConfig> emojiConfigList, int emojiIndex)
        {
            if (emojiConfigList != null || emojiIndex < 0)
            {
                var totalCount = 0;
                for (int i = 0, count = emojiConfigList.Count; i < count; ++i)
                {
                    var emojiConfig = emojiConfigList[i];
                    if (emojiConfig.target == null || emojiConfig.emojiList == null)
                    {
                        continue;
                    }

                    if (emojiIndex - totalCount >= emojiConfig.emojiList.Count)
                    {
                        totalCount = emojiConfig.emojiList.Count;
                        continue;
                    }

                    var emoji = emojiConfig.emojiList[emojiIndex - totalCount];
                    if (emoji.spriteList != null && emoji.spriteList.Count > 0 && emoji.spriteList[0] != null)
                    {
                        return i;
                    }
                    return -1;
                }
            }
            return -1;
        }

        public static EmojiInfo GetEmoji(List<EmojiConfig> emojiConfigList, int emojiConfigIndex, int emojiIndex)
        {
            var totalCount = 0;
            for (int i = 0; i <= emojiConfigIndex; ++i)
            {
                var emojiConfig = emojiConfigList[i];
                if (emojiConfig.target == null || emojiConfig.emojiList == null)
                {
                    continue;
                }

                if (emojiIndex - totalCount >= emojiConfig.emojiList.Count)
                {
                    totalCount = emojiConfig.emojiList.Count;
                    continue;
                }

                return emojiConfigList[emojiConfigIndex].emojiList[emojiIndex - totalCount];
            }

            return default(EmojiInfo);
        }

        public static int GetEmojiIndex(List<EmojiConfig> emojiConfigList, int emojiConfigIndex, int emojiIndex)
        {
            var totalCount = 0;
            for (int i = 0; i <= emojiConfigIndex; ++i)
            {
                var emojiConfig = emojiConfigList[i];
                if (emojiConfig.target == null || emojiConfig.emojiList == null)
                {
                    continue;
                }

                if (emojiIndex - totalCount >= emojiConfig.emojiList.Count)
                {
                    totalCount = emojiConfig.emojiList.Count;
                    continue;
                }

                return emojiIndex - totalCount;
            }

            return -1;
        }

        public static bool ParseSymbol(string text, TextGenerationSettingsEx setting, ref int index, List<Color> colors,
            ref int sub, ref bool bold, ref bool italic, ref bool underline, ref bool strike, int defaultFontSize,
            ref int newFontSize, ref int emojiSymbol, ref string url, ref int urlIndex)
        {
            int length = text.Length;

            if (text[index] == '#' && setting.parseEmoji)
            {
                int endIndex = index;
                for (int i = index + 1; i < length; ++i)
                {
                    if (text[i] < '0' || text[i] > '9')
                    {
                        endIndex = i - 1;
                        break;
                    }
                    ++endIndex;
                }

                if (endIndex < index + 1)
                {
                    emojiSymbol = int.MinValue;
                    return false;
                }

                var emojiSymbolSub = text.Substring(index + 1, endIndex - index);
                var emojiIndex = 0;
                if (int.TryParse(emojiSymbolSub, out emojiIndex))
                {
                    if (GetEmojiConfigIndex(s_EmojiConfigList, emojiIndex) >= 0)
                    {
                        emojiSymbol = emojiIndex;
                        index += endIndex + 1 - index;
                        return true;
                    }
                }

                emojiSymbol = int.MinValue;
                return false;
            }
            emojiSymbol = int.MinValue;

            if (index + 3 > length || text[index] != '<') return false;

            if (text[index + 2] == '>')
            {
                if (text[index + 1] == '/')
                {
                    if (colors != null && colors.Count > 1)
                        colors.RemoveAt(colors.Count - 1);
                    index += 3;
                    return true;
                }

                string sub3 = text.Substring(index, 3);

                switch (sub3)
                {
                    case "<b>":
                        if (!setting.parseBold)
                            return false;
                        bold = true;
                        index += 3;
                        return true;

                    case "<i>":
                        if (!setting.parseItatic)
                            return false;
                        italic = true;
                        index += 3;
                        return true;

                    case "<u>":
                        if (!setting.parseUnderline)
                            return false;
                        underline = true;
                        index += 3;
                        return true;

                    case "<s>":
                        if (!setting.parseStrikethrough)
                            return false;
                        strike = true;
                        index += 3;
                        return true;
                }
            }

            if (index + 4 > length) return false;

            if (text[index + 3] == '>')
            {
                string sub4 = text.Substring(index, 4);

                switch (sub4)
                {
                    case "</b>":
                        bold = false;
                        index += 4;
                        return true;

                    case "</i>":
                        italic = false;
                        index += 4;
                        return true;

                    case "</u>":
                        underline = false;
                        index += 4;
                        return true;

                    case "</s>":
                        strike = false;
                        index += 4;
                        return true;


                    case "</a>":
                        url = null;
                        index += 4;
                        return true;
                }
            }

            if (text[index + 1] == 'a' && text[index + 2] == '=' && setting.parseUrl)
            {
                int closingBracket = text.IndexOf('>', index + 2);

                if (closingBracket != -1)
                {
                    ++urlIndex;
                    url = text.Substring(index + 3, closingBracket - index - 3);
                    index = closingBracket + 1;
                    return true;
                }
            }

            if (index + 5 > length) return false;

            if (text[index + 4] == '>')
            {
                string sub5 = text.Substring(index, 5);

                switch (sub5)
                {
                    case "<sub>":
                        if (!setting.parseSub)
                            return false;
                        sub = 1;
                        index += 5;
                        return true;

                    case "<sup>":
                        if (!setting.parseSup)
                            return false;
                        sub = 2;
                        index += 5;
                        return true;
                }
            }

            if (index + 6 > length) return false;

            if (text[index + 5] == '>')
            {
                string sub6 = text.Substring(index, 6);

                switch (sub6)
                {
                    case "</sub>":
                        if (sub == 1)
                            sub = 0;
                        index += 6;
                        return true;

                    case "</sup>":
                        if (sub == 2)
                            sub = 0;
                        index += 6;
                        return true;
                }
            }

            if (index + 7 > length) return false;

            if (text[index + 6] == '>')
            {
                string sub7 = text.Substring(index, 7);
                switch (sub7)
                {
                    case "</size>":
                        index += 7;
                        newFontSize = defaultFontSize;
                        return true;
                }
            }

            if (index + 8 > length) return false;

            var subSize = text.Substring(index, 6);
            if (subSize == "<size=" && setting.parseSize)
            {
                var sb = new StringBuilder();
                for (int i = index + 6, textLength = text.Length; i < textLength; ++i)
                {
                    if (text[i] == '>')
                    {
                        break;
                    }
                    sb.Append(text[i]);
                }
                int fontSize;
                if (int.TryParse(sb.ToString(), out fontSize) && fontSize > 0)
                {
                    newFontSize = fontSize;
                    index += 7 + sb.Length;
                    return true;
                }
            }

            if (text[index + 7] == '>' && setting.parseColor)
            {
                Color c = ParseColor24(text, index + 1);

                if (EncodeColor24(c) != text.Substring(index + 1, 6).ToUpper())
                    return false;

                if (colors != null)
                {
                    c.a = colors.Count > 0 ? colors[colors.Count - 1].a : 1;
                    colors.Add(c);
                }
                index += 8;
                return true;
            }

            if (index + 10 > length) return false;

            if (text[index + 9] == '>' && setting.parseColor)
            {
                Color c = ParseColor32(text, index + 1);
                if (EncodeColor32(c) != text.Substring(index + 1, 8).ToUpper())
                    return false;

                if (colors != null)
                {
                    colors.Add(c);
                }
                index += 10;
                return true;
            }
            return false;
        }
    }

    public static class NGUIMath
    {
        /// <summary>
        /// Convert a hexadecimal character to its decimal value.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static int HexToDecimal(char ch)
        {
            switch (ch)
            {
                case '0':
                    return 0x0;
                case '1':
                    return 0x1;
                case '2':
                    return 0x2;
                case '3':
                    return 0x3;
                case '4':
                    return 0x4;
                case '5':
                    return 0x5;
                case '6':
                    return 0x6;
                case '7':
                    return 0x7;
                case '8':
                    return 0x8;
                case '9':
                    return 0x9;
                case 'a':
                case 'A':
                    return 0xA;
                case 'b':
                case 'B':
                    return 0xB;
                case 'c':
                case 'C':
                    return 0xC;
                case 'd':
                case 'D':
                    return 0xD;
                case 'e':
                case 'E':
                    return 0xE;
                case 'f':
                case 'F':
                    return 0xF;
            }
            return 0xF;
        }

        /// <summary>
        /// Convert a decimal value to its hex representation.
        /// It's coded because num.ToString("X6") syntax doesn't seem to be supported by Unity's Flash. It just silently crashes.
        /// string.Format("{0,6:X}", num).Replace(' ', '0') doesn't work either. It returns the format string, not the formatted value.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static string DecimalToHex24(int num)
        {
            num &= 0xFFFFFF;
#if UNITY_FLASH
		StringBuilder sb = new StringBuilder();
		sb.Append(DecimalToHexChar((num >> 20) & 0xF));
		sb.Append(DecimalToHexChar((num >> 16) & 0xF));
		sb.Append(DecimalToHexChar((num >> 12) & 0xF));
		sb.Append(DecimalToHexChar((num >> 8) & 0xF));
		sb.Append(DecimalToHexChar((num >> 4) & 0xF));
		sb.Append(DecimalToHexChar(num & 0xF));
		return sb.ToString();
#else
            return num.ToString("X6");
#endif
        }

        /// <summary>
        /// Convert a decimal value to its hex representation.
        /// It's coded because num.ToString("X6") syntax doesn't seem to be supported by Unity's Flash. It just silently crashes.
        /// string.Format("{0,6:X}", num).Replace(' ', '0') doesn't work either. It returns the format string, not the formatted value.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static string DecimalToHex32(int num)
        {
#if UNITY_FLASH
		StringBuilder sb = new StringBuilder();
		sb.Append(DecimalToHexChar((num >> 28) & 0xF));
		sb.Append(DecimalToHexChar((num >> 24) & 0xF));
		sb.Append(DecimalToHexChar((num >> 20) & 0xF));
		sb.Append(DecimalToHexChar((num >> 16) & 0xF));
		sb.Append(DecimalToHexChar((num >> 12) & 0xF));
		sb.Append(DecimalToHexChar((num >> 8) & 0xF));
		sb.Append(DecimalToHexChar((num >> 4) & 0xF));
		sb.Append(DecimalToHexChar(num & 0xF));
		return sb.ToString();
#else
            return num.ToString("X8");
#endif
        }

        /// <summary>
        /// Convert the specified color to RGBA32 integer format.
        /// </summary>

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerStepThrough]
        public static int ColorToInt(Color c)
        {
            int retVal = 0;
            retVal |= Mathf.RoundToInt(c.r*255f) << 24;
            retVal |= Mathf.RoundToInt(c.g*255f) << 16;
            retVal |= Mathf.RoundToInt(c.b*255f) << 8;
            retVal |= Mathf.RoundToInt(c.a*255f);
            return retVal;
        }
    }
}