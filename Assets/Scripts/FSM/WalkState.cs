using UnityEngine;
using System.Collections;

public class WalkState : IState
{
    private Entity m_Entity;

    public WalkState(Entity entity)
    {
        m_Entity = entity;
    }

    public void OnEnter()
    {
        m_Entity.Idle();
    }

    public void OnLeave()
    {
        
    }

    public void OnUpdate()
    {
         
    }
}
