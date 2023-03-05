using Godot;
using System;

public partial class SpikesArea : Area2D {
    public override void _Ready() {
        this.Connect("body_entered", new Callable(this, "PlayerEnter"));
    }

    void PlayerEnter(Player player) {
        player.Fall();
    }

}
