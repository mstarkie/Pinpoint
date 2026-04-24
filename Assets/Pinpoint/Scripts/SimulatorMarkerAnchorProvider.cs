using UnityEngine;

public class SimulatorMarkerAnchorProvider : MonoBehaviour, IMarkerAnchorProvider
{
    public MarkerAnchorDto CreateAnchor(Transform markerTransform)
    {
        return MarkerAnchorDto.FromTransform(MarkerAnchorSource.Simulator, markerTransform);
    }

    public Pose ResolvePose(PinpointMarkerDto markerDto)
    {
        if (markerDto.anchor != null && markerDto.anchor.HasUsablePose)
            return markerDto.anchor.ToPose();

        return new Pose(markerDto.position, Quaternion.identity);
    }
}
