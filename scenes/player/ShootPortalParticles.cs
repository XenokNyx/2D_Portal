using Godot;
using System;

public partial class ShootPortalParticles : GpuParticles2D {

    [Export] Player _player;
    [Export] Vector2 _startDes;

    ParticleProcessMaterial _material;
    int _amount;

    public override void _Ready() {

        _amount = Amount;
        _material = ProcessMaterial as ParticleProcessMaterial;
        _player.AddOnShootPortal(LaunchParticles);
    }

    void LaunchParticles(Portal portal, Vector2 position) {
        this.Restart();

        Vector2 globalDes = _player.GlobalPosition + GlobalTransform.BasisXform(_startDes);

        float distance = globalDes.DistanceTo(position) * .5f;
        Vector2 direction = globalDes.DirectionTo(position);

        _material.EmissionBoxExtents = new Vector3(distance, 2, 1);

        GlobalRotation = direction.Angle();
        GlobalPosition = globalDes + direction * distance;

        this.Modulate = portal.Color;
        this.Emitting = true;

    }
}
