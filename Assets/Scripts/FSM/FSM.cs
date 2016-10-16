using UnityEngine;

public class FSM : MonoBehaviour
{
    public IState CurrentState { get; set; }

    public void ChangeState(IState newState)
    {
        if(CurrentState != null)
            CurrentState.OnLeave();

        CurrentState = newState;

        if (CurrentState != null)
            CurrentState.OnEnter();
    }

    void Update ()
    {
        if (CurrentState != null)
            CurrentState.OnUpdate();
    }
}
