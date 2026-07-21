using Godot;
using System;
using UARX;
using System.Buffers.Binary;
using System.Linq;
using System.Text;


public partial class rwsl : Control {
  private enum ModeType { SIGN, READ, WRITE, LOG }
  private const string SavePath = "user://manufacture_log.json";
  private const string ODS_Path = "res://data/ARCDB.ods";
  private const string PluginName = "ARXNFC";
  private GodotObject _nfc;
  private readonly System.Collections.Generic.Dictionary<int, Texture2D> _factions = [];
  private readonly System.Collections.Generic.Dictionary<int, Texture2D> _runes = [];
  private Texture2D Status_Good;
  private Texture2D Status_Load;
  [Export] private Color GreenColor = new Color(0f, 1f, 0f, 1f);
  [Export] private Color BlueColor = new Color(0f, 0.5f, 1f, 1f);

  // Timing parameters
  [Export] private double LerpDuration = 0.3; // Time to fade in/out
  [Export] private double HoldDelay = 1.0;    // Time to stay active before reverting

  private Color _initialColor = new Color(0f, 0f, 0f, 0f); // Transparent
  private Tween _activeTween;
  private Tween _writeTween;

  //BYTEARRAY
  private byte[] PAGE4 = [0x5F, 0x41, 0x52, 0x58]; //MFC
  private byte[] PAGE5; //MFI
  private byte[] PAGE6; //HXID
  private byte[] PAGE7; //RSS1
  private byte[] PAGE8; //RSS2
  private byte[] PAGE9; //PRNG
  private byte[] READBLOCK;

  // Unique Accessors
  private Button _mode;
  private RichTextLabel _log, _wPayload;
  private Label _wSn, _wCn, _wMfid, _rMfgId, _rPrint, _rSet, _rNum, _rName, _rFinish, _rAlter;
  private Label _rAtk, _rDef, _rSpe, _rMag, _rLgc, _rVit, _rHash;
  private TextureRect _wStatus, _sIndicator, _rFaction, _r1, _r2, _r3, _r4, _r5, _r6;
  private ColorRect _sColor, _modeLog, _modeWrite, _modeSign, _modeRead;
  private Button _wPset, _wNset, _wPcard, _wNcard, _readClear;

  private ModeType _currentMode = ModeType.LOG;

  public override void _Ready() {
    //LOG INIT
    _log = GetNode<RichTextLabel>("%log");

    //NFC INIT
    if (Engine.HasSingleton(PluginName)) {
      _nfc = Engine.GetSingleton(PluginName);
      _nfc.Connect("tag_discovered", Callable.From<string, string>(_OnTagDiscovered));
      _nfc.Connect("tag_lost", Callable.From(_OnTagLost));
      _nfc.Connect("nfc_error", Callable.From<string>(_OnNfcError));
      _nfc.Connect("tag_read", Callable.From<string>(_OnTagRead));
      _nfc.Connect("nfc_sign_status", Callable.From<bool>(_OnNFCSign));
      _nfc.Connect("nfc_write_status", Callable.From<bool>(_OnNFCWrite));
      _nfc.Call("onNFC");
    }
    else {
      Log("GodotNFC plugin not found.");
    }
    string xv = ODS.Parse(ODS_Path);
    Log(ProjectSettings.GlobalizePath(ODS_Path));
    Log(xv);
    if (xv == "PATH_DNE") return;
    MFL.LoadPath = ProjectSettings.GlobalizePath(SavePath);
    MFL.Load();

    // Unique Accessors (%): UI Controls
    _mode = GetNode<Button>("%mode");

    _wPayload = GetNode<RichTextLabel>("%w_payload");
    _wSn = GetNode<Label>("%w_sn");
    _wCn = GetNode<Label>("%w_cn");
    _wMfid = GetNode<Label>("%w_mfid");
    _rMfgId = GetNode<Label>("%r_mfg_id");
    _rPrint = GetNode<Label>("%r_print");
    _rSet = GetNode<Label>("%r_set");
    _rNum = GetNode<Label>("%r_num");
    _rName = GetNode<Label>("%r_name");
    _rFinish = GetNode<Label>("%r_finish");
    _rAlter = GetNode<Label>("%r_alter");
    _rAtk = GetNode<Label>("%r_atk");
    _rDef = GetNode<Label>("%r_def");
    _rSpe = GetNode<Label>("%r_spe");
    _rMag = GetNode<Label>("%r_mag");
    _rLgc = GetNode<Label>("%r_lgc");
    _rVit = GetNode<Label>("%r_vit");
    _rHash = GetNode<Label>("%r_hash");
    _wStatus = GetNode<TextureRect>("%w_status");
    _sIndicator = GetNode<TextureRect>("%s_indicator");
    _rFaction = GetNode<TextureRect>("%r_faction");
    _r1 = GetNode<TextureRect>("%r_1");
    _r2 = GetNode<TextureRect>("%r_2");
    _r3 = GetNode<TextureRect>("%r_3");
    _r4 = GetNode<TextureRect>("%r_4");
    _r5 = GetNode<TextureRect>("%r_5");
    _r6 = GetNode<TextureRect>("%r_6");
    _sColor = GetNode<ColorRect>("%s_color");

    // Unique Accessors (%): Mode Containers & Action Buttons
    _modeLog = GetNode<ColorRect>("%mode_log");
    _modeWrite = GetNode<ColorRect>("%mode_write");
    _modeSign = GetNode<ColorRect>("%mode_sign");
    _modeRead = GetNode<ColorRect>("%mode_read");
    _wPset = GetNode<Button>("%w_pset");
    _wNset = GetNode<Button>("%w_nset");
    _wPcard = GetNode<Button>("%w_pcard");
    _wNcard = GetNode<Button>("%w_ncard");
    _readClear = GetNode<Button>("%read_clear");

    // Signal Bindings
    _mode.Pressed += Mode;
    _wPset.Pressed += () => CycleSet(false);
    _wNset.Pressed += () => CycleSet(true);
    _wPcard.Pressed += () => CycleCard(false);
    _wNcard.Pressed += () => CycleCard(true);
    _readClear.Pressed += ReadClear;

    // Params
    _wCn.Text = MFL.GetCurrentCard(true);
    _wSn.Text = MFL.GetCurrentSet();
    _wMfid.Text = MFL.GetMFGText();
    _wPayload.Text = MFL.GetCurrentCardData().ToString();
    Status_Good = GD.Load<Texture2D>("res://WCM.png");
    Status_Load = GD.Load<Texture2D>("res://WEM.png");

    UpdateModeUI();

    ScanFolder("res://factions", _factions);
    ScanFolder("res://runes", _runes);

  }


  private void ScanFolder(string path, System.Collections.Generic.Dictionary<int, Texture2D> dict) {
    using var dir = DirAccess.Open(path);
    if (dir == null) return;

    dir.ListDirBegin();
    string file = dir.GetNext();

    while (!string.IsNullOrEmpty(file)) {
      if (!dir.CurrentIsDir()) {
        if (file.EndsWith(".import")) file = file.TrimEnd(".import".ToCharArray());
        if (file.EndsWith(".png") || file.EndsWith(".jpg")) {
          string nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file);
          if (int.TryParse(nameWithoutExt, out int id)) dict[id] = GD.Load<Texture2D>($"{path}/{file}");
        }
      }
      file = dir.GetNext();
    }
  }


  private void Mode() {
    _currentMode = (ModeType)(((int)_currentMode + 1) % 4);
    UpdateModeUI();
  }

  private void UpdateModeUI() {
    _nfc?.Call("setOpMode", _currentMode.ToString());
    _mode.Text = $"MODE : {_currentMode}";
    _modeSign.Visible = _currentMode == ModeType.SIGN;
    _modeRead.Visible = _currentMode == ModeType.READ;
    _modeWrite.Visible = _currentMode == ModeType.WRITE;
    _modeLog.Visible = _currentMode == ModeType.LOG;
  }

  private void CycleSet(bool isNext) {
    MFL.CycleSet(isNext);
    _wCn.Text = MFL.GetCurrentCard(true);
    _wSn.Text = MFL.GetCurrentSet();
    _wMfid.Text = MFL.GetMFGText();
    _wPayload.Text = MFL.GetCurrentCardData().ToString();
  }
  private void CycleCard(bool isNext) {
    _wCn.Text = MFL.CycleCard(isNext);
    _wMfid.Text = MFL.GetMFGText();
    _wPayload.Text = MFL.GetCurrentCardData().ToString();
  }
  private void ReadClear() {
    READBLOCK = null;
    UpdateReader();
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
    byte[][] byteArrays = [PAGE4, PAGE5, PAGE6, PAGE7, PAGE8, PAGE9];
    byte[] flattenedBytes = byteArrays.SelectMany(b => b).ToArray();
    int[] byteLengths = byteArrays.Select(b => b.Length).ToArray();
    _nfc?.Call("setWritePayload", ids, flattenedBytes, byteLengths);
  }


  private void _OnTagDiscovered(string uid, string tagType) {
    Log($"Tag found with UID: {uid}");
    Log($"Tag Encryption Status: {tagType}");
  }


  private void _OnTagRead(string data) {
    Log(data);
    READBLOCK = data
    .Trim('[', ']')
    .Split(',', StringSplitOptions.TrimEntries)
    .Select(hex => Convert.ToByte(hex, 16))
    .ToArray();
    UpdateReader();
  }

  private void UpdateReader() {
	byte[] BLANK = new byte[] { 0x03, 0x00, 0xFE, 0x00 };
    if (READBLOCK == null || READBLOCK.AsSpan(0, 4).SequenceEqual(BLANK)) {
      _rMfgId.Text = "";
      _rPrint.Text = "";
      _rSet.Text = "";
      _rName.Text = "";
      _rNum.Text = "";
      _rFinish.Text = "";
      _rAlter.Text = "";
      _rAtk.Text = "";
      _rDef.Text = "";
      _rSpe.Text = "";
      _rMag.Text = "";
      _rLgc.Text = "";
      _rVit.Text = "";
      _rHash.Text = "";
      _rFaction.Texture = null;
      _r1.Texture = null;
      _r2.Texture = null;
      _r3.Texture = null;
      _r4.Texture = null;
      _r5.Texture = null;
      _r6.Texture = null;
    }
    else {
      //page 4
      _rMfgId.Text = Encoding.ASCII.GetString(READBLOCK, 0, 4);
      //page 5
      _rPrint.Text = ((READBLOCK[4] << 8) | READBLOCK[5]).ToString("D4");
      _rSet.Text = ODS.ODS_Data.ElementAt(READBLOCK[6]).Key;
      _rNum.Text = READBLOCK[7].ToString();
      //page 6
      _rName.Text = ODS.ODS_Data.ElementAt(READBLOCK[6]).Value[READBLOCK[7] - 1][0];
      _rFaction.Texture = _factions[READBLOCK[10] + 1];
      _rFinish.Text = (READBLOCK[11] >> 4).ToString("X");
      _rAlter.Text = (READBLOCK[11] & 0x0F).ToString("X");
      // page 7
      string cset = IB6S(((READBLOCK[12] << 8) | READBLOCK[13]), 6);
      int[] digits = new int[6];
      for (int i = 0; i < 6; i++) digits[i] = cset[i] - '0';
      _r1.Texture = _runes[digits[0] + 1];
      _r2.Texture = _runes[digits[1] + 1];
      _r3.Texture = _runes[digits[2] + 1];
      _r4.Texture = _runes[digits[3] + 1];
      _r5.Texture = _runes[digits[4] + 1];
      _r6.Texture = _runes[digits[5] + 1];
      _rAtk.Text = READBLOCK[14].ToString();
      _rDef.Text = READBLOCK[15].ToString();
      _rSpe.Text = READBLOCK[16].ToString();
      _rMag.Text = READBLOCK[17].ToString();
      _rLgc.Text = READBLOCK[18].ToString();
      _rVit.Text = READBLOCK[19].ToString();
      _rHash.Text = string.Concat(READBLOCK.Skip(20).Take(4).Select(b => b.ToString("X2")));
    }
  }

  private string IB6S(int value, int minDigits = 6) {
    if (value == 0) return new string('0', minDigits);
    string result = "";
    while (value > 0) {
      int remainder = value % 6;
      result = remainder + result;
      value /= 6;
    }
    return result.PadLeft(minDigits, '0');
  }

  private void _OnTagLost() {
    Log("Connection to tag was abruptly closed.");
  }


  private void _OnNfcError(string msg) {
    Log($"NFC Error: {msg}");
  }

  private void _OnNFCSign(bool status) {
    Log($"Sign Status: {status}");
    if (_activeTween != null && _activeTween.IsRunning()) _activeTween.Kill();
    _sIndicator.Texture = Status_Good;
    Color targetColor = status ? GreenColor : BlueColor;
    _activeTween = CreateTween();
    _activeTween.TweenProperty(_sColor, "color", targetColor, LerpDuration);
    _activeTween.TweenInterval(HoldDelay);
    _activeTween.TweenProperty(_sColor, "color", _initialColor, LerpDuration);
    _activeTween.TweenCallback(Callable.From(() => {_sIndicator.Texture = Status_Load;}));
  }

  private void _OnNFCWrite(bool status) {
	if (!status) return;
    MFL.Add();
    SetBytes();
    WriteNFCPayload();
    _wMfid.Text = MFL.GetMFGText();
	if (_writeTween != null && _writeTween.IsRunning()) _writeTween.Kill();
    _wStatus.Texture = Status_Good;
    _writeTween = CreateTween();
    _writeTween.TweenInterval(HoldDelay);
    _writeTween.TweenCallback(Callable.From(() => {_wStatus.Texture = Status_Load;}));
  }


  public override void _ExitTree() {
    if (_nfc != null) _nfc.Call("offNFC");
  }

  private void Log(string message) {
    _log.AppendText($"{message}\n");
  }
}