using Godot;
using System;

public partial class AutoPortal : Node {
    [Export] int _portalInd;

    [Export] Godot.Collections.Array<NodePath> _targetsPaths;
    Node2D[] _targets;

    [Export] float[] _seconds;
    [Export] bool _loop;

    [Export] public AudioStreamPlayer2D _portalSound;

    bool _active = true;

    public override void _Ready() {
        _targets = new Node2D[_targetsPaths.Count];
        for (int i = 0; i < _targets.Length; i++) {
            _targets[i] = GetNode<Node2D>(_targetsPaths[i]);
        }

        PlacePortals();

    }

    async void PlacePortals() {
        int i = 0;

        while (i < _targets.Length && _active) {

            await this.ToTimeOut(_seconds[i]);

            if (!_active) break;

            ScnCtrl.INSTANCE.Player.PlacePortal(_portalInd, _targets[i].GlobalPosition, _targets[i].GlobalTransform.Y);
            _portalSound?.Play();

            i++;
            if (_loop && i >= _targets.Length) {
                i = 0;
            }
        }
    }

    void _disable(PhysicsBody2D body) {
        _active = false;
    }
}
