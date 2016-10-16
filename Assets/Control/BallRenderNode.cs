using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BallRenderNode : MonoBehaviour
{
    public enum BallRenderType
    {
        Player = 1,
        Food = 2,
        Protect = 3,
        NormalMove = 4,
    }

    private static Material OldMaterial;                    //原始的Prefab上绑定的材质信息

    private MeshFilter     m_MeshFilter;
    private MeshRenderer   m_MeshRenderer;
    private Mesh           m_Mesh;
    private Transform      m_SelfTransform;
    private BallRenderType m_RenderType = BallRenderType.Food;

    public BallRenderType RenderType
    {
        get { return m_RenderType;  }
        set { m_RenderType = value; }
    }
    public float RenderRadius { get; protected set; } //圆的半径
    public float RenderColor  { get; protected set; } //渲染的颜色
    public float Score { get; set; }                  

    private void Awake()
    {
        m_MeshFilter    = GetComponent<MeshFilter>();
        m_MeshRenderer  = GetComponent<MeshRenderer>();
        m_Mesh          = null;
        m_SelfTransform = transform;

        if (OldMaterial != null)
            return;
        OldMaterial = m_MeshRenderer.sharedMaterial;
        
    }

    private void OnDisable()
    {
        RenderRadius = 0.0f;
        Score  = 0.0f;
        RenderColor = 0.0f;
        m_MeshRenderer.sharedMaterial = OldMaterial;
    }

    private void Start()
    {
        var pos = m_SelfTransform.position;
        pos.z   = -RenderRadius * 10;
        m_SelfTransform.position = pos;
    }

    private bool DrawFood(int side)
    {
        if (m_Mesh != null)
            return true;

        side   = Mathf.Clamp(side, 3, 6);
        m_Mesh = DynamicRenderData.GetColorCircleMesh(side, RenderColor);
        m_MeshFilter.mesh = m_Mesh;
        return true;
    }

    private bool DrawIcon(int side)
    {
        side   = DynamicRenderData.GetSide(side);
        m_Mesh = DynamicRenderData.GetIconCircleMesh(side);
        m_MeshFilter.mesh = m_Mesh;
        return true;
    }

    private bool DrawNoIcon(int side)
    {
        side   = DynamicRenderData.GetSide(side);
        m_Mesh = DynamicRenderData.GetColorCircleMesh(side, RenderColor);
        m_MeshFilter.mesh = m_Mesh;
        return true;
    }

    private bool DrawProtect()
    {
        if (m_Mesh != null)
            return true;

        m_Mesh = DynamicRenderData.SpikyBallMesh.RenderMesh;
        m_MeshFilter.mesh = m_Mesh;
        return true;
    }

    public void Set(float score, float color)
    {
        RenderColor = Mathf.Clamp01(color);
        switch (RenderType)
        {
            case BallRenderType.Food:
            case BallRenderType.Protect:
                SetScore(score, true);
                break;
            default:
                SetScore(score, false);
                break;
        }
    }

    //关于食物和刺球的动画先不处理
    public bool SetRadius(float radius, bool animation = false)
    {
        m_SelfTransform.localScale = radius * Vector3.one;
        RenderRadius = radius;

        var pos = m_SelfTransform.position;
        pos.z   = -radius * 10;
        m_SelfTransform.position = pos;

        switch (RenderType)
        {
            case BallRenderType.Player:
                return DrawNoIcon(GetDrawPlayerSide(radius));
            case BallRenderType.Food:
                //loom.fps这里使用的是实时fps
                //return DrawFood(Mathf.Min(30/ 10 + 2, 7 - (int)(CameraFollow.Size / 10.0)));
            case BallRenderType.Protect:
                return DrawProtect();
            case BallRenderType.NormalMove:
                return DrawNoIcon(20);
            default:
                return DrawFood(6);
        }
    }

    private int GetDrawPlayerSide(float radius)
    {
        var quality = 1;  //球的质量默认是1
        return (int)(16.0 + quality * 2.0 + radius * (quality * 0.5 + 3.0));
    }

    public bool SetScore(float score, bool animation = false)
    {
        if (Mathf.Approximately(score,Score))
            return false;
        Score = score;
        return SetRadius(Score2Radius(Score), animation);
    }

    public static float Score2Radius(float score)
    {
        return Mathf.Sqrt(score * 0.165f + 0.6f) * 0.5f;
    }
}
