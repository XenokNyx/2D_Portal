using Godot;
using System;

public partial class RoomCheck : Node, I_Activable {
    [Export] bool _defaultState;


    bool _isInEntrance;

    public bool IsActive { get; private set; }
    System.Action _onStateChange;

    public override void _Ready() {
        SetDefaultState();
    }

    async void SetDefaultState() {
        IsActive = _defaultState;

        await this.ToNextFrame();

        _onStateChange?.Invoke();
    }

    void _on_exit_area_body_exited(Player player) {
        if (!_isInEntrance) {
            IsActive = !_defaultState;
            _onStateChange?.Invoke();
        }

    }

    void _on_entrance_area_body_entered(Player player) {
        _isInEntrance = true;
    }

    void _on_entrance_area_body_exited(Player player) {
        _isInEntrance = false;
    }

    public void AddOnStateChange(Action action) {
        _onStateChange += action;
    }
}
