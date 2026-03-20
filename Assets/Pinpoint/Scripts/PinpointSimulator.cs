using System.Collections.Generic;
using UnityEngine;

public class PinpointSimulator : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public GameObject markerPrefab;

    [Header("Placement")]
    public LayerMask placementMask = ~0;   // everything
    public float fallbackDistance = 2.0f;

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
            HandleClick();
        }
    }

    private void HandleClick()
    {
        if (mainCamera == null)
        {
            Debug.LogError("PinpointSimulator: mainCamera is not assigned.");
            return;
        }
        if (markerPrefab == null)
        {
            Debug.LogError("PinpointSimulator: markerPrefab is not assigned.");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, placementMask, QueryTriggerInteraction.Ignore))
        {
            // If we clicked a marker, select it; otherwise place a new marker at the hit point.
            if (hit.collider != null && hit.collider.gameObject.CompareTag("PinpointMarker"))
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
            Vector3 p = mainCamera.transform.position + mainCamera.transform.forward * fallbackDistance;
            PlaceMarker(p);
        }
    }

    private void PlaceMarker(Vector3 position)
    {
        GameObject m = Instantiate(markerPrefab, position, Quaternion.identity);
        m.tag = "PinpointMarker";
        _markers.Add(m);
        SelectMarker(m);
    }

    private void SelectMarker(GameObject marker)
    {
        // Unhighlight old
        if (_selected != null)
        {
            _selected.transform.localScale *= (1f / 1.3f);
        }

        _selected = marker;

        // Highlight new
        if (_selected != null)
        {
            _selected.transform.localScale *= 1.3f;
        }
    }
}
