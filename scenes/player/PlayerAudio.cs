using Godot;
using System;

public partial class PlayerAudio : Node2D {

    [Export] Player _player;
    [Export] AudioStreamPlayer2D _shootAudio;
    [Export] AudioStreamPlayer2D _clearAudio;
    [Export] AudioStreamPlayer2D _pickUpAudio;

    RandomNumberGenerator _rng;

    public override void _Ready() {
        _rng = new RandomNumberGenerator();
        _player.AddOnShootPortal((Portal, Vector2) => PlayShootSound());
        _player.AddOnClearPortals(PlayClearSound);
        _player.AddOnItemPickUp(PlayItemPickUp);
    }

    void PlayShootSound() {
        _shootAudio.PitchScale = _rng.RandfRange(.9f, 1.1f);
        _shootAudio.Play();
    }

    void PlayClearSound() {
        _clearAudio.Play();
    }

    void PlayItemPickUp() {
        _pickUpAudio.Play();
    }
}
