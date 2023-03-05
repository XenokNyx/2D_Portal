using Godot;
using System;

public partial class Player : CharacterBody2D {

    public static Player INSTANCE { get; private set; }

    [Export] float _moveSpeed = 100;
    [Export] PackedScene _portalPck;

    [Export] Color _portal1Color = new Color(0, 0, 1);
    [Export] Color _portal2Color = new Color(1, 0, 0);

    [Export(PropertyHint.Layers2DPhysics)] uint _portalWallCollisionLayer;
    [Export(PropertyHint.Layers2DPhysics)] uint _portalCollisionLayer;

    [Export] float _grabItemDistance = 64;
    [Export] float _kickForce = 10;

    [Export]
    public bool CanShootPortals {
        get => _canShootPortals;
        set {
            _canShootPortals = value;
            _portalGunSprite.Visible = _canShootPortals;
        }
    }
    bool _canShootPortals = true;

    [Export] Sprite2D _portalGunSprite;

    Vector2 _direction;
    Camera2D _camera;

    Portal[] _portals;

    PhysicsDirectSpaceState2D _spaceState;
    PhysicsRayQueryParameters2D _portalRay;
    PhysicsRayQueryParameters2D _actionRay;
    PhysicsRayQueryParameters2D _emptyRay;

    Item _grabedItem;

    Vector2 _lastValidPosition;
    bool _onGround = true;

    System.Action<Portal, Vector2> _onShootPortal;
    System.Action _onClearPortals;
    System.Action _onItemPickUp;

    public bool Active = false;

    public override void _Ready() {
        INSTANCE = this;

        _camera = GetViewport().GetCamera2D();
        CreatePortals();

        _spaceState = GetWorld2D().DirectSpaceState;

        _portalRay = new PhysicsRayQueryParameters2D();
        _portalRay.CollisionMask = _portalWallCollisionLayer;
        _portalRay.CollideWithAreas = true;

        _actionRay = new PhysicsRayQueryParameters2D();
        _actionRay.CollideWithAreas = true;
        _actionRay.CollisionMask = ~_portalRay.CollisionMask;

        _emptyRay = new PhysicsRayQueryParameters2D();
        _emptyRay.CollideWithAreas = true;
        _emptyRay.HitFromInside = true;
        _emptyRay.CollisionMask = _portalCollisionLayer;
    }

    private void CreatePortals() {
        _portals = new Portal[2];

        _portals[0] = _portalPck.Instantiate<Portal>();
        _portals[0].SetColor(_portal1Color);

        _portals[1] = _portalPck.Instantiate<Portal>();
        _portals[1].SetColor(_portal2Color);

        _portals[0].ConnectTo(_portals[1]);
    }

    public override void _Process(double delta) {

        if (!Active || !_onGround) return;

        LookAt(GetGlobalMousePosition());

        if (CanShootPortals) {
            if (Input.IsActionJustPressed("shoot_1")) {
                ShootPortal(0);
            } else if (Input.IsActionJustPressed("shoot_2")) {
                ShootPortal(1);
            }
        }

        if (Input.IsActionJustPressed("action")) {
            if (_grabedItem == null) {
                GrabItem();
            } else {
                ReleaseItem();
            }
        }

        if (Input.IsActionJustPressed("reset")) {
            ScnCtrl.ResetScene();
        }
    }

    private void ReleaseItem() {
        _grabedItem.Release();
        _grabedItem = null;
    }

    public override void _PhysicsProcess(double delta) {

        if (!Active || !_onGround) return;

        Move();
        _lastValidPosition = GlobalPosition;

        if (Input.IsActionJustPressed("kick") && _grabedItem != null) {
            _grabedItem.ApplyImpulse(GlobalPosition.DirectionTo(GetGlobalMousePosition()) * 1000);
            ReleaseItem();
        }

        MoveItem((float)delta);

    }

    private void MoveItem(float delta) {


        if (_grabedItem != null && !_grabedItem.CanBePickUp) {
            ReleaseItem();
        }

        if (_grabedItem != null) {

            Vector2 oriTarget = GlobalPosition + GlobalPosition.DirectionTo(GetGlobalMousePosition()) * _grabItemDistance;
            Portal closestPortal;
            if (GlobalPosition.DistanceTo(_portals[0].GlobalPosition) < GlobalPosition.DistanceTo(_portals[1].GlobalPosition)) {
                closestPortal = _portals[0];
            } else {
                closestPortal = _portals[1];
            }

            _emptyRay.From = GlobalPosition;
            _emptyRay.To = oriTarget;
            IntersectRay(_emptyRay, out Vector2 target);

            Vector2 endDirection = _emptyRay.From.DirectionTo(_emptyRay.To);

            Vector2 mirrorPos = closestPortal.GetMirrorPosition(target);
            if (_grabedItem.GlobalPosition.DistanceTo(oriTarget) < _grabedItem.GlobalPosition.DistanceTo(target)) {
                target = oriTarget;
                endDirection = GlobalTransform.BasisXform(Vector2.Right);
            }
            if (_grabedItem.GlobalPosition.DistanceTo(mirrorPos) < _grabedItem.GlobalPosition.DistanceTo(target)) {
                target = mirrorPos;
            }

            if (_grabedItem.GlobalPosition.DistanceTo(GlobalPosition) < _grabItemDistance) {
                Vector2 playerDir = GlobalPosition.DirectionTo(_grabedItem.GlobalPosition);
                float angle = playerDir.AngleTo(GlobalPosition.DirectionTo(target));
                playerDir = playerDir.Rotated(angle / 2);

                target = GlobalPosition + playerDir * _grabItemDistance;

            }

            Vector2 dir = target - _grabedItem.GlobalPosition;
            _grabedItem.LinearVelocity = dir * delta * 2000;

            _grabedItem.AngularVelocity = -endDirection.AngleTo(_grabedItem.GlobalTransform.BasisXform(Vector2.Right)) * delta * 1000f;

            if (_grabedItem.GlobalPosition.DistanceTo(target) > _grabItemDistance * 2.5f) {
                ReleaseItem();
            }
        }
    }

