using Godot;
using System.Collections.Generic;

public partial class Portal : Area2D {

    static Color CLEAR_COLOR = new Color(1, 1, 1, 0);
    static Color MASK_COLOR = new Color(1, 1, 1);


    [Export] float _width = 64;
    [Export] float _vDes = 0;

    [Export] public Portal ConnectedPortal { get; private set; }
    [Export] Material _visibleMaskMaterial;

    [Export(PropertyHint.Layers2DPhysics)] uint _wallCollisionLayer;

    [Export(PropertyHint.Range, "0, 100")]
    public int MaxRecursivePortals {
        get => _maxRecursivePortals;
        set {
            _maxRecursivePortals = value;
            CreateMasks();
        }
    }

    [Export]
    public Color Color {
        get => _color;
        private set {
            _color = value;
            _onColorChange?.Invoke(_color);
        }
    }
    Color _color = new Color(1, 1, 1);
    int _maxRecursivePortals = 5;

    public bool Active { get => ConnectedPortal != null && this.IsInsideTree() && ConnectedPortal.IsInsideTree(); }

    Polygon2D[] _mask;
    Camera2D _camera;

    System.Action<int, Polygon2D> _onMaskPolygonChange;
    System.Action<int> _onMaskInvisible;
    System.Action<PhysicsBody2D, Vector2, float> _onEntityEnter;
    System.Action<PhysicsBody2D> _onEntityExit;
    System.Action<PhysicsBody2D, Vector2, float> _onEntityMove;
    System.Action<Polygon2D[]> _onMaskCreated;
    System.Action _onPositionChange;
    System.Action<Color> _onColorChange;

    Vector2[][] _polygon;
    Vector2[] _uv;
    Vector2 _dirL, _dirR;
    Vector2 _screenSize;

    HashSet<PhysicsBody2D> _bodysEnterPoint = new HashSet<PhysicsBody2D>();
    HashSet<PhysicsBody2D> _bodysEnterPointRemove = new HashSet<PhysicsBody2D>();

    uint _maskLayer = 1 << 1;

    float _targetRelativeRotation { get => (ConnectedPortal.GlobalRotation - Mathf.Pi) - GlobalRotation; }

    bool _skipUpdate = false;

    SubViewport _maskViewport;

    public override void _Ready() {
        CreateMaskViewport();
        CreateMasks();

        GetViewport().SizeChanged += UpdateViewport;
        UpdateViewport();

        if (ConnectedPortal != null) {
            ConnectTo(ConnectedPortal);
        }

        Connect("body_entered", new Callable(this, "_on_area_2d_body_entered"));
        Connect("body_exited", new Callable(this, "_on_area_2d_body_exited"));
    }

    private void CreateMaskViewport() {
        _maskViewport = new SubViewport();
        AddChild(_maskViewport);
        _maskViewport.World2D = GetViewport().World2D;
        _maskViewport.CanvasCullMask -= _maskLayer;
        _maskViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;

        _camera = new Camera2D();
        _camera.IgnoreRotation = false;
        _maskViewport.AddChild(_camera);
    }

    private void CreateMasks() {
        if (_maskViewport == null) return;
        if (_mask != null && MaxRecursivePortals == _mask.Length) return;

        _mask = new Polygon2D[MaxRecursivePortals + 1];
        for (int i = 0; i < _mask.Length; i++) {
            _mask[i] = new Polygon2D();
            _mask[i].Material = _visibleMaskMaterial;

            _mask[i].VisibilityLayer = _maskLayer;
            _mask[i].Texture = _maskViewport.GetTexture();
            _mask[i].Visible = true;

            if (i == 0) {
                AddChild(_mask[i]);
            } else {
                _mask[i - 1].AddChild(_mask[i]);
            }
        }

        _mask[0].ZAsRelative = false;

        int numPolygons = 5;
        _uv = new Vector2[numPolygons];
        _polygon = new Vector2[_mask.Length][];
        for (int i = 0; i < _polygon.Length; i++) {
            _polygon[i] = new Vector2[numPolygons];
        }

        _polygon[0][0] = Vector2.Down * _vDes;
        _polygon[0][1] = Vector2.Down * _vDes + Vector2.Right * _width;

        _onMaskCreated?.Invoke(_mask);
    }

    public override void _Process(double delta) {

        if (!Active) return;


        CheckPassThroughPortal();

        if (_skipUpdate) {
            _skipUpdate = false;
            return;
        }


        for (int i = 0; i < _mask.Length; i++) {

            if ((i > 0 && (!_mask[i - 1].Visible || _mask[i - 1].ZIndex < 0)) || !IsBodyInFront(i, Player.INSTANCE)) {
                HideMask(i);

            } else {
                if (i == 0) {
                    _mask[0].SelfModulate = MASK_COLOR;
                }

                _mask[i].Visible = true;
                CalculateMask(i);
            }
        }
    }

