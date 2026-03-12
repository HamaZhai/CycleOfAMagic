using UnityEngine;

[System.Serializable]
public class TileData
{
    public string tileName;    // уникальное имя
    public TileZone zone;      // зона
    public string effect = ""; // слот под эффект

    public TileData(string name, TileZone zone)
    {
        tileName = name;
        this.zone = zone;
        effect = "";
    }
}