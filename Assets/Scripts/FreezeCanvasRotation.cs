using UnityEngine;

public class FreezeCanvasRotation : MonoBehaviour
{
    private Quaternion initialRotation;

    void Start()
    {
        // Store the initial rotation of the canvas
        initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // Reset the rotation to the initial rotation in LateUpdate to avoid flickering
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}
