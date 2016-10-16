using UnityEngine;
using System.Collections;

namespace Pattern.Node
{
    public interface ISwitchable
    {
        bool IsOpen { get; }
        bool SwitchOn();
        bool SwitchOff();
        bool Toggle(bool turnOn);
    }
}