    public void Clear() {
        _mask[0].Visible = false;
        for (int i = 0; i < _mask.Length; i++) {
            HideMask(i);
        }
    }

    private void CalculateMask(int ind) {
        Vector2 startPolygon = CalculateMaskStartPolygons(ind);
        if (_mask[ind].Visible) {
            CalculateVisibleMaskPolygons(ind, startPolygon);
        }
    }

    private void HideMask(int i) {
        if (i == 0) {
            _mask[0].ZIndex = -4000;//Don't set mask 0 to !visible so the UV of the mask are updated to prevent artifacts when is not visible
            _mask[0].SelfModulate = CLEAR_COLOR;
        } else {
            _mask[i].Visible = false;
        }
        _onMaskInvisible?.Invoke(i);
    }

    bool IsPointInsideMask(Vector2 point, Vector2[] polygon) {
        float totalAngle = 0;
        float angle;
        Vector2 preDir = point.DirectionTo(polygon[0]);
        Vector2 dir;

        for (int i = 1; i < polygon.Length; i++) {
            dir = point.DirectionTo(polygon[i]);
            angle = preDir.AngleTo(dir);
            totalAngle += angle;

            preDir = dir;
        }

        dir = point.DirectionTo(polygon[0]);
        angle = preDir.AngleTo(dir);
        totalAngle += angle;

        return totalAngle + .001f >= Mathf.Tau;
    }

    bool IsPointInViewPort(Vector2 point) {
        return GetViewport().GetCamera2D().GlobalPosition.DistanceTo(point) < GetViewportRect().Size.X;
    }

    public static Vector2 Intersection(Vector2 p1_1, Vector2 p1_2, Vector2 p2_1, Vector2 p2_2) {
        float dx12 = p1_2.X - p1_1.X;
        float dy12 = p1_2.Y - p1_1.Y;
        float dx34 = p2_2.X - p2_1.X;
        float dy34 = p2_2.Y - p2_1.Y;

        float denominator = (dy12 * dx34 - dx12 * dy34);

        if (denominator == 0) {// The lines are parallel.
            return new Vector2(float.NaN, float.NaN);
        }

        float t1 = ((p1_1.X - p2_1.X) * dy34 + (p2_1.Y - p1_1.Y) * dx34) / denominator;
        float t2 = ((p2_1.X - p1_1.X) * dy12 + (p1_1.Y - p2_1.Y) * dx12) / -denominator;

        if (((t1 >= 0) && (t1 <= 1) && (t2 >= 0) && (t2 <= 1))) {
            return new Vector2(p1_1.X + dx12 * t1, p1_1.Y + dy12 * t1);
        }

        return new Vector2(float.NaN, float.NaN);
    }

    Vector2 GetReflectedPosition(int ind) {
        if (ind == 0) return GlobalPosition;
        if (ind == -1) return ConnectedPortal.GlobalPosition + ConnectedPortal.GlobalTransform.X * _width;

        Vector2 preGlobalPos = GetReflectedPosition(ind - 1);

        Vector2 nextPost = GetReflectedPosition(ind - 2) - preGlobalPos;
        nextPost = preGlobalPos + nextPost.Rotated(GlobalRotation - ConnectedPortal.GlobalRotation);

        return nextPost;
    }

    float GetReflectedRotation(int ind) {
        if (ind <= 0) return GlobalRotation;
        return GlobalRotation - ((ConnectedPortal.GlobalRotation - GlobalRotation) + Mathf.Pi) * ind;
    }

