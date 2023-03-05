using Godot;
using System.Linq;


public partial class Door : StaticBody2D {

    [Export] float _openSpeed = 1;
    [Export] float _openWidth = 64;

    [Export] Node2D _doorLeft;
    [Export] Node2D _doorRigth;

    [Export] Godot.Collections.Array<NodePath> _listeningPaths;

    [Export] AudioStreamPlayer2D _audio;

    I_Activable[] _listening;

    CollisionShape2D[] _collisionShapes;

    bool _open;

    Vector2 _leftClosePos;
    Vector2 _rightClosePos;

    public override void _Ready() {

        _collisionShapes = GetChildren().OfType<CollisionShape2D>().ToArray();

        _leftClosePos = _doorLeft.Position;
        _rightClosePos = _doorRigth.Position;

        if (_listeningPaths != null && _listeningPaths.Count > 0) {
            _listening = new I_Activable[_listeningPaths.Count];
            for (int i = 0; i < _listening.Length; i++) {
                _listening[i] = GetNode<I_Activable>(_listeningPaths[i]);
                _listening[i].AddOnStateChange(StateChange);
            }
        } else {
            GD.PrintErr($"No listeningPaths deffined for Door: {this.Name}");
        }

    }


    async void OpenDoor(bool open) {

        if (_open == open) return;

        _open = open;

        float e = 0;
        float weight;

        _audio.Play();

        while (e <= 1) {

            if (open) weight = e;
            else weight = 1 - e;

            _doorLeft.Position = _leftClosePos.Lerp(_leftClosePos + Vector2.Left * _openWidth * .5f, weight);
            _doorRigth.Position = _rightClosePos.Lerp(_rightClosePos + Vector2.Right * _openWidth * .5f, weight);

            e += TimeDelta.Delta * _openSpeed;
            await this.ToNextFrame();
        }


    }

    void StateChange() {
        bool open = true;

        foreach (var a in _listening) {
            if (!a.IsActive) {
                open = false;
                break;
            }
        }

        OpenDoor(open);

    }

}
