using Godot;
using System;

public partial class PortalBorder : Node {

    [Export] Portal _portal;
    [Export] PackedScene _linePck;

    [Export] bool _usePortalColor;

    Line2D[] _line;

    public override void _Ready() {
        _portal.AddOnMaskPolygonChange(OnMaskChange);
        _portal.AddOnMaskInvisible(OnMaskInvisible);
        _portal.AddOnMaskCreated(CreateLines);


    }

    void CreateLines(Polygon2D[] masks) {
        _line = new Line2D[masks.Length];
        for (int i = 0; i < _line.Length; i++) {
            _line[i] = _linePck.Instantiate<Line2D>();
            _line[i].Points = new Vector2[5];

            masks[i].AddChild(_line[i]);

            if (_usePortalColor) {
                _line[i].DefaultColor = _portal.SelfModulate;
            }
        }
    }

    void OnMaskChange(int ind, Polygon2D mask) {
        _line[ind].Visible = true;

        for (int i = 0; i < mask.Polygon.Length - 1; i++) {
            _line[ind].SetPointPosition(i, mask.Polygon[i + 1]);
        }

        _line[ind].SetPointPosition(_line[ind].Points.Length - 1, mask.Polygon[0]);
    }

    void OnMaskInvisible(int ind) {
        _line[ind].Visible = false;
    }
}
