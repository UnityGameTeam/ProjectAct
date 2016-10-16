using UnityEngine;
using System.Collections;

public interface IState
{
    void OnEnter();
    void OnLeave();
    void OnUpdate();
}
