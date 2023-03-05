using Godot;
using System.Linq;
using System.Threading.Tasks;

public partial class Item : RigidBody2D {

    [Export] Node2D _spriteBase;
    [Export] float _pickUpScale = 1.2f;
    [Export] Sprite2D _shadow;
    [Export] float _pickUpSpeed = 10;
    [Export] float _maxShadowSize = 1.2f;

    uint _startLayer;

    public bool CanBePickUp { get; private set; } = true;

    float _delta;

    public bool IsOnFloor { get; private set; } = true;

    Vector2 _startPosition;
    bool _reset = false;

    public override void _Ready() {
        _startLayer = this.CollisionLayer;

        _shadow.Visible = false;

        _startPosition = GlobalPosition;
    }

    public override void _Process(double delta) {
        _delta = (float)delta;

        if (IsOnFloor) {
            _shadow.Scale = Vector2.One * Mathf.Lerp(1, _maxShadowSize, Mathf.Clamp(LinearVelocity.Length() / 500, 0, 1));
            _spriteBase.Scale = Vector2.One;
        }

    }

    public void OnPickUp() {
        _shadow.Visible = true;
        ShowShadow();
        IsOnFloor = false;
    }

    public void Release() {
        IsOnFloor = true;
    }

    async void ShowShadow() {
        float e = 0;
        while (e <= 1) {

            _shadow.Scale = Vector2.One * Mathf.Lerp(1, _maxShadowSize, e);
            _spriteBase.Scale = Vector2.One * Mathf.Lerp(1, _pickUpScale, e);

            await this.ToNextFrame();
            e += _delta * _pickUpSpeed;
        }

        _shadow.Scale = Vector2.One * _maxShadowSize;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state) {
        if (_reset) {

            Transform2D t = new Transform2D();
            t.Origin = _startPosition;

            state.Transform = t;


            _reset = false;
        }
    }

    void EnableCollisions(bool enabled) {
        if (!enabled) this.CollisionLayer = 0;
        else this.CollisionLayer = _startLayer;
    }

    public async void Destroy() {
        CanBePickUp = false;

        LinearVelocity = Vector2.Zero;
        AngularVelocity = 0;

        EnableCollisions(false);

        ShaderMaterial material = _spriteBase.Material as ShaderMaterial;

        _shadow.Visible = false;
        await Dissolve(material);

        _reset = true;

        while (_reset == true) {
            await this.ToNextFrame();
        }
        await this.ToNextFrame();

        _shadow.Visible = true;
        material.SetShaderParameter("dissolveVal", 0);
        EnableCollisions(true);


        CanBePickUp = true;

    }

    async Task Dissolve(ShaderMaterial material) {

        float e = 0;
        while (e <= 1) {

            material.SetShaderParameter("dissolveVal", e);

            await this.ToNextFrame();
            e += TimeDelta.Delta * .5f;
        }

    }
}
