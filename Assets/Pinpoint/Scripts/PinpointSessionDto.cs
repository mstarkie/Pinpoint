using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PinpointSessionDto
{
    public string sessionName;
    public List<PinpointMarkerDto> markers = new();
}

[Serializable]
public class PinpointMarkerDto
{
    public string markerId;
    public string title;
    public int severity;
    public int status;
    public string rawNote;
    public Vector3 position;
}
