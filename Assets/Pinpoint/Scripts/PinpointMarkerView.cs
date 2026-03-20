using UnityEngine;

public class PinpointMarkerView : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float selectedScaleMultiplier = 1.3f;

    private Vector3 _baseScale;
    private bool _initialized;

    private void Awake()
    {
       if (visualRoot == null) visualRoot = transform; // attached to GameObject 
       _baseScale = visualRoot.localScale;
       _initialized = true;
    }

    public void SetSelected(bool selected)
    {
        if (!_initialized)
        {
            Awake();
        }

        visualRoot.localScale = selected ? _baseScale * selectedScaleMultiplier : _baseScale;
    }
}
