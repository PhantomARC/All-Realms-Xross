using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

using System.Text.Json;

namespace UARX;


public class ODS {
  
  private readonly JsonSerializerOptions _jsO = new() { WriteIndented = true };
  public string Version = "";
  public Dictionary<string, object> ODS_Data = [];
  
  

  public string Parse(string path) {
    if (!File.Exists(path)) return "PATH_DNE";
    using ZipArchive archive = ZipFile.OpenRead(path);
    ZipArchiveEntry zaar = archive.GetEntry("content.xml");
    if (zaar == null) return "NULL_FILE";
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
        ODS_Data[sheetName] = sheetData;
      }
    }
    return "OK";
  }

  public string FormatODS() {
    return JsonSerializer.Serialize(ODS_Data, _jsO);
  }
}
