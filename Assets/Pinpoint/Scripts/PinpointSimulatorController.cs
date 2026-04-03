using System.Collections.Generic;
using UnityEngine;

public class PinpointSimulatorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private MarkerDetailsPanel detailsPanel;


    [Header("Raycast")]
    [SerializeField] private LayerMask placementMask = ~0;
    [SerializeField] private float fallbackDistance = 2f;


    private readonly List<GameObject> _markers = new();
    private GameObject _selected;

    void Reset()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // left click
        {
            HandleLeftClick();
        }
    }

    private void HandleLeftClick()
    {
        if (mainCamera == null)
        {
            Debug.LogError("PinpointSimulatorController: mainCamera is not assigned.");
            return;
        }
        if (markerPrefab == null)
        {
            Debug.LogError("PinpointSimulatorController: markerPrefab is not assigned.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask, QueryTriggerInteraction.Ignore))
        {
            // If we clicked a marker, select it; otherwise place a new marker at the hit point.
            if (hit.collider.CompareTag("PinpointMarker"))
            {
                SelectMarker(hit.collider.gameObject);
            }
            else
            {
                PlaceMarker(hit.point);
            }
        }
        else
        {
            // Fallback: place in front of camera
            //Vector3 p = mainCamera.transform.position + mainCamera.transform.forward * fallbackDistance;
            //PlaceMarker(p);
            DeselectCurrent();
        }
    }

    private void PlaceMarker(Vector3 position)
    {
        var marker = Instantiate(markerPrefab, position, Quaternion.identity);
        marker.tag = "PinpointMarker";

        var data = marker.GetComponent<PinpointMarkerModel>();
        if (data == null) data = marker.AddComponent<PinpointMarkerModel>();

        data.InitializeNew();

        if (marker.GetComponent<PinpointMarkerView>() == null)
            marker.AddComponent<PinpointMarkerView>();

        _markers.Add(marker);
        SelectMarker(marker);
    }

    private void SelectMarker(GameObject marker)
    {
        // Unhighlight old
        if (_selected != null && _selected.TryGetComponent(out PinpointMarkerView oldView))
            oldView.SetSelected(false);

        _selected = marker;

        // Highlight new
        if (_selected != null && _selected.TryGetComponent(out PinpointMarkerView newView))
            newView.SetSelected(true);

        var data = _selected != null ? _selected.GetComponent<PinpointMarkerModel>() : null;
        detailsPanel.Bind(data);
    }

    private void DeselectCurrent()
    {
        if (_selected != null && _selected.TryGetComponent(out PinpointMarkerView oldView))
            oldView.SetSelected(false);

        _selected = null;

        if (detailsPanel != null)
            detailsPanel.Bind(null);
    }
}
