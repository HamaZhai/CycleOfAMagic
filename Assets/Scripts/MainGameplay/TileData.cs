using UnityEngine;

[System.Serializable]
public class TileData
{
    public string tileName;    // ���������� ���
    public TileZone zone;      // ����
    public string effect = ""; // ���� ��� ������

    public TileData(string name, TileZone zone)
    {
        tileName = name;
        this.zone = zone;
        effect = "";
    }
}