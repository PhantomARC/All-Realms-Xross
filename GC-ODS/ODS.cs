using Godot;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Text.Json;

namespace UARX.ODS;


public partial class ODS : Node {
  [Export] public string ODS_Path = "res://data/ARCDB.ods";
  private Dictionary<string, object> Data = [];
  private string Version = "";


  public override void _Ready() {
    ParseODS(ProjectSettings.GlobalizePath(ODS_Path));
  }


  private void ParseODS(string path) {
    if (!File.Exists(path)) {
      GD.PrintErr($"File DNE: {path}");
      return;
    }

    using (ZipArchive archive = ZipFile.OpenRead(path)) {
      ZipArchiveEntry zaar = archive.GetEntry("content.xml");
      if (zaar == null) {
        GD.PrintErr("Invalid ODS file: content.xml missing.");
        return;
      }
      XmlDocument xdoc = new();
      xdoc.Load(zaar.Open());
      XmlNodeList xmlsheets = xdoc.GetElementsByTagName("table:table");

      foreach (XmlNode sheet in xmlsheets) {
        string sheetName = sheet.Attributes?["table:name"]?.Value;
        if (string.IsNullOrEmpty(sheetName)) continue;
        XmlNodeList rows = ((XmlElement)sheet).GetElementsByTagName("table:table-row");
        if (sheetName == "VERSION") {
          XmlNodeList cells = ((XmlElement)rows[0]).GetElementsByTagName("table:table-cell");
          Version = cells[0].InnerText.Trim().Replace("/", ".");
        }
        else if (sheetName.StartsWith("SET.")) {
          List<List<string>> sheetData = [];
          bool cutR1 = true;

          foreach (XmlNode row in rows) {
            XmlNodeList cells = ((XmlElement)row).GetElementsByTagName("table:table-cell");
            if (cutR1) {
              cutR1 = false;
              sheetName = cells[0].InnerText.Trim();
              continue;
            }

            List<string> rowData = [];
            bool rowHasData = false;
            foreach (XmlNode cell in cells) {
              string cellText = cell.InnerText.Trim();
              rowData.Add(cellText);
              if (!string.IsNullOrEmpty(cellText)) rowHasData = true;
            }
            if (rowHasData) sheetData.Add(rowData);
          }

          Data[sheetName] = sheetData;
        }
      }
    }
    // Print to console and save to file
    GD.Print($"Version {Version}");
    var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    string prettyPrint = JsonSerializer.Serialize(Data, jsonOptions);
    GD.Print(prettyPrint);
  }
}
