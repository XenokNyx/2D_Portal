using Godot;
using System.Collections.Generic;

public partial class Laser : Node2D {

    [Export] PackedScene _laserLinePck;
    [Export(PropertyHint.Layers2DPhysics)] uint _collisionMask;

    [Export] Node2D _hitParticles;


    List<Line2D> _activeLasers = new List<Line2D>(5);


    PhysicsDirectSpaceState2D _spaceState;
    PhysicsRayQueryParameters2D _rayData;

    Godot.Collections.Dictionary _hit;

    Node2D _hitBody;

    HashSet<Node2D> _hits = new HashSet<Node2D>();
    Line2D _laser;
    LaserReceiver _connectedReceiver;


    public override void _Ready() {
        _spaceState = GetWorld2D().DirectSpaceState;

        _rayData = new PhysicsRayQueryParameters2D();
        _rayData.CollisionMask = _collisionMask;
        _rayData.CollideWithAreas = true;
    }

    public override void _PhysicsProcess(double delta) {
        _hits.Clear();
        ClearLaser();

        CastLaser(GlobalPosition + GlobalTransform.BasisXform(Vector2.Down) * 32, GlobalTransform.BasisXform(Vector2.Right));
    }

    private void CastLaser(Vector2 from, Vector2 dir) {
        _rayData.From = from;
        _rayData.To = _rayData.From + dir * 10000;

        _hit = _spaceState.IntersectRay(_rayData);
        _rayData.Exclude = null;

        Vector2 hitPosition;

        if (_hit.Count > 0) {
            hitPosition = (Vector2)_hit["position"];


            _hitBody = (Node2D)_hit["collider"];

            if (_hitBody.IsInGroup("LaserBox") && !_hits.Contains(_hitBody)) {

                hitPosition += dir * 10;
                _hits.Add(_hitBody);
                CastLaser(_hitBody.GlobalPosition - _hitBody.GlobalTransform.BasisXform(Vector2.Right) * 10, _hitBody.GlobalTransform.BasisXform(Vector2.Right));
            } else if (_hitBody.IsInGroup("PortalArea") && (_hitBody as Portal).Active) {

                _rayData.Exclude = new Godot.Collections.Array<Rid>() { (_hitBody as Portal).ConnectedPortal.GetRid() };
                CastLaser((_hitBody as Portal).GetMirrorPosition(hitPosition - dir * 10), (_hitBody as Portal).GetMirrorDirection(dir));

                hitPosition += dir * 10;
            } else if (_hitBody.IsInGroup("LaserReceiver") && _connectedReceiver != _hitBody) {
                if (_connectedReceiver != null) {
                    DisconectReceiver();
                }
                _connectedReceiver = (_hitBody as LaserReceiver);
                _connectedReceiver.ConnectLaser(this);
            } else {

                _hitParticles.GlobalPosition = hitPosition;
                _hitParticles.Rotation = -((Vector2)_hit["normal"]).AngleTo(Vector2.Right) - GlobalRotation;
            }

            if (!_hitBody.IsInGroup("LaserReceiver") && _connectedReceiver != null) {
                DisconectReceiver();
            }

            _hitParticles.Visible = !_hitBody.IsInGroup("LaserReceiver");

        } else {
            hitPosition = _rayData.To;
        }

        ShowLaser(from, hitPosition);
    }

    private void DisconectReceiver() {
        _connectedReceiver.DisconnectLaser(this);
        _connectedReceiver = null;
    }

    void ShowLaser(Vector2 from, Vector2 to) {
        _laser = GetLaser();

        _laser.SetPointPosition(0, from);
        _laser.SetPointPosition(1, to);
    }

    Line2D GetLaser() {
        Line2D created = null;

        foreach (var l in _activeLasers) {
            if (!l.IsInsideTree()) {
                created = l;
                break;
            }
        }

        if (created == null) {
            created = _laserLinePck.Instantiate<Line2D>();
            _activeLasers.Add(created);
        }

        GetTree().Root.AddChild(created);
        created.GlobalPosition = Vector2.Zero;

        return created;
    }

    void ClearLaser() {
        foreach (var l in _activeLasers) {
            if (l.IsInsideTree()) {
                l.GetParent().RemoveChild(l);
            }
        }
    }

    public override void _ExitTree() {
        foreach (var l in _activeLasers) {
            l.QueueFree();
        }

        _activeLasers.Clear();
    }
}
