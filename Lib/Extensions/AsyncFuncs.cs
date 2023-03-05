using Godot;
using System;

public static class AsyncFuncs {

    public static SignalAwaiter ToTimeOut(this Node node, float time) {
        return node.ToSignal(node.GetTree().CreateTimer(time, false), "timeout");
    }

    public static SignalAwaiter ToNextFrame(this Node node) {
        if (node.GetTree().Paused) {
            return ToTimeOut(node, 0);
        }
        return node.ToSignal(node.GetTree(), "process_frame");
    }
}
