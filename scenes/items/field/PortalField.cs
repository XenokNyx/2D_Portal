using Godot;
using System;

public partial class PortalField : Area2D {

    [Export] bool _defaultStateON = true;
    [Export] Godot.Collections.Array<NodePath> _listeningPaths;

    I_Activable[] _listening;

    [Export] GpuParticles2D _particles1;
    [Export] GpuParticles2D _particles2;

    uint _defaultCollisionLayer;

    bool _open = true;

    public override void _Ready() {
        this.Connect("body_entered", new Callable(this, "PlayerEnter"));

        _defaultCollisionLayer = CollisionLayer;

        if (_listeningPaths != null && _listeningPaths.Count > 0) {
            _listening = new I_Activable[_listeningPaths.Count];
            for (int i = 0; i < _listening.Length; i++) {
                _listening[i] = GetNode<I_Activable>(_listeningPaths[i]);
                _listening[i].AddOnStateChange(StateChange);
            }

            StateChange();
        }
    }

    void PlayerEnter(PhysicsBody2D body) {

        if (!_open) return;

        if (body is Player) {
            (body as Player).ClearPortals();
        } else {
            (body as Item).Destroy();
        }
    }


    void StateChange() {

        foreach (var a in _listening) {
            if (!a.IsActive) {
                _open = false;
                break;
            }
        }

        if (_defaultStateON) {
            _open = !_open;
        }

        _particles1.Emitting = _open;
        _particles2.Emitting = _open;

        if (!_open) {
            CollisionLayer = 0;
        } else {
            CollisionLayer = _defaultCollisionLayer;
        }

    }

}
