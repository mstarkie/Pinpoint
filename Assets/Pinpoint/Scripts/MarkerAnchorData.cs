using System;
using UnityEngine;

public enum MarkerAnchorSource
{
    Simulator = 0,
    ARPlane = 1,
    SpatialAnchor = 2,
    ModelCoordinate = 3,
    QRCode = 4
}

[Serializable]
public class MarkerAnchorDto
{
    public int source;
    public Vector3 position;
    public Quaternion rotation;
    public string externalAnchorId;

    public MarkerAnchorSource Source => (MarkerAnchorSource)source;
    public bool HasUsablePose => rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w > 0.0001f;

    public static MarkerAnchorDto FromTransform(MarkerAnchorSource source, Transform transform, string externalAnchorId = "")
    {
        return new MarkerAnchorDto
        {
            source = (int)source,
            position = transform.position,
            rotation = transform.rotation,
            externalAnchorId = externalAnchorId
        };
    }

    public Pose ToPose()
    {
        return new Pose(position, rotation);
    }
}

public interface IMarkerAnchorProvider
{
    MarkerAnchorDto CreateAnchor(Transform markerTransform);
    Pose ResolvePose(PinpointMarkerDto markerDto);
}
