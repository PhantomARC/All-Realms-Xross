using Godot;
using System;
using UARX;
using System.Buffers.Binary;
using System.Linq;

public partial class Main : Control {
  private const string SavePath = "user://manufacture_log.json";
  private const string ODS_Path = "res://data/ARCDB.ods";
  private Button _prevCard;
  private Button _nextCard;
  private Button _prevSet;
  private Button _nextSet;
  private Button _MFG;
  private Label _SN;
  private Label _CN;
  private Label _MN;
  private RichTextLabel _RTL;

  private byte[] PAGE4 = [0x5F, 0x41, 0x52, 0x58]; //MFC
  private byte[] PAGE5; //MFI
  private byte[] PAGE6; //HXID
  private byte[] PAGE7; //RSS1
  private byte[] PAGE8; //RSS2
  private byte[] PAGE9; //PRNG

  public override void _Ready() {
    ODS.Parse(ProjectSettings.GlobalizePath(ODS_Path));
    MFL.LoadPath = ProjectSettings.GlobalizePath(SavePath);
    MFL.Load();
    _prevCard = GetNode<Button>("%PCard");
    _nextCard = GetNode<Button>("%NCard");
    _prevSet = GetNode<Button>("%PSet");
    _nextSet = GetNode<Button>("%NSet");
    _MFG = GetNode<Button>("%MFGB");
    _SN = GetNode<Label>("%SetName");
    _CN = GetNode<Label>("%CardName");
    _MN = GetNode<Label>("%MFGN");
    _RTL = GetNode<RichTextLabel>("%RTL");
    _prevCard.Pressed += PCPress;
    _nextCard.Pressed += NCPress;
    _prevSet.Pressed +=  PSPress;
    _nextSet.Pressed += NSPress;
    _MFG.Pressed += MFGPress;

    _CN.Text = MFL.GetCurrentCard(true);
    _SN.Text = MFL.GetCurrentSet();
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }
  

  private void PCPress() {
    _CN.Text = MFL.CycleCard(false);
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }
  
  private void NCPress() {
    _CN.Text = MFL.CycleCard(true);
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }

  private void PSPress() {
    MFL.CycleSet(false);
    _CN.Text = MFL.GetCurrentCard(true);
    _SN.Text = MFL.GetCurrentSet();
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }

  private void NSPress() {
    MFL.CycleSet(true);
    _CN.Text = MFL.GetCurrentCard(true);
    _SN.Text = MFL.GetCurrentSet();
    _MN.Text = MFL.GetMFGText();
    _RTL.Text = MFL.GetCurrentCardData().ToString();
  }

  private void MFGPress() {
    MFL.Add();
    SetBytes();
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
}
