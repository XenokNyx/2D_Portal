using Godot;
using System;

public partial class Cable : Line2D {

    [Export] Godot.Collections.Array<NodePath> _listeningPaths;
    I_Activable[] _listening;

    [Export] Color _dissabledColor = new Color(0, 0, 0);
    [Export] Color _enabledColor = new Color(1, 1, 1);


    Sprite2D _sprite;

    public override void _Ready() {

        _sprite = GetChild<Sprite2D>(0);

        if (_listeningPaths != null && _listeningPaths.Count > 0) {
            _listening = new I_Activable[_listeningPaths.Count];
            for (int i = 0; i < _listening.Length; i++) {
                _listening[i] = GetNode<I_Activable>(_listeningPaths[i]);
                _listening[i].AddOnStateChange(StateChange);
            }
        } else {
            GD.PrintErr($"No listeningPaths deffined for Cable: {this.Name}");
        }

        StateChange();

    }

    void StateChange() {
        bool open = true;

        foreach (var a in _listening) {
            if (!a.IsActive) {
                open = false;
                break;
            }
        }

        if (open) {
            DefaultColor = _enabledColor;
        } else {
            DefaultColor = _dissabledColor;
        }

        _sprite.SelfModulate = DefaultColor;

    }

}
