using Godot;
using System;

public partial class Main : Control {
  private Label _l;
  private const string PluginName = "ARXNFC";
  private GodotObject _nfc; // Android singletons are accessed as GodotObject in C#

  public override void _Ready() {
    _l = GetNode<Label>("L");

    if (Engine.HasSingleton(PluginName)) {
      _nfc = Engine.GetSingleton(PluginName);


      _nfc.Connect("tag_discovered", Callable.From<string, string>(_OnTagDiscovered));
      _nfc.Connect("tag_lost", Callable.From(_OnTagLost));
      _nfc.Connect("nfc_error", Callable.From<string>(_OnNfcError));
      _nfc.Connect("tag_read", Callable.From<string>(_OnTagRead));


      _nfc.Call("onNFC");
    }
    else {
      GD.Print("GodotNFC plugin not found.");
    }
  }

  private void _OnTagDiscovered(string uid, string tagType) {
    GD.Print("Tag found with UID: ", uid);
    GD.Print("Tag Encryption Status: ", tagType);
  }

  private void _OnTagRead(string data) {
    GD.Print(data);
  }

  private void _OnTagLost() {
    GD.Print("Tag connection lost (User pulled phone away).");
  }

  private void _OnNfcError(string msg) {
    GD.Print("NFC Error: ", msg);
  }

  public override void _ExitTree() {
    if (_nfc != null) {
      _nfc.Call("offNFC");
    }
  }


  private void _on_read_pressed() {
    _l.Text = "READ 227";
    _nfc?.Call("setOpMode", "READ");
  }


  private void _on_write_pressed() {
    _l.Text = "WRITE 227";
    _nfc?.Call("setOpMode", "WRITE");
  }


  private void _on_sign_pressed() {
    _l.Text = "SIGN 227";
    _nfc?.Call("setOpMode", "SIGN");
  }
}
