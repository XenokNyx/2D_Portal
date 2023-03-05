using Godot;
using System;

public partial class Elevator : Area2D {

    [Export] PackedScene nextMap;

    public override void _Ready() {

        if (nextMap == null) GD.PrintErr("No Next Map Defined");

        Connect("body_entered", new Callable(this, "_on_body_entered"));
    }

    async void _on_body_entered(Player player) {
        await this.ToTimeOut(.5f);
        ScnCtrl.ChangePlayScene(nextMap);
    }
}
