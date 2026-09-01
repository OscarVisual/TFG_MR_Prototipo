using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class AnonymousFaceAnchorController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera userCamera;
    [SerializeField] private InterlocutorAnchorPlacer interlocutorAnchorPlacer;

    [Header("Conversión cámara 2D a mundo 3D")]
    [Range(0f, 1f)]
    [SerializeField] private float normalizedFaceX = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float normalizedFaceY = 0.5f;

    [SerializeField] private float placementDistance = 2.0f;
    [SerializeField] private float verticalOffset = 0.0f;

    [Header("Formato de coordenadas")]
    [SerializeField] private bool inputUsesImageTopLeftOrigin = true;

    [Header("Modo de prueba")]
    [SerializeField] private bool testWithSecondaryButton = true;
    [SerializeField] private bool placeSimulatedFaceOnStart = false;
    [SerializeField] private float startDelay = 1.0f;

    private bool wasSecondaryButtonPressed;

    private void Start()
    {
        ResolveReferencesIfNeeded();

        if (placeSimulatedFaceOnStart)
        {
            Invoke(nameof(PlaceFromCurrentSimulatedPoint), startDelay);
        }
    }

    private void Update()
    {
        if (testWithSecondaryButton && WasSecondaryButtonPressedThisFrame())
        {
            PlaceFromCurrentSimulatedPoint();
        }
    }

    public void PlaceFromCurrentSimulatedPoint()
    {
        Vector2 normalizedPoint = new Vector2(normalizedFaceX, normalizedFaceY);
        PlaceFromNormalizedCameraPoint(normalizedPoint);
    }

    public void PlaceFromNormalizedCameraPoint(Vector2 normalizedPoint)
    {
        ResolveReferencesIfNeeded();

        if (userCamera == null)
        {
            Debug.LogWarning("AnonymousFaceAnchorController: no se ha encontrado la cámara.");
            return;
        }

        if (interlocutorAnchorPlacer == null)
        {
            Debug.LogWarning("AnonymousFaceAnchorController: falta InterlocutorAnchorPlacer.");
            return;
        }

        float x = Mathf.Clamp01(normalizedPoint.x);
        float y = Mathf.Clamp01(normalizedPoint.y);

        // Muchos detectores de imagen usan origen arriba-izquierda.
        // Unity ViewportPointToRay usa origen abajo-izquierda.
        if (inputUsesImageTopLeftOrigin)
        {
            y = 1f - y;
        }

        Ray ray = userCamera.ViewportPointToRay(new Vector3(x, y, 0f));

        Vector3 worldPosition = ray.origin + ray.direction.normalized * placementDistance;
        worldPosition.y += verticalOffset;

        interlocutorAnchorPlacer.PlaceInterlocutorAtWorldPosition(worldPosition, true);

        Debug.Log(
            "AnonymousFaceAnchorController: ancla colocada desde punto normalizado de cámara: "
            + normalizedPoint
        );
    }

    private bool WasSecondaryButtonPressedThisFrame()
    {
        bool isPressedNow = false;

        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
            devices
        );

        foreach (InputDevice device in devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonPressed))
            {
                if (secondaryButtonPressed)
                {
                    isPressedNow = true;
                    break;
                }
            }
        }

        if (isPressedNow && !wasSecondaryButtonPressed)
        {
            wasSecondaryButtonPressed = true;
            return true;
        }

        if (!isPressedNow)
        {
            wasSecondaryButtonPressed = false;
        }

        return false;
    }

    private void ResolveReferencesIfNeeded()
    {
        if (userCamera == null && Camera.main != null)
        {
            userCamera = Camera.main;
        }

        if (interlocutorAnchorPlacer == null)
        {
            interlocutorAnchorPlacer = FindObjectOfType<InterlocutorAnchorPlacer>();
        }
    }
}