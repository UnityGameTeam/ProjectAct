using UnityEngine;

public class EntityNodeHandler
{
	public void Update ()
    {
	
	}

    public void AddFood(int count)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < count; ++i)
        {
            var foodEntity = new BallEntity();
            //GameServer.Instance.AddNode(foodEntity);
            //GameServer.Instance.NodesFood.Add();
        }
    }
}
