using Godot;
using System;

public partial class SocialBtn : Node {
    void _on_btn_mastodon_pressed() {
        OS.ShellOpen("https://mastodon.gamedev.place/@XenokDev");
    }

    void _on_bnt_twitter_pressed() {
        OS.ShellOpen("https://twitter.com/XenokDev");
    }
}
