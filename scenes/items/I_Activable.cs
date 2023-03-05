using Godot;
using System;

public interface I_Activable {

    bool IsActive { get; }

    void AddOnStateChange(System.Action action);
}
