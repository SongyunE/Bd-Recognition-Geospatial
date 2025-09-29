using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Google.XR.ARCoreExtensions;
using TMPro;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class GeospatialManager : MonoBehaviour
{
    [Header("AR Components")]
    public AREarthManager EarthManager;
    public ARAnchorManager AnchorManager;

    [Header("UI")]
    public TextMeshProUGUI StatusText;
    public GameObject buildingInfoPanel;
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI buildingDescriptionText;

    [Header("Geospatial Content")]
    public List<BuildingData> buildingList;
    public float detectionRadius = 160.0f;
    public float detectionAngle = 60.0f;
    public float verticalAngleLimit = 60.0f;

    // --- 내부 변수 ---
    private Transform _cameraTransform;
    private BuildingData _currentlyDisplayedBuilding;
    private Dictionary<string, ARGeospatialAnchor> _buildingAnchors = new Dictionary<string, ARGeospatialAnchor>();

    IEnumerator Start()
    {
        _cameraTransform = Camera.main.transform;
        if (buildingInfoPanel != null) buildingInfoPanel.SetActive(false);

        // --- 권한 요청 및 초기화 ---
        StatusText.text = "Checking location permission...";
        if (!Input.location.isEnabledByUser)
        {
            StatusText.text = "User has not enabled location services.";
        }
        Input.location.Start();
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            StatusText.text = "Location service failed to initialize.";
            yield break;
        }

        while (EarthManager.EarthTrackingState != TrackingState.Tracking)
        {
            StatusText.text = $"Earth Tracking State: {EarthManager.EarthTrackingState}";
            yield return new WaitForSeconds(1.0f);
        }
        StatusText.text = "Tracking established. Creating anchors...";
        CreateAllBuildingAnchors();
    }

    void CreateAllBuildingAnchors()
    {
        foreach (var building in buildingList)
        {
            if (!_buildingAnchors.ContainsKey(building.buildingName))
            {
                ARGeospatialAnchor anchor = AnchorManager.AddAnchor(building.latitude, building.longitude, building.altitude, Quaternion.identity);
                if (anchor != null)
                {
                    _buildingAnchors.Add(building.buildingName, anchor);
                }
            }
        }
    }

    void Update()
    {
        if (EarthManager == null || EarthManager.EarthState != EarthState.Enabled) return;

        string status = $"Earth Tracking State: {EarthManager.EarthTrackingState}";
        if (StatusText != null) StatusText.text = status;

        if (EarthManager.EarthTrackingState == TrackingState.Tracking)
        {
            BuildingData newTarget = FindBestTargetFromExistingAnchors();

            if (newTarget != null)
            {
                if (_currentlyDisplayedBuilding == null || _currentlyDisplayedBuilding.buildingName != newTarget.buildingName)
                {
                    _currentlyDisplayedBuilding = newTarget;
                    UpdateBuildingInfo(_currentlyDisplayedBuilding);
                    if (buildingInfoPanel != null) buildingInfoPanel.SetActive(true);
                }
            }
            else
            {
                if (_currentlyDisplayedBuilding != null)
                {
                    _currentlyDisplayedBuilding = null;
                    if (buildingInfoPanel != null) buildingInfoPanel.SetActive(false);
                }
            }
        }
    }

    void UpdateBuildingInfo(BuildingData data)
    {
        if (buildingNameText != null) buildingNameText.text = data.buildingName;
        if (buildingDescriptionText != null) buildingDescriptionText.text = data.description;
    }

    BuildingData FindBestTargetFromExistingAnchors()
    {
        float pitchAngle = 90.0f - Vector3.Angle(_cameraTransform.forward, Vector3.up);
        if (Mathf.Abs(pitchAngle) > verticalAngleLimit / 2) return null;

        BuildingData bestTarget = null;
        float minAngle = float.MaxValue;

        foreach (var building in buildingList)
        {
            if (_buildingAnchors.ContainsKey(building.buildingName))
            {
                ARGeospatialAnchor anchor = _buildingAnchors[building.buildingName];
                Vector3 buildingWorldPosition = anchor.transform.position;

                float distance = Vector3.Distance(_cameraTransform.position, buildingWorldPosition);
                if (distance > detectionRadius) continue; 

                Vector3 cameraForward = _cameraTransform.forward;
                cameraForward.y = 0;
                Vector3 directionToBuilding = buildingWorldPosition - _cameraTransform.position;
                directionToBuilding.y = 0;
                float angle = Vector3.Angle(cameraForward.normalized, directionToBuilding.normalized);

                if (angle < detectionAngle / 2 && angle < minAngle)
                {
                    minAngle = angle;
                    bestTarget = building;
                }
            }
        }
        return bestTarget;
    }
}