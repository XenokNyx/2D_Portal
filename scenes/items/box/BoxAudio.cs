using Godot;
using System;

public partial class BoxAudio : AudioStreamPlayer2D {

    [Export] Item _box;

    public override void _Ready() {
        _box.Connect("body_entered", new Callable(this, "_on_box_body_entered"));
    }

    void _on_box_body_entered(Node body) {
        VolumeDb = Mathf.LinearToDb(Mathf.Clamp(_box.LinearVelocity.Length() * .016f, 0.4f, 2));
        Play();
    }

}
