using System.IO;
using System.IO.Compression;
using System.Xml;
using Godot.Collections;
using Godot;

namespace UARX;


public class ODS {
  public static string Version = "";
  public static Dictionary<string, Array<Array<string>>> ODS_Data = [];


  public static Array<string> GetSetList() {
    return (Array<string>)ODS_Data.Keys;
  }


  public static Array<string> GetCardList(string set) {
    Array<string> cards = [];
    foreach (Array<string> c in ODS_Data[set]) cards.Add(c[1]);
    return cards;
  }

  public static Array<string> GetCardList(int setN) {
    Array<string> cards = [];
    string set = GetSetList()[setN];
    foreach (Array<string> c in ODS_Data[set]) cards.Add(c[1]);
    return cards;
  }


  public static Array<string> FetchCard(int setN, int id) {
    string set = GetSetList()[setN];
    return ODS_Data[set][id];
  }



  public static string Parse(string path) {
    if (!Godot.FileAccess.FileExists(path)) return "PATH_DNE";
    ZipReader zipReader = new ZipReader();
    Error err = zipReader.Open(path);
    if (err != Error.Ok) return "ZIP_OPEN_ERROR";
    byte[] xmlBytes = zipReader.ReadFile("content.xml");
    zipReader.Close();
    if (xmlBytes == null || xmlBytes.Length == 0) return "NULL_FILE";
    XmlDocument xdoc = new();
    using (MemoryStream ms = new MemoryStream(xmlBytes)) xdoc.Load(ms);
    XmlNodeList xmls = xdoc.GetElementsByTagName("table:table");
    foreach (XmlNode sheet in xmls) {
      string sheetName = sheet.Attributes?["table:name"]?.Value;
      if (string.IsNullOrEmpty(sheetName)) continue;
      XmlNodeList rows = ((XmlElement)sheet).GetElementsByTagName("table:table-row");
      if (sheetName == "VERSION") {
        XmlNodeList cells = ((XmlElement)rows[0]).GetElementsByTagName("table:table-cell");
        Version = cells[0].InnerText.Trim().Replace("/", ".");
      }
      else if (sheetName.StartsWith("SET.")) {
        Array<Array<string>> sheetData = [];
        bool cutR1 = true;
        foreach (XmlNode row in rows) {
          XmlNodeList cells = ((XmlElement)row).GetElementsByTagName("table:table-cell");
          if (cutR1) {
            cutR1 = false;
            sheetName = cells[0].InnerText.Trim();
            continue;
          }
          Array<string> block = [];
          foreach (XmlNode cell in cells) {
            string cTex = cell.InnerText.Trim();
            int rCt = 1;
            XmlAttribute rpA = cell.Attributes?["table:number-columns-repeated"];
            if (rpA != null && int.TryParse(rpA.Value, out int pCt)) rCt = (string.IsNullOrEmpty(cTex) && pCt > 100) ? 1 : pCt;
            for (int i = 0; i < rCt; i++) if (!string.IsNullOrEmpty(cTex)) block.Add(cTex);
          }
          if (block.Count != 0) {
            for (int i = 0; i < 5; i++) block.RemoveAt(0); //terminate first 5 values of set values
            sheetData.Add(block);
          }
        }
        ODS_Data[sheetName] = sheetData;
      }
    }
    return "OK";
  }
}