    private Vector2 CalculateMaskStartPolygons(int ind) {
        Vector2 startPolygon = (Vector2.Down * _vDes + GetReflectedPosition(ind) - GlobalPosition).Rotated(-GlobalRotation);

        _polygon[ind][0] = startPolygon;
        _polygon[ind][1] = startPolygon + Vector2.Right.Rotated(GetReflectedRotation(ind) - GlobalRotation) * _width;

        if (ind > 0) {

            bool p1 = IsPointInsideMask(_polygon[ind][0], _polygon[ind - 1]);
            bool p2 = IsPointInsideMask(_polygon[ind][1], _polygon[ind - 1]);

            if (!p1 && !p2) {

                Vector2 intersection = Intersection(_polygon[ind][0], _polygon[ind][1], _polygon[ind - 1][0], _polygon[ind - 1][4]);

                if (float.IsNaN(intersection.X)) {
                    HideMask(ind);
                    return Vector2.Inf;
                } else {
                    _polygon[ind][0] = intersection;

                    intersection = Intersection(_polygon[ind][0], _polygon[ind][1], _polygon[ind - 1][1], _polygon[ind - 1][2]);
                    if (!float.IsNaN(intersection.X)) {
                        _polygon[ind][1] = intersection;
                    } else if (_polygon[ind][1].DistanceTo(_polygon[ind - 1][1]) > .001f) {
                        HideMask(ind);
                        return Vector2.Inf;
                    }
                }
            }

            if (p1 != p2) {
                Vector2 intersection = Intersection(_polygon[ind][0], _polygon[ind][1], _polygon[ind - 1][0], _polygon[ind - 1][4]);
                if (float.IsNaN(intersection.X)) {
                    intersection = Intersection(_polygon[ind][0], _polygon[ind][1], _polygon[ind - 1][1], _polygon[ind - 1][2]);
                }

                if (!float.IsNaN(intersection.X)) {
                    if (!p1) _polygon[ind][0] = intersection;
                    else _polygon[ind][1] = intersection;
                }

            }

        }

        return startPolygon;
    }
    private void CalculateVisibleMaskPolygons(int ind, Vector2 startPolygon) {
        float step = 1 - (GetRelativePosition(ind, Player.INSTANCE).X / _width);
        if (step > 1 || step < 0) {
            step = .5f;
        }

        AddMaskPolygon(ind, 0, 2);
        AddMaskPolygon(ind, step, 3);
        AddMaskPolygon(ind, 1, 4);

        Vector2 dir = startPolygon - _polygon[0][0];

        for (int i = 0; i < _polygon[ind].Length; i++) {
            _uv[i] = (_polygon[ind][i] - dir).Rotated(-(GetReflectedRotation(ind) - GlobalRotation)) + _screenSize * .5f + Vector2.Up * _screenSize.Y * .5f;
        }

        _mask[ind].Polygon = _polygon[ind];
        _mask[ind].UV = _uv;
        _mask[ind].ZIndex = (int)RenderingServer.CanvasItemZMax - Mathf.RoundToInt(Player.INSTANCE.GlobalPosition.DistanceTo(GlobalPosition + _polygon[ind][0]) / 10);

        _onMaskPolygonChange?.Invoke(ind, _mask[ind]);

    }

    private void AddMaskPolygon(int ind, float step, int i) {
        Vector2 lerpPos = _polygon[ind][1].Lerp(_polygon[ind][0], step);
        _dirR = Player.INSTANCE.GlobalPosition.DirectionTo(GlobalPosition + lerpPos.Rotated(GlobalRotation));
        _polygon[ind][i] = lerpPos + _dirR.Rotated(-GlobalRotation) * 10000;
    }


    private void CheckPassThroughPortal() {

        foreach (var body in _bodysEnterPoint) {

            Vector2 relativePos = (body.GlobalPosition - GlobalPosition).Rotated(-GlobalRotation);
            relativePos = CalculateTargetRelativePosition(relativePos);
            ConnectedPortal._onEntityMove?.Invoke(body, relativePos, _targetRelativeRotation);

            relativePos = relativePos.Rotated(ConnectedPortal.Rotation);

            if (!IsBodyInFront(0, body)) {

                body.GlobalPosition = ConnectedPortal.GlobalPosition + relativePos;
                body.GlobalRotation += _targetRelativeRotation;

                if (body == Player.INSTANCE) {
                    _skipUpdate = true;

                    Player.INSTANCE.OnPassThroughPortal(_targetRelativeRotation);

                    if (GetIndex() < ConnectedPortal.GetIndex()) {//If the target portal is executed after this one, skip the next frame 
                        ConnectedPortal._skipUpdate = true;
                    }

                    _mask[0].Visible = true;
                } else if (body is RigidBody2D) {
                    (body as RigidBody2D).LinearVelocity = (body as RigidBody2D).LinearVelocity.Rotated(_targetRelativeRotation);
                }

                _bodysEnterPointRemove.Add(body);
            }
        }

        foreach (var body in _bodysEnterPointRemove) {

            RemoveCollisionBody(body);
            ConnectedPortal.AddCollisionBody(body);
        }
        _bodysEnterPointRemove.Clear();
    }


    private Vector2 CalculateTargetRelativePosition(Vector2 relativePos) {
        relativePos.X = _width - relativePos.X;
        relativePos.Y *= -1;
        return relativePos;
    }

