using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class InterlocutorAnchorPlacer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform userCamera;
    [SerializeField] private GameObject profileCardsGroup;
    [SerializeField] private GameObject debugSphere;

    [Header("Colocación")]
    [SerializeField] private float distanceFromUser = 2.0f;
    [SerializeField] private float verticalOffset = 0.0f;

    [Header("Comportamiento inicial")]
    [SerializeField] private bool hideCardsOnStart = true;
    [SerializeField] private bool hideDebugSphereOnStart = true;
    [SerializeField] private bool placeAutomaticallyOnStart = false;

    private bool wasPrimaryButtonPressed;

    private void Start()
    {
        ResolveCameraIfNeeded();

        if (hideCardsOnStart && profileCardsGroup != null)
        {
            profileCardsGroup.SetActive(false);
        }

        if (hideDebugSphereOnStart && debugSphere != null)
        {
            debugSphere.SetActive(false);
        }

        if (placeAutomaticallyOnStart)
        {
            PlaceInterlocutorInFrontOfUser();
            ShowProfileCards();
        }
    }

    private void Update()
    {
        if (WasPrimaryButtonPressedThisFrame())
        {
            PlaceInterlocutorInFrontOfUser();
            ShowProfileCards();
        }
    }

    private void PlaceInterlocutorInFrontOfUser()
    {
        ResolveCameraIfNeeded();

        if (userCamera == null)
        {
            Debug.LogWarning("InterlocutorAnchorPlacer: no se ha encontrado la cámara del usuario.");
            return;
        }

        Vector3 forward = userCamera.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = userCamera.forward;
        }

        forward.Normalize();

        Vector3 targetPosition = userCamera.position + forward * distanceFromUser;
        targetPosition.y = userCamera.position.y + verticalOffset;

        transform.position = targetPosition;
        transform.rotation = Quaternion.identity;

        Debug.Log("InterlocutorAnchorPlacer: interlocutor colocado delante del usuario.");
    }

    private void ShowProfileCards()
    {
        if (profileCardsGroup != null)
        {
            profileCardsGroup.SetActive(true);
        }
    }

    private bool WasPrimaryButtonPressedThisFrame()
    {
        bool isPressedNow = false;

        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
            devices
        );

        foreach (InputDevice device in devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonPressed))
            {
                if (primaryButtonPressed)
                {
                    isPressedNow = true;
                    break;
                }
            }
        }

        if (isPressedNow && !wasPrimaryButtonPressed)
        {
            wasPrimaryButtonPressed = true;
            return true;
        }

        if (!isPressedNow)
        {
            wasPrimaryButtonPressed = false;
        }

        return false;
    }

    private void ResolveCameraIfNeeded()
    {
        if (userCamera != null)
            return;

        if (Camera.main != null)
        {
            userCamera = Camera.main.transform;
        }
    }
}