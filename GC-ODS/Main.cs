using Godot;
using UARX;

public partial class Main : Node {
  private const string ODS_Path = "res://data/ARCDB.ods";
  public override void _Ready() {
    string p_res = ODS.Parse(ProjectSettings.GlobalizePath(ODS_Path));
    if (p_res == "OK") {
      GD.Print(ODS.Version);
      GD.Print(ODS.ODS_Data);
    }
  }
}
