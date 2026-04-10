using UnityEngine;

public class MousePointerRayProvider : MonoBehaviour, IPointerRayProvider
{
    [SerializeField] private Camera targetCamera;
    private void Reset()
    {
        targetCamera = Camera.main;
    }
    public Ray GetPointerRay()
    {
        return targetCamera.ScreenPointToRay(Input.mousePosition);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
