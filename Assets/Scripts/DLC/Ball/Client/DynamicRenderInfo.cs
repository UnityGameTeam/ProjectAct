using UnityEngine;

public class DynamicRenderInfo
{
    private Vector3[] m_Vertices;   //动态生成的Mesh的顶点组成
    private Vector2[] m_UV;         //动态生成的Mesh的UV组成
    private int[]     m_Triangles;  //动态生成的Mesh的三角形组成

    public Mesh RenderMesh { get; protected set; } //动态生成的Mesh对象

    /// <summary>
    /// 绘制刺球
    /// </summary>
    public void DrawSpikyBall()
    {
        int maxSide         = 42 * 4;                    //组成刺球的圆的边数
        m_UV                = new Vector2[maxSide + 1];  
        m_UV[maxSide]       = new Vector2(0.5f, 0.3f);
        m_Triangles         = new int[maxSide * 3];
        m_Vertices          = new Vector3[maxSide + 1];
        m_Vertices[maxSide] = Vector3.zero;

        for (int curSide = 0; curSide < maxSide; ++curSide)
        {
            m_Vertices[curSide]          = DynamicRenderData.GetRadianVector(curSide, maxSide);
            m_UV[curSide]                = new Vector2(m_Vertices[curSide].y * 0.25f + m_UV[maxSide].x, 0.3f);
            m_Triangles[curSide * 3]     = curSide;
            m_Triangles[curSide * 3 + 1] = curSide + 1;
            m_Triangles[curSide * 3 + 2] = maxSide;
        }
        m_Triangles[(maxSide - 1) * 3 + 1] = 0;

        //间距将圆上的顶点向外突出一些
        for (int i = 0; i < maxSide; i += 4)
        {
            m_Vertices[i + 2] = m_Vertices[i + 2] * 1.17f;
            m_Vertices[i + 3] = m_Vertices[i + 3] * 1.17f;
            m_UV[i + 2] = new Vector2(m_Vertices[i + 2].y * 0.25f + m_UV[maxSide].x, 0.3f);
            m_UV[i + 3] = new Vector2(m_Vertices[i + 3].y * 0.25f + m_UV[maxSide].x, 0.3f);
        }

        RenderMesh           = new Mesh();
        RenderMesh.vertices  = m_Vertices;
        RenderMesh.uv        = m_UV;
        RenderMesh.triangles = m_Triangles;
    }

    /// <summary>
    /// 绘制带有Icon的圆,Side是组成圆的边数
    /// </summary>
    public void DrawIconCircle(int side)
    {
        m_UV             = new Vector2[side + 1];
        m_UV[side]       = new Vector2(0.5f, 0.5f);
        m_Triangles      = new int[side * 3];
        m_Vertices       = new Vector3[side + 1];
        m_Vertices[side] = Vector3.zero;

        for (int curSide = 0; curSide < side; ++curSide)
        {
            m_Vertices[curSide]          = DynamicRenderData.GetRadianVector(curSide, side);
            m_UV[curSide]                = new Vector2(m_Vertices[curSide].x * 0.5f + 0.5f, m_Vertices[curSide].y * 0.5f + 0.5f);
            m_Triangles[curSide * 3]     = curSide;
            m_Triangles[curSide * 3 + 1] = curSide + 1;
            m_Triangles[curSide * 3 + 2] = side;
        }
        m_Triangles[(side - 1) * 3 + 1] = 0;

        RenderMesh           = new Mesh();
        RenderMesh.vertices  = m_Vertices;
        RenderMesh.uv        = m_UV;
        RenderMesh.triangles = m_Triangles;
    }

    /// <summary>
    /// 绘制纯色的圆
    /// </summary>
    public void DrawNoIconCircle(int side, int color)
    {
        float y              = color / (float)byte.MaxValue;
        m_UV                 = new Vector2[side * 3 + 1];
        m_UV[side * 3]       = new Vector2(0.7f, y);
        m_Triangles          = new int[side * 3 * 3];
        m_Vertices           = new Vector3[side * 3 + 1];
        m_Vertices[side * 3] = Vector3.zero;

        for (int curSide = 0; curSide < side; ++curSide)
        {
            m_Vertices[curSide]                     = DynamicRenderData.GetRadianVector(curSide, side);
            m_Vertices[curSide + side]              = m_Vertices[curSide];
            m_Vertices[curSide + side * 2]          = m_Vertices[curSide] * 1.014f;
            m_UV[curSide]                           = new Vector2(m_Vertices[curSide].y * 0.25f + m_UV[side * 3].x, y - m_Vertices[curSide].y / 50f);
            m_UV[curSide + side]                    = new Vector2(0.3f, y);
            m_UV[curSide + side * 2]                = new Vector2(0.3f, y);
            m_Triangles[curSide * 3]                = curSide;
            m_Triangles[curSide * 3 + 1]            = (curSide + 1) % side;
            m_Triangles[curSide * 3 + 2]            = side * 3;
            m_Triangles[curSide * 3 + side * 3]     = side + curSide;
            m_Triangles[curSide * 3 + side * 3 + 1] = side * 2 + curSide;
            m_Triangles[curSide * 3 + side * 3 + 2] = side + (curSide + 1) % side;
            m_Triangles[curSide * 3 + side * 6]     = side + (curSide + 1) % side;
            m_Triangles[curSide * 3 + side * 6 + 1] = side * 2 + curSide;
            m_Triangles[curSide * 3 + side * 6 + 2] = side * 2 + (curSide + 1) % side;
        }

        RenderMesh           = new Mesh();
        RenderMesh.vertices  = m_Vertices;
        RenderMesh.uv        = m_UV;
        RenderMesh.triangles = m_Triangles;
    }

    /// <summary>
    /// 绘制3到6条边的食物
    /// </summary>
    public void DrawFood(int side, int color)
    {
        float y          = color / (float)byte.MaxValue;
        side             = Mathf.Clamp(side, 3, 6);
        m_UV             = new Vector2[side + 1];
        m_UV[side]       = new Vector2(0.4f, y);
        m_Triangles      = new int[side * 3];
        m_Vertices       = new Vector3[side + 1];
        m_Vertices[side] = Vector3.zero;

        for (int curSide = 0; curSide < side; ++curSide)
        {
            m_Vertices[curSide]          = DynamicRenderData.GetRadianVector(curSide, side);
            m_UV[curSide]                = new Vector2(m_Vertices[curSide].y * 0.3333f + m_UV[side].x, y);
            m_Triangles[curSide * 3]     = curSide;
            m_Triangles[curSide * 3 + 1] = curSide + 1;
            m_Triangles[curSide * 3 + 2] = side;
        }
        m_Triangles[(side - 1) * 3 + 1] = 0;

        RenderMesh           = new Mesh();
        RenderMesh.vertices  = m_Vertices;
        RenderMesh.uv        = m_UV;
        RenderMesh.triangles = m_Triangles;
    }
}
