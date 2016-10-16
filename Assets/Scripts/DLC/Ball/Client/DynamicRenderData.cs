using System.Collections.Generic;
using DLC.Ball.Config;
using UnityEngine;

public class DynamicRenderData
{
    private static DynamicRenderData _instance;
    public static DynamicRenderData Instance
    {
        get
        {
            if (_instance == null)
                _instance = new DynamicRenderData();
            return _instance;
        }
    }

    private static int    MinSide      = 20;               //动态生成的圆的最小边数
    private static int    MaxSide      = 100;              //动态生成的圆的最大边数
    private static int    SideOffset   = 4;                //边的偏移,用于在MinSide和MaxSide之间等间隔边数的Mesh
    private static int    ColorOffset  = 8;                //颜色边数间隔,用于在0-255之间等间隔生成Mesh
    private static int    MaxSideCount = 720;              //圆的最大边数,用于将圆分成720份大小的弧度,以及每份弧度在单位圆上的x,y值
    private static List<int> ColorList = new List<int>();  //颜色列表
    private static List<int> SideList  = new List<int>();  //边列表
    private static Dictionary<int, DynamicRenderInfo>             IconCircleMeshMap = new Dictionary<int, DynamicRenderInfo>();  //带有皮肤的圆的mesh缓存信息,key的圆的边数
    //纯色的Mesh缓存信息,key是Mesh的边数, value是Dictionary<int, DynamicRenderInfo>， value中的key是颜色列表中的值
    private static Dictionary<int, Dictionary<int, DynamicRenderInfo>> ColorMeshMap = new Dictionary<int, Dictionary<int, DynamicRenderInfo>>(); 

    public static DynamicRenderInfo SpikyBallMesh { get; protected set; }    //刺球的Mesh信息
     
    private Vector3[] RadianVectors;    //弧度在单位圆上的x,y值

    private DynamicRenderData()
    {
        InitRadianData();
        InitConfig();
    }

    private void InitRadianData()
    {
        RadianVectors    = new Vector3[MaxSideCount];
        float unitRadian = 2 * Mathf.PI / MaxSideCount;
        for (int side = 0; side < MaxSideCount; ++side)
        {
            float f = unitRadian * side;
            RadianVectors[side] = new Vector3(Mathf.Sin(f), Mathf.Cos(f), 0.0f);
        }
    }

    private void InitConfig()
    {
        int systemMemorySize = SystemInfo.systemMemorySize;
        if (systemMemorySize < 522)
        {
            MaxSide = 60;
            SideOffset = 8;
            ColorOffset = 16;
            BallConfig.BallQuality = 0;
        }
        else if (systemMemorySize < 1224)
        {
            MaxSide = 80;
            SideOffset = 8;
            ColorOffset = 16;
        }
        else if (systemMemorySize < 2248)
        {
            MaxSide = 90;
            SideOffset = 8;
            ColorOffset = 16;
        }
        else if (systemMemorySize < 3100)
        {
            MaxSide = 100;
            SideOffset = 6;
            ColorOffset = 12;
        }

        for (int i = 0; i < 256; i += ColorOffset)
        {
            ColorList.Add(i);
        }

        for (int i = MinSide; i <= MaxSide; i += SideOffset)
        {
            SideList.Add(i);
        }
    }

    /// <summary>
    /// 创建动态生成的Mesh的缓存信息
    /// </summary>
    public void CreateMeshCache()
    {
        //以下的后续步骤可以考虑异步执行，现在同步操作看性能也不是太差，先不管

        //生成刺球的Mesh
        SpikyBallMesh = new DynamicRenderInfo();
        SpikyBallMesh.DrawSpikyBall();

        //生成食物的Mesh，不同边数的不同颜色的Mesh
        for (int i = 3; i <= 6; ++i)
        {
            for (int j = 0; j < 256; j += ColorOffset)
            {
                DynamicRenderInfo meshInfo = new DynamicRenderInfo();
                meshInfo.DrawFood(i, j);

                if (!ColorMeshMap.ContainsKey(i))
                    ColorMeshMap.Add(i, new Dictionary<int, DynamicRenderInfo>());

                ColorMeshMap[i].Add(j, meshInfo);
            }
        }

        //生成纯色的圆的Mesh信息
        for (int i = MinSide; i <= MaxSide; i += SideOffset)
        {
            for (int j = 0; j < 256; j += ColorOffset)
            {
                DynamicRenderInfo meshInfo = new DynamicRenderInfo();
                meshInfo.DrawNoIconCircle(i, j);

                if (!ColorMeshMap.ContainsKey(i))
                    ColorMeshMap.Add(i, new Dictionary<int, DynamicRenderInfo>());

                ColorMeshMap[i].Add(j, meshInfo);
            }
        }

        //生成显示Icon的圆的Mesh信息
        for (int i = MinSide; i <= MaxSide; i += SideOffset)
        {
            DynamicRenderInfo meshInfo = new DynamicRenderInfo();
            meshInfo.DrawIconCircle(i);
            IconCircleMeshMap.Add(i, meshInfo);
        }
    }

    public static int GetSide(int side)
    {
        if ((side - MinSide)%SideOffset == 0)
        {
            if (side > MaxSide)
            {
                return SideList[SideList.Count - 1];
            }
            return SideList[(side - MinSide)/SideOffset];
        }
         
        if (side > MaxSide)
        {
            return SideList[SideList.Count - 1];
        }
        return SideList[(side - MinSide)/SideOffset + 1];
    }

    public static Mesh GetIconCircleMesh(int side)
    {
        return IconCircleMeshMap[side].RenderMesh;
    }

    /// <summary>
    /// color必须在0到1之间
    /// </summary>
    public static Mesh GetColorCircleMesh(int side,float color)
    {
        return ColorMeshMap[side][GetColor(color)].RenderMesh;
    }

    /// <summary>
    /// color必须在0到1之间
    /// </summary>
    public static int GetColor(float color)
    {
        return ColorList[(int)(color * (ColorList.Count - 1))];
    }

    /// <summary>
    /// 根据当前边数和最大边数得到弧度在单位圆上的向量值
    /// </summary>
    public static Vector3 GetRadianVector(int curSide, int maxSide)
    {
        return Instance.RadianVectors[(int)(curSide / (double)maxSide * MaxSideCount)];
    }
}