    Godot.Collections.Dictionary IntersectRay(PhysicsRayQueryParameters2D parameters, out Vector2 hitPoint) {
        return IntersectRay(parameters, new Rid(), out hitPoint);
    }
    Godot.Collections.Dictionary IntersectRay(PhysicsRayQueryParameters2D parameters, Rid connectedPortal, out Vector2 hitPoint) {

        if (connectedPortal.Id != 0) {
            var exclude = new Godot.Collections.Array<Rid>();
            exclude.Add(connectedPortal);
            parameters.Exclude = exclude;
        } else {
            parameters.Exclude = null;
        }

        var hit = _spaceState.IntersectRay(parameters);

        if (hit.Count > 0) {

            Node node = (Node)hit["collider"];

            hitPoint = (Vector2)hit["position"];

            if (node.IsInGroup("PortalArea")) {

                Portal portal = node as Portal;
                Vector2 direction = parameters.From.DirectionTo(parameters.To);

                if (Mathf.Abs(direction.AngleTo(-portal.GlobalTransform.Y)) > Mathf.Pi * .5f) {
                    float distance = parameters.From.DistanceTo(parameters.To);
                    distance -= parameters.From.DistanceTo(hitPoint);

                    if (distance > 0) {

                        parameters.From = portal.GetMirrorPosition(hitPoint);
                        parameters.To = parameters.From + portal.GetMirrorDirection(direction) * distance;

                        return IntersectRay(parameters, portal.ConnectedPortal.GetRid(), out hitPoint);
                    }
                } else {
                    return IntersectRay(parameters, portal.GetRid(), out hitPoint);
                }


            } else {
                return hit;
            }
        }

        hitPoint = parameters.To;
        return new Godot.Collections.Dictionary();
    }


    private void GrabItem() {
        _actionRay.From = GlobalPosition;
        _actionRay.To = GlobalPosition + GlobalPosition.DirectionTo(GetGlobalMousePosition()) * _grabItemDistance;

        var hit = IntersectRay(_actionRay, out Vector2 hitPoint);
        if (hit.Count > 0) {
            Node node = (Node)hit["collider"];
            if (node.IsInGroup("Item")) {
                _grabedItem = (Item)node;
                _grabedItem.OnPickUp();
                _onItemPickUp?.Invoke();
            }

        }
    }

    private void Move() {
        _direction.X = Input.GetAxis("move_left", "move_right");
        _direction.Y = Input.GetAxis("move_up", "move_down");

        _direction = _direction.Rotated(_camera.Rotation);

        this.Velocity = _direction * _moveSpeed;

        MoveAndSlide();
    }

    private void ShootPortal(int portalInd) {
        _portalRay.From = GlobalPosition;
        _portalRay.To = GlobalPosition + GlobalPosition.DirectionTo(GetGlobalMousePosition()) * _kickForce * 1000;

        var hit = _spaceState.IntersectRay(_portalRay);
        Vector2 position = _portalRay.To;

        if (hit.Count > 0) {

            Node collider = (Node)hit["collider"];
            position = (Vector2)hit["position"];
            Vector2 normal = (Vector2)hit["normal"];
            bool canPlacePortal = true;

            if (collider.IsInGroup("BlockPortal")) {
                canPlacePortal = false;
            } else if (collider is TileMap) {
                TileMap tileMap = collider as TileMap;

                canPlacePortal = !tileMap.GetCellTileData(1, tileMap.LocalToMap(position - normal)).GetCustomData("BlockPortal").AsBool();
            }

            if (canPlacePortal) {
                PlacePortal(portalInd, position, normal);
            }
        }

        _onShootPortal?.Invoke(_portals[portalInd], position);
    }

    public void PlacePortal(int portalInd, Vector2 position, Vector2 normal) {
        Portal portal = _portals[portalInd];

        if (!portal.IsInsideTree()) {
            GetTree().Root.AddChild(portal);
        }

        position /= 64;//Normalize position to the tile map
        position -= normal.Rotated(Mathf.Pi * .5f) * .5f;
        position = position.Round();
        position *= 64;

        portal.SetPortalPosition(position, normal);
    }


    public void ClearPortals(bool invoke = true) {
        foreach (var p in _portals) {
            if (p.IsInsideTree()) {
                p.Clear();
                p.GetParent().RemoveChild(p);
            }
        }

        if (invoke) {
            _onClearPortals?.Invoke();
        }
    }

    public void OnPassThroughPortal(float relativeRotation) {
        _camera.GlobalPosition = GlobalPosition;
        _camera.GlobalRotation += relativeRotation;
    }


    public async void Fall() {
        _onGround = false;

        float e = 0;
        while (e <= 1) {
            Scale = Vector2.One.Lerp(Vector2.Zero, e);

            e += TimeDelta.Delta;
            await this.ToNextFrame();
        }

        Scale = Vector2.One;
        _onGround = true;
        ScnCtrl.ResetScene();

    }

    public void AddOnShootPortal(System.Action<Portal, Vector2> action) {
        _onShootPortal += action;
    }
    public void AddOnClearPortals(System.Action action) {
        _onClearPortals += action;
    }
    public void AddOnItemPickUp(System.Action action) {
        _onItemPickUp += action;
    }

}
