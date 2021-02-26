using UnityEngine;

public class ScaleToFillScreen : MonoBehaviour
{
    public Transform planeObject;
    public Camera planeCam;
    void Update()
    {
        var scale = planeObject.localScale;
        scale.x = scale.y * planeCam.aspect;
        planeObject.localScale = scale;
    }
}


