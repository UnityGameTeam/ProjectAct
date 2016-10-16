using System.Collections.Generic;
using UnityEngine;


public class MapConfig
{
    public static float cx = 0;
    public static float cy = 0;
    public static float halfWidth = 300;
    public static float halfHeight = 300;
}

public class FoodConfig
{
    public static int foodSpawnAmount = 10;
    public static int foodStartAmount = 500;
    public static int foodMaxAmount = 4500;
    public static int foodScore = 10;
}

public class GameServer : MonoBehaviour
{
    public QuadTree<BallEntity> MapQuadTree { get; set; }
    public Dictionary<uint, BallEntity> NodesFood { get; set; }

    public static GameServer Instance;

    private EntityNodeHandler m_EntityNodeHandler;
    private uint m_LastNodeId = 0;

    void Start ()
	{
	    Instance = this;

        MapQuadTree = new QuadTree<BallEntity>(null, new Rectangle(MapConfig.cx, MapConfig.cy, MapConfig.halfWidth, MapConfig.halfHeight), new QuadTreeSetting() {MaxLevel = 10,MaxObjectNum = 128});
        NodesFood = new Dictionary<uint, BallEntity>();

        m_EntityNodeHandler = new EntityNodeHandler();
    }

	void Update ()
    {
        m_EntityNodeHandler.Update();
    }

    public void AddNode(BallEntity ballEntity)
    {
        MapQuadTree.Add(ballEntity);
    }

    public void RemoveNode(BallEntity ballEntity)
    {
        MapQuadTree.Remove(ballEntity);
    }

    public uint GetNextNodeId()
    {
        ++m_LastNodeId;
        return m_LastNodeId;
    }
}
