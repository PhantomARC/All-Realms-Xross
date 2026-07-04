using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.Json;
using Godot.Collections;
using Godot;
namespace UARX;


public static class MFL {
  public static string LoadPath = "";
  public static System.Collections.Generic.Dictionary<string, 
      System.Collections.Generic.Dictionary<string, int>> MFD = [];
  private static int Set = 0;
  private static int Card = 0;
  private static int SetCount = 0;
  private static int CardCount = 0;


  public static void Log() {
    string jStr = JsonSerializer.Serialize(MFD, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(LoadPath, jStr);
  }


  public static void Load() {
    if (!File.Exists(LoadPath)) Log();
    try {
      string jStr = File.ReadAllText(LoadPath);
      MFD = JsonSerializer.Deserialize<
          System.Collections.Generic.Dictionary<string, 
          System.Collections.Generic.Dictionary<string, int>>>(jStr) ?? [];
      foreach (var (key, value) in ODS.ODS_Data) {
        MFD.TryAdd((string)key,[]);
        foreach (Array<string> entry in value) MFD[key].TryAdd(entry[4],0);
      }
      Log();
      SetCount = ODS.GetSetList().Count;
      CardCount = ODS.GetCardList(0).Count;
    }
    catch (SystemException e) {
      GD.Print(e);
    }
  }


  public static void Add() {
    string set = GetCurrentSet();
    string id = GetCurrentCard();
    MFD.TryAdd(set, []);
    MFD[set].TryAdd(id, 0);
    MFD[set][id]++;
    Log();
  }


  public static string CycleCard(bool next) {
    Card += next ? 1 : -1;
    if (Card < 0) Card += CardCount;
    Card = Card % CardCount;
    return GetCurrentCard(true);
  }


  public static void CycleSet(bool next) {
    Card = 0;
    Set += next ? 1 : -1;
    if (Set < 0) Set += SetCount;
    Set = Set % SetCount;
    CardCount = ODS.GetCardList(Set).Count;
  }

  public static Array<string> GetCurrentCardData() {
    return ODS.FetchCard(Set,Card);
  }

  public static string GetCurrentCard(bool pname = false) {
    return ODS.FetchCard(Set,Card)[pname ? 0 : 4];
  }

  public static string GetCurrentSet() {
    return ODS.GetSetList()[Set];
  }

  public static string GetMFGText() {
    return MFD[GetCurrentSet()][GetCurrentCard()].ToString();
  }

  public static short GetMFGCount() {
    return (short)MFD[GetCurrentSet()][GetCurrentCard()];
  }
}