using UnityEngine;

namespace UguiExtensions
{
    public struct TextGenerationSettingsEx
    {
        public Font font;
        public Color color;
        public int fontSize;
        public float SpacingX;
        public float lineSpacing;
        public bool richText;
        public float scaleFactor;
        public FontStyle fontStyle;
        public TextAnchor textAnchor;
        public bool resizeTextForBestFit;
        public int resizeTextMinSize;
        public int resizeTextMaxSize;
        public VerticalWrapMode verticalOverflow;
        public HorizontalWrapMode horizontalOverflow;
        public Vector2 generationExtents;
        public Vector2 pivot;
        public bool elipsizeEnd;
        public bool parseEmoji;
        public bool parseColor;
        public bool parseBold;
        public bool parseItatic;
        public bool parseUnderline;
        public bool parseStrikethrough;
        public bool parseUrl;
        public bool parseSub;
        public bool parseSup;
        public bool parseSize;

        private bool CompareColors(Color left, Color right)
        {
            if (Mathf.Approximately(left.r, right.r) && Mathf.Approximately(left.g, right.g) && Mathf.Approximately(left.b, right.b))
                return Mathf.Approximately(left.a, right.a);
            return false;
        }

        private bool CompareVector2(Vector2 left, Vector2 right)
        {
            if (Mathf.Approximately(left.x, right.x))
                return Mathf.Approximately(left.y, right.y);
            return false;
        }

        public bool Equals(TextGenerationSettingsEx other)
        {
            if (this.CompareColors(this.color, other.color) && this.fontSize == other.fontSize && Mathf.Approximately(this.SpacingX, other.SpacingX) &&
                (Mathf.Approximately(this.scaleFactor, other.scaleFactor) && (elipsizeEnd == other.elipsizeEnd) &&
                (parseSub == other.parseSub && parseBold == other.parseBold && parseColor == other.parseColor && parseEmoji == other.parseEmoji &&
                parseItatic == other.parseItatic && parseSize == other.parseSize && parseStrikethrough == other.parseStrikethrough && parseSup == other.parseSup &&
                parseUrl == other.parseUrl && parseUnderline == other.parseUnderline) &&
                 this.resizeTextMinSize == other.resizeTextMinSize) &&
                (this.resizeTextMaxSize == other.resizeTextMaxSize &&
                 Mathf.Approximately(this.lineSpacing, other.lineSpacing) &&
                 (this.fontStyle == other.fontStyle && this.richText == other.richText)) &&
                (this.textAnchor == other.textAnchor && this.resizeTextForBestFit == other.resizeTextForBestFit &&
                 (this.resizeTextMinSize == other.resizeTextMinSize && this.resizeTextMaxSize == other.resizeTextMaxSize) &&
                 (this.resizeTextForBestFit == other.resizeTextForBestFit &&
                  (this.horizontalOverflow == other.horizontalOverflow && this.verticalOverflow == other.verticalOverflow))) &&
                (this.CompareVector2(this.generationExtents, other.generationExtents) &&
                 this.CompareVector2(this.pivot, other.pivot)))
                return this.font == other.font;
            return false;
        }
    }
}
