using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Button : Area2D, I_Activable {

    Sprite2D _sprite;
    [Export] Vector2 _unPressed;
    [Export] Vector2 _pressed;

    HashSet<PhysicsBody2D> _bodies = new HashSet<PhysicsBody2D>();

    public bool IsActive { get; private set; }
    System.Action _onStateChange;

    public override void _Ready() {

        _sprite = GetChildren().OfType<Sprite2D>().ElementAt(0);
        if (_sprite == null) {
            GD.PrintErr("Button Needs Sprite2D");
        }

        Connect("body_entered", new Callable(this, "_on_body_entered"));
        Connect("body_exited", new Callable(this, "_on_body_exited"));
    }

    public override void _Process(double delta) {
        SetPress(CheckPress());
    }

    private void SetPress(bool press) {

        if (IsActive == press) return;

        Rect2 rect = _sprite.RegionRect;

        if (press) {
            rect.Position = _pressed;
        } else {
            rect.Position = _unPressed;
        }

        _sprite.RegionRect = rect;

        IsActive = press;

        _onStateChange?.Invoke();
    }

    private bool CheckPress() {
        foreach (var b in _bodies) {
            if (b.IsInGroup("Item")) {
                if ((b as Item).IsOnFloor) {
                    return true;
                }
            } else {
                return true;
            }
        }

        return false;
    }

    void _on_body_entered(PhysicsBody2D body) {
        _bodies.Add(body);
    }

    void _on_body_exited(PhysicsBody2D body) {
        _bodies.Remove(body);
    }

    public void AddOnStateChange(Action action) {
        _onStateChange += action;
    }
}
