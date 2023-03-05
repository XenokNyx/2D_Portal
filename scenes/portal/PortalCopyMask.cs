using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PortalCopyMask : Node2D {

    [Export] Portal _portal;

    Dictionary<Node2D, SpriteGroup> _copys = new Dictionary<Node2D, SpriteGroup>();

    HashSet<Sprite2D> _sprites = new HashSet<Sprite2D>();
    HashSet<Node2D> _nodes = new HashSet<Node2D>();

    public override void _Ready() {
        Visible = false;

        _portal.AddOnEntityEnter(AddCopy);
        _portal.AddOnEntityExit(RemoveCopy);
        _portal.AddOnEntityMove(UpdateCopy);
    }

    // public override void _Process(double delta) {

    //     foreach (var copy in _copys) {
    //         copy.Value.GlobalPosition = copy.Key.GlobalPosition;
    //     }
    // }

    void UpdateCopy(Node2D copy, Vector2 relativePos, float relativeRotation) {
        _copys[copy].UpdatePosition(relativePos, copy.GlobalRotation + relativeRotation);
    }

    void AddCopy(Node2D node, Vector2 relativePos, float relativeRotation) {

        var nodeSprites = node.FindChildren("*", "Sprite2D");

        SpriteGroup group = new SpriteGroup(nodeSprites.Count, GetNode());

        for (int i = 0; i < nodeSprites.Count; i++) {
            group.SetSprite(i, nodeSprites[i] as Sprite2D, GetSprite());
        }

        _copys.Add(node, group);

        UpdateCopy(node, relativePos, relativeRotation);

        Visible = true;
    }

    void RemoveCopy(Node2D node) {

        foreach (var sprite in _copys[node].Sprites) {
            RemoveSprite(sprite);
        }

        RemoveNode(_copys[node]._base);

        _copys.Remove(node);

        if (_copys.Count == 0) {
            Visible = false;
        }
    }

    Node2D GetNode() {
        Node2D found;
        if (_nodes.Count > 0) {
            found = _nodes.First();
            _nodes.Remove(found);
        } else {
            found = new Node2D();
        }

        AddChild(found);

        return found;
    }

    Sprite2D GetSprite() {
        Sprite2D found;
        if (_sprites.Count > 0) {
            found = _sprites.First();
            _sprites.Remove(found);
        } else {
            found = new Sprite2D();
        }

        return found;
    }

    void RemoveNode(Node2D node) {
        _nodes.Add(node);
        node.GetParent().RemoveChild(node);
    }
    void RemoveSprite(Sprite2D sprite) {
        sprite.Texture = null;
        _sprites.Add(sprite);
        sprite.GetParent().RemoveChild(sprite);
    }

    struct SpriteGroup {

        public Node2D _base;
        public Sprite2D[] Sprites;

        public SpriteGroup(int size, Node2D baseCopy) {
            _base = baseCopy;
            Sprites = new Sprite2D[size];
        }

        public void SetSprite(int i, Sprite2D original, Sprite2D copy) {
            copy.Texture = original.Texture;
            copy.RegionEnabled = original.RegionEnabled;
            copy.RegionRect = original.RegionRect;

            copy.Modulate = original.Modulate;
            copy.SelfModulate = original.SelfModulate;

            copy.GlobalScale = original.GlobalScale;

            copy.ZIndex = original.ZIndex;
            copy.ShowBehindParent = original.ShowBehindParent;

            copy.Visible = original.Visible;

            if (i > 0) {
                Sprites[i - 1].AddChild(copy);
            } else {
                _base.AddChild(copy);
            }

            Sprites[i] = copy;

            copy.Position = original.Position;
            copy.Rotation = original.Rotation;
        }

        public void UpdatePosition(Vector2 relativePos, float relativeRotation) {
            _base.Position = relativePos;
            _base.GlobalRotation = relativeRotation;

        }
    }

}
