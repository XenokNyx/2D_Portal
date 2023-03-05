using Godot;
using System;

public partial class PortalParticles : GpuParticles2D {

    [Export] Portal _portal;

    public override void _Ready() {
        _portal.AddOnColorChange(SetColor);
        SetColor(_portal.Color);
    }

    void SetColor(Color color) {
        this.Modulate = color;
    }
}
