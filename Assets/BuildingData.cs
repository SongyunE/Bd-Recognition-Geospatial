using System; // [Serializable]을 위해 추가

[Serializable]
public class BuildingData
{
    public string buildingName;
    public string description;
    public double latitude;
    public double longitude;
    public double altitude;
}