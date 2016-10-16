using UnityEngine;
using System.Collections;

public class AttackState : IState
{
    private Entity m_Entity;
    public AttackState(Entity entity)
    {
        m_Entity = entity;
    }

    public void OnEnter()
    {
        m_Entity.Attack();
        m_Entity.longWeaponTrail.Activate();
        m_Entity.shortWeaponTrail.Activate();
    }

    public void OnLeave()
    {
 
    }

    public void OnUpdate()
    {
        var stateInfo = m_Entity.EntityAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.normalizedTime >= 1 && stateInfo.IsName("attack"))
        {
            m_Entity.longWeaponTrail.Deactivate();
            m_Entity.shortWeaponTrail.Deactivate();
            m_Entity.EntityFsm.ChangeState(m_Entity.IdleState);
        }
    }
}
