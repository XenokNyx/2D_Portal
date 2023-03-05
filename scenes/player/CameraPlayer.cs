using Godot;
using System;

public partial class CameraPlayer : Camera2D {

    [Export] Node2D _player;

    public override void _Process(double delta) {

        GlobalPosition = _player.GlobalPosition;
    }


}
