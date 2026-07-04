using Godot;
using UARX;

public partial class Main : Node {
  private ODS ds;
  private static readonly string ODS_Path = "res://data/ARCDB.ods";
  public override void _Ready() {
    ds = new();
    string p_res = ds.Parse(ProjectSettings.GlobalizePath(ODS_Path));
    if (p_res == "OK") {
      GD.Print(ds.Version);
      GD.Print(ds.FormatODS());
    }
  }
}
