using UnityEngine;

public class PinpointMarkerView : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material selectedMaterial;

    private void Reset()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
    }


    public void SetSelected(bool isSelected)
    {
        if (targetRenderer == null)
            return;

        targetRenderer.sharedMaterial = isSelected ? selectedMaterial : normalMaterial;
    }
}
