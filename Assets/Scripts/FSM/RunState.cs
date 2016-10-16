using UnityEngine;
using System.Collections;

public class RunState : IState
{
    private Entity m_Entity;
    public RunState(Entity entity)
    {
        m_Entity = entity;
    }

    public void OnEnter()
    {
        m_Entity.Run();
    }

    public void OnLeave()
    {
 
    }

    public void OnUpdate()
    {
 
    }
}
