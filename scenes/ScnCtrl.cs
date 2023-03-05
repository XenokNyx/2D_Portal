using Godot;
using System;
using System.Threading.Tasks;

public partial class ScnCtrl : Node {

    public static ScnCtrl INSTANCE;

    [Export] public Player Player { get; private set; }
    [Export] PackedScene _startMap;
    [Export] AnimationPlayer _animationPlayer;

    [Export] Control _startMenu;
    [Export] Control _endMenu;

    const string MAPS_DIR = "scenes/play/";
    const string DIR_END = ".tscn";

    static PackedScene _currentScene;
    static MapData _currentMap;

    bool _started = false;
    bool _endeded = false;

    public override void _Ready() {
        INSTANCE = this;
        _endMenu.Visible = false;
    }

    public override void _Input(InputEvent @event) {
        if (@event is InputEventKey && @event.IsPressed()) {

            if (!_started) {
                _started = true;

                if (_startMap != null) {
                    ChangePlayScene(_startMap);
                } else {
                    ChangePlayScene("map_01");
                }

                _startMenu.Visible = false;

            } else if (_endeded) {
                _started = false;
                _startMenu.Visible = true;
                _endMenu.Visible = false;
                _endeded = false;
            }
        }
    }

    public static void ChangePlayScene(string map) {
        var nextMap = GD.Load<PackedScene>(MAPS_DIR + map + DIR_END);
        ChangePlayScene(nextMap);
    }

    public static void ChangePlayScene(PackedScene nextMap) {
        INSTANCE.ChangeScene(nextMap);
    }

    async void ChangeScene(PackedScene nextMap) {

        Player.Active = false;


        if (_currentMap != null) {

            _animationPlayer.Play("EndChamber");
            while (_animationPlayer.IsPlaying()) {
                await this.ToNextFrame();
            }

            _currentMap.QueueFree();
            _currentMap = null;
        }

        await this.ToTimeOut(.5f);

        _currentScene = nextMap;

        Player.GlobalPosition = Vector2.Zero;
        Player.ClearPortals(false);

        if (_currentScene != null) {
            await LodMap();
        } else {
            _endMenu.Visible = true;
            _endeded = true;
        }

    }

    private async Task LodMap() {

        _currentMap = _currentScene.Instantiate<MapData>();
        Player.CanShootPortals = _currentMap.CanShootPortals;

        GetViewport().GetCamera2D().GlobalRotation = 0;

        AddChild(_currentMap);

        await this.ToNextFrame();

        Player.Active = true;

        _animationPlayer.Play("StartChamber");
        while (_animationPlayer.IsPlaying()) {
            await this.ToNextFrame();
        }
    }

    public static void ResetScene() {
        ChangePlayScene(_currentScene);
    }
}
