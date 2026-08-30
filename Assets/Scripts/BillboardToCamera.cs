using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    private Transform cameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null)
        {
            if (Camera.main == null) return;
            cameraTransform = Camera.main.transform;
        }

        Vector3 directionToCamera = transform.position - cameraTransform.position;

        // Mantenemos la tarjeta vertical, sin inclinarla hacia arriba o abajo.
        directionToCamera.y = 0f;

        if (directionToCamera.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(directionToCamera, Vector3.up);
    }
}
