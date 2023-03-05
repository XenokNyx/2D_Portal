using Godot;
using System;

public partial class PortalGunPickUp : Area2D {

    public override void _Ready() {
        this.Connect("body_entered", new Callable(this, "ActivatePortals"));
    }

    void ActivatePortals(Player player) {
        player.CanShootPortals = true;

        this.QueueFree();
    }
}
