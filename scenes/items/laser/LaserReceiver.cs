using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LaserReceiver : Area2D, I_Activable {

    Sprite2D[] _sprites;
    [Export] Vector2[] _unPressed;
    [Export] Vector2[] _pressed;
    HashSet<Laser> _connectedLasers = new HashSet<Laser>();

    public bool IsActive { get; private set; }
    System.Action _onStateChange;

    public override void _Ready() {
        _sprites = FindChildren("*", "Sprite2D").OfType<Sprite2D>().ToArray();
    }

    public void ConnectLaser(Laser laser) {
        _connectedLasers.Add(laser);
        CheckActive();
    }

    public void DisconnectLaser(Laser laser) {
        _connectedLasers.Remove(laser);
        CheckActive();
    }

    void CheckActive() {
        bool countA = _connectedLasers.Count > 0;

        if (IsActive == countA) return;

        IsActive = countA;

        Rect2 rect;

        for (int i = 0; i < _sprites.Length; i++) {
            rect = _sprites[i].RegionRect;

            if (IsActive) {
                rect.Position = _pressed[i];
            } else {
                rect.Position = _unPressed[i];
            }

            _sprites[i].RegionRect = rect;

        }

        _onStateChange?.Invoke();
    }

    public void AddOnStateChange(Action action) {
        _onStateChange += action;
    }

}
