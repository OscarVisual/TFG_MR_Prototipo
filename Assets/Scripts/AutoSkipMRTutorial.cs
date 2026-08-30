using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoSkipMRTutorial : MonoBehaviour
{
    [SerializeField] private float delayBeforeSkip = 0.2f;
    [SerializeField] private float delayBeforeHideTutorial = 0.5f;

    private IEnumerator Start()
    {
        // Ocultamos visualmente el tutorial desde el primer frame,
        // pero lo dejamos activo para que sus scripts puedan funcionar.
        MakeTemplateTutorialInvisible();

        // Esperamos un poco para que el botón Skip exista y el template inicialice.
        yield return new WaitForSeconds(delayBeforeSkip);

        bool skipClicked = TryClickSkipButton();

        if (skipClicked)
        {
            Debug.Log("AutoSkipMRTutorial: botón Skip pulsado automáticamente.");
        }
        else
        {
            Debug.LogWarning("AutoSkipMRTutorial: no se encontró el botón Skip.");
        }

        // Dejamos tiempo para que el Skip active passthrough.
        yield return new WaitForSeconds(delayBeforeHideTutorial);

        // Ahora sí desactivamos objetos visuales del tutorial.
        HideTemplateTutorialObjects();
    }

    private void MakeTemplateTutorialInvisible()
    {
        SetCanvasGroupInvisible("CoachingCardRoot");
        SetCanvasGroupInvisible("Tutorial Player");
        SetCanvasGroupInvisible("Tap Tooltip");
    }

    private void SetCanvasGroupInvisible(string objectName)
    {
        Transform target = FindTransformByName(objectName);

        if (target == null)
            return;

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private bool TryClickSkipButton()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);

        foreach (Button button in buttons)
        {
            TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                if (text.text.Trim().ToLower().Contains("skip"))
                {
                    button.onClick.Invoke();
                    return true;
                }
            }
        }

        return false;
    }

    private void HideTemplateTutorialObjects()
    {
        HideObjectByName("CoachingCardRoot");
        HideObjectByName("Tutorial Player");
        HideObjectByName("Tap Tooltip");
    }

    private void HideObjectByName(string objectName)
    {
        Transform target = FindTransformByName(objectName);

        if (target != null)
        {
            target.gameObject.SetActive(false);
            Debug.Log("AutoSkipMRTutorial: ocultado " + objectName);
        }
    }

    private Transform FindTransformByName(string objectName)
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);

        foreach (Transform t in allTransforms)
        {
            if (t.name == objectName)
            {
                return t;
            }
        }

        return null;
    }
}
