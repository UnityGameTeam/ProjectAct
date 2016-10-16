using System.Collections.Generic;

public struct Rectangle
{
    public static List<Rectangle> s_CarveRectangles = new List<Rectangle>(4); 

    public float x { get; set; }
    public float y { get; set; }
    public float halfWidth { get; set; }
    public float halfHeight { get; set; }

    public Rectangle(float x, float y, float halfWidth, float halfHeight)
    {
        this.x      = x;
        this.y      = y;
        this.halfWidth = halfWidth;
        this.halfHeight = halfHeight;
    }

    public bool IsInner(Rectangle rect)
    {
        return (x + halfWidth <= rect.x + rect.halfWidth) && (x - halfWidth >= rect.x - rect.halfWidth) &&
               (y + halfHeight <= rect.y + rect.halfHeight) && (y - halfHeight >= rect.y - rect.halfHeight);
    }

    public List<Rectangle> Carve(Rectangle rect)
    {
        s_CarveRectangles.Clear();

        var bottomY = y - halfHeight;
        var topY = y + halfHeight;
        var leftX = x - halfWidth;
        var rightX = x + halfWidth;

        var carveVertital = topY > rect.y && bottomY < rect.y;
        var caveHorizontal = rightX > rect.x && leftX < rect.x;

        if (caveHorizontal && carveVertital)
        {
            var leftHalfWidth = (rect.x - leftX) * 0.5f;
            var rightHalfWidth = (rightX - rect.x) * 0.5f;
            var topHalfHeight = (topY - rect.y) * 0.5f;
            var bottomHalfHeight = (rect.y - bottomY) * 0.5f;

            s_CarveRectangles.Add(new Rectangle(rect.x + rightHalfWidth, rect.y + topHalfHeight, rightHalfWidth, topHalfHeight));
            s_CarveRectangles.Add(new Rectangle(rect.x - leftHalfWidth, rect.y + topHalfHeight, leftHalfWidth, topHalfHeight));
            s_CarveRectangles.Add(new Rectangle(rect.x - leftHalfWidth, rect.y - bottomHalfHeight, leftHalfWidth, bottomHalfHeight));
            s_CarveRectangles.Add(new Rectangle(rect.x + rightHalfWidth, rect.y - bottomHalfHeight, rightHalfWidth, bottomHalfHeight));
        }
        else if(caveHorizontal && !carveVertital)
        {
            var leftHalfWidth = (rect.x - leftX) * 0.5f;
            var rightHalfWidth = (rightX - rect.x) * 0.5f;
            s_CarveRectangles.Add(new Rectangle(rect.x - leftHalfWidth, y, leftHalfWidth, halfHeight));
            s_CarveRectangles.Add(new Rectangle(rect.x + rightHalfWidth, y, rightHalfWidth, halfHeight));
        }
        else if (!caveHorizontal && carveVertital)
        {
            var topHalfHeight = (topY - rect.y)*0.5f;
            var bottomHalfHeight = (rect.y - bottomY) * 0.5f;
            s_CarveRectangles.Add(new Rectangle(x, rect.y + topHalfHeight, halfWidth, topHalfHeight));
            s_CarveRectangles.Add(new Rectangle(x, rect.y - bottomHalfHeight, halfWidth, bottomHalfHeight));
        }

        return s_CarveRectangles;
    } 
}
