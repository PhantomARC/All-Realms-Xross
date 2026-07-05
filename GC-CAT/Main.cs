using Godot;
using System;
using UARX;
using System.Buffers.Binary;
using System.Linq;

public partial class Main : Control {
  private const string SavePath = "user://manufacture_log.json";
  private const string ODS_Path = "res://data/ARCDB.ods";
  private const string PluginName = "ARXNFC";
  private GodotObject _nfc;
  private Button _prevCard;
  private Button _nextCard;
  private Button _prevSet;
  private Button _nextSet;
  private Button _xR;
  private Button _xW;
  private Button _xS;
  private Button _MFG;
  private Label _SN;
  private Label _CN;
  private Label _MN;
  private Label _xL;
  private RichTextLabel _RTL;

  private byte[] PAGE4 = [0x5F, 0x41, 0x52, 0x58]; //MFC
  private byte[] PAGE5; //MFI
  private byte[] PAGE6; //HXID
  private byte[] PAGE7; //RSS1
  private byte[] PAGE8; //RSS2
  private byte[] PAGE9; //PRNG

  public override void _Ready() {
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
    string xv = ODS.Parse(ODS_Path);
    GD.Print(ProjectSettings.GlobalizePath(ODS_Path));
    GD.Print(xv);
    if (xv == "PATH_DNE") return;
    MFL.LoadPath = ProjectSettings.GlobalizePath(SavePath);
    MFL.Load();
    _prevCard = GetNode<Button>("%PCard");
    _xR = GetNode<Button>("%xRead");
    _xW = GetNode<Button>("%xWrite");
    _xS = GetNode<Button>("%xSign");
    _nextCard = GetNode<Button>("%NCard");
    _prevSet = GetNode<Button>("%PSet");
    _nextSet = GetNode<Button>("%NSet");
    _MFG = GetNode<Button>("%MFGB");
    _SN = GetNode<Label>("%SetName");
    _CN = GetNode<Label>("%CardName");
    _MN = GetNode<Label>("%MFGN");
    _xL = GetNode<Label>("%xLabel");
    _RTL = GetNode<RichTextLabel>("%RTL");
    _prevCard.Pressed += () => ChangeCard(false);
    _nextCard.Pressed += () => ChangeCard(true);
    _prevSet.Pressed += () => ChangeSet(false);
    _nextSet.Pressed += () => ChangeSet(true);
    _xR.Pressed += () => _On_xRWS("READ");
    _xW.Pressed += () => _On_xRWS("WRITE");
    _xS.Pressed += () => _On_xRWS("SIGN");
    _MFG.Pressed += MFGPress;
    _xL.Text = "READ";
    _CN.Text = MFL.GetCurrentCard(true);
    _SN.Text = MFL.GetCurrentSet();
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }


  private void ChangeCard(bool cycle_forward) {
    _CN.Text = MFL.CycleCard(cycle_forward);
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }


  private void ChangeSet(bool cycle_forward) {
    MFL.CycleSet(cycle_forward);
    _CN.Text = MFL.GetCurrentCard(true);
    _SN.Text = MFL.GetCurrentSet();
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }


  private void MFGPress() {
    MFL.Add();
    SetBytes();
    WriteNFCPayload();
    _MN.Text = MFL.GetMFGText();
  }

  private void SetBytes() {
    var Q = MFL.GetCurrentCardData();
    PAGE5 = [0x00, 0x00];
    BinaryPrimitives.WriteInt16BigEndian(PAGE5, MFL.GetMFGCount());
    PAGE5 = PAGE5.Concat(Convert.FromHexString(Q[4])).ToArray();
    PAGE6 = Convert.FromHexString(Q[1]);
    PAGE7 = Convert.FromHexString(Q[2]);
    PAGE8 = Convert.FromHexString(Q[3]);
    PAGE9 = System.Security.Cryptography.RandomNumberGenerator.GetBytes(4);
  }

  public void WriteNFCPayload() {
    int[] ids = [4, 5, 6, 7, 8, 9];
    byte[][] byteArrays = [PAGE4,PAGE5,PAGE6,PAGE7,PAGE8,PAGE9];
    byte[] flattenedBytes = byteArrays.SelectMany(b => b).ToArray();
    int[] byteLengths = byteArrays.Select(b => b.Length).ToArray();
    _nfc?.Call("setWritePayload", ids, flattenedBytes, byteLengths);
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


  private void _On_xRWS(string mode) {
    _nfc?.Call("setOpMode", mode);
    _xL.Text = mode;
  }


  public override void _ExitTree() {
    if (_nfc != null) _nfc.Call("offNFC");
  }
}
