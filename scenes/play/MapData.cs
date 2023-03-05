using Godot;
using System;

public partial class MapData : Node {
    [Export]
    public bool CanShootPortals { get; private set; } = true;
}