    public Vector2 GetMirrorPosition(Vector2 position) {
        Vector2 relativePos = (position - GlobalPosition).Rotated(-GlobalRotation);
        relativePos = CalculateTargetRelativePosition(relativePos);
        relativePos = relativePos.Rotated(ConnectedPortal.Rotation);
        return ConnectedPortal.GlobalPosition + relativePos;
    }

    public Vector2 GetMirrorDirection(Vector2 direction) {
        return direction.Rotated(_targetRelativeRotation);
    }

    private bool IsBodyInFront(int ind, Node2D body) {
        return IsPointInFront(ind, body.GlobalPosition);
    }
    private bool IsPointInFront(int ind, Vector2 point) {
        return GetRelativePosition(ind, point).Y < 0;
    }

    public bool IsPointInFront(Vector2 point) {
        return IsPointInFront(0, point);
    }

    private Vector2 GetRelativePosition(int ind, Node2D body) {
        return GetRelativePosition(ind, body.GlobalPosition);
    }
    private Vector2 GetRelativePosition(int ind, Vector2 position) {
        return (position - GetReflectedPosition(ind)).Rotated(-GetReflectedRotation(ind));
    }

    public void ConnectTo(Portal connection) {
        ConnectedPortal = connection;
        ConnectedPortal.ConnectedPortal = this;

        UpdateCamera();
        ConnectedPortal.UpdateCamera();
    }

    public void SetPortalPosition(Vector2 position, Vector2 direction) {
        GlobalPosition = position;

        GlobalRotation = Vector2.Up.AngleTo(direction);

        UpdateCamera();
        ConnectedPortal.UpdateCamera();

        if (ConnectedPortal.Active) {
            ConnectedPortal.CalculateMask(0);//Force update in case connected portal is not visible
        }

        _onPositionChange?.Invoke();
    }

    private void UpdateCamera() {
        if (!Active) return;
        if (_camera == null) return;

        _camera.GlobalRotation = ConnectedPortal.GlobalRotation + Mathf.Pi;
        _camera.GlobalPosition = ConnectedPortal.GlobalPosition + _camera.GlobalTransform.Y * _screenSize.Y * .5f;


    }

    private void UpdateViewport() {

        (_camera.GetViewport() as SubViewport).Size = (Vector2I)GetViewport().GetVisibleRect().Size + Vector2I.Right * (int)_width * 2;
        _screenSize = (Vector2I)GetViewport().GetVisibleRect().Size;

        UpdateCamera();
    }

    void _on_area_2d_body_entered(PhysicsBody2D body) {
        if (!Active) return;

        body.CollisionMask &= ~_wallCollisionLayer;
        AddCollisionBody(body);
    }


    public void SetColor(Color color) {
        Color = color;
    }

    void _on_area_2d_body_exited(PhysicsBody2D body) {

        if (_bodysEnterPoint.Contains(body)) {
            body.CollisionMask |= _wallCollisionLayer;

            RemoveCollisionBody(body);
        }
    }


    private void AddCollisionBody(PhysicsBody2D body) {
        if (!_bodysEnterPoint.Contains(body)) {
            _bodysEnterPoint.Add(body);

            Vector2 relativePos = (body.GlobalPosition - GlobalPosition).Rotated(-Rotation);
            relativePos = CalculateTargetRelativePosition(relativePos);

            ConnectedPortal._onEntityEnter?.Invoke(body, relativePos, _targetRelativeRotation);
        }
    }

    void RemoveCollisionBody(PhysicsBody2D body) {
        _bodysEnterPoint.Remove(body);
        ConnectedPortal._onEntityExit?.Invoke(body);
    }

    public void AddOnMaskPolygonChange(System.Action<int, Polygon2D> actions) {
        _onMaskPolygonChange += actions;
    }
    public void AddOnMaskInvisible(System.Action<int> actions) {
        _onMaskInvisible += actions;
    }
    public void AddOnEntityEnter(System.Action<PhysicsBody2D, Vector2, float> action) {
        _onEntityEnter += action;
    }
    public void AddOnEntityExit(System.Action<PhysicsBody2D> action) {
        _onEntityExit += action;
    }
    public void AddOnEntityMove(System.Action<PhysicsBody2D, Vector2, float> action) {
        _onEntityMove += action;
    }
    public void AddOnMaskCreated(System.Action<Polygon2D[]> action) {
        _onMaskCreated += action;
    }
    public void AddOnPositionChange(System.Action action) {
        _onPositionChange += action;
    }
    public void AddOnColorChange(System.Action<Color> action) {
        _onColorChange += action;
    }
}

