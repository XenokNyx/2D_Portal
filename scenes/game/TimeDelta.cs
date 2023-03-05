using Godot;
using System;

public partial class TimeDelta : Node {

    public static float Delta { get; private set; }

    public override void _Process(double delta) {
        Delta = (float)delta;
    }
}
