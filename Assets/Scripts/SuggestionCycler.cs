using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

public class SuggestionCycler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text suggestionText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private GameObject cardsRoot;

    [Header("Input")]
    [Tooltip("B en mando derecho / Y en mando izquierdo. Usa el sistema Unity XR.")]
    [SerializeField] private bool useSecondaryButton = true;

    [Tooltip("Opcional: gatillo lateral/de agarre. Déjalo desactivado primero.")]
    [SerializeField] private bool useGripButton = false;

    [Tooltip("Solo para probar en el editor.")]
    [SerializeField] private KeyCode editorTestKey = KeyCode.N;

    [SerializeField] private bool onlyChangeWhenCardsAreVisible = true;

    [Tooltip("Evita que una pulsación larga cambie varias sugerencias seguidas.")]
    [SerializeField, Min(0.1f)]
    private float inputCooldown = 0.35f;

    [Header("Text")]
    [SerializeField] private string title = "Sugerencia:";
    [SerializeField] private string controlHint = "Siguiente: B/Y";
    [SerializeField] private bool showSuggestionCounter = true;

    [TextArea(2, 4)]
    [SerializeField]
    private List<string> suggestions = new List<string>
    {
        "Pregúntale qué fue lo que más le gustó de la excavación.",
        "Pregúntale si volvería a participar en una experiencia parecida.",
        "¿El trabajo en una excavación es muy duro físicamente?",
        "Pregúntale qué herramientas suelen usar durante la excavación.",
        "Pregúntale qué tipo de ruinas encontraron y de qué época eran."
    };

    [SerializeField] private int currentSuggestionIndex = 0;
    [SerializeField] private bool logSuggestionChanges = true;

    private bool wasSuggestionButtonPressed = false;
    private float lastInputTime = -999f;

    private void Start()
    {
        currentSuggestionIndex = Mathf.Clamp(
            currentSuggestionIndex,
            0,
            Mathf.Max(0, suggestions.Count - 1)
        );

        ApplyCurrentSuggestion();
        ApplyHint();
    }

    private void Update()
    {
        if (Time.time - lastInputTime < inputCooldown)
        {
            // Actualizamos el estado para no dejar una pulsación atascada.
            WasSuggestionButtonPressedThisFrame();
            return;
        }

        if (!WasSuggestionButtonPressedThisFrame())
        {
            return;
        }

        if (onlyChangeWhenCardsAreVisible && cardsRoot != null && !cardsRoot.activeInHierarchy)
        {
            if (logSuggestionChanges)
            {
                Debug.Log($"{nameof(SuggestionCycler)}: botón detectado, pero las tarjetas no están visibles.");
            }

            return;
        }

        lastInputTime = Time.time;
        ShowNextSuggestion();
    }

    private bool WasSuggestionButtonPressedThisFrame()
    {
        bool isPressedNow = false;

#if UNITY_EDITOR
        if (Input.GetKeyDown(editorTestKey))
        {
            isPressedNow = true;
        }
#endif

        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
            devices
        );

        foreach (InputDevice device in devices)
        {
            if (useSecondaryButton)
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

            if (useGripButton)
            {
                if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButtonPressed))
                {
                    if (gripButtonPressed)
                    {
                        isPressedNow = true;
                        break;
                    }
                }
            }
        }

        if (isPressedNow && !wasSuggestionButtonPressed)
        {
            wasSuggestionButtonPressed = true;
            return true;
        }

        if (!isPressedNow)
        {
            wasSuggestionButtonPressed = false;
        }

        return false;
    }

    public void ShowNextSuggestion()
    {
        if (suggestions == null || suggestions.Count == 0)
        {
            ApplyCurrentSuggestion();
            ApplyHint();
            return;
        }

        currentSuggestionIndex++;

        if (currentSuggestionIndex >= suggestions.Count)
        {
            currentSuggestionIndex = 0;
        }

        ApplyCurrentSuggestion();
        ApplyHint();

        if (logSuggestionChanges)
        {
            Debug.Log($"{nameof(SuggestionCycler)}: suggestion changed to index {currentSuggestionIndex}");
        }
    }

    public void ShowPreviousSuggestion()
    {
        if (suggestions == null || suggestions.Count == 0)
        {
            ApplyCurrentSuggestion();
            ApplyHint();
            return;
        }

        currentSuggestionIndex--;

        if (currentSuggestionIndex < 0)
        {
            currentSuggestionIndex = suggestions.Count - 1;
        }

        ApplyCurrentSuggestion();
        ApplyHint();

        if (logSuggestionChanges)
        {
            Debug.Log($"{nameof(SuggestionCycler)}: suggestion changed to index {currentSuggestionIndex}");
        }
    }

    private void ApplyCurrentSuggestion()
    {
        if (suggestionText == null)
        {
            Debug.LogWarning($"{nameof(SuggestionCycler)}: suggestionText is not assigned.");
            return;
        }

        if (suggestions == null || suggestions.Count == 0)
        {
            suggestionText.text = $"{title}\nSin sugerencias disponibles.";
            return;
        }

        string suggestion = suggestions[currentSuggestionIndex];
        suggestionText.text = $"{title}\n{suggestion}";
    }

    private void ApplyHint()
    {
        if (hintText == null)
        {
            return;
        }

        if (showSuggestionCounter && suggestions != null && suggestions.Count > 0)
        {
            hintText.text = $"{controlHint} · {currentSuggestionIndex + 1}/{suggestions.Count}";
        }
        else
        {
            hintText.text = controlHint;
        }

        hintText.gameObject.SetActive(!string.IsNullOrWhiteSpace(hintText.text));
    }
}