using TMPro;
using UnityEngine;

public class ProfileCardsManager : MonoBehaviour
{
    [Header("Referencias a las tarjetas")]
    [SerializeField] private TextMeshProUGUI identityText;
    [SerializeField] private TextMeshProUGUI affinityText;
    [SerializeField] private TextMeshProUGUI interestsText;
    [SerializeField] private TextMeshProUGUI memoryText;
    [SerializeField] private TextMeshProUGUI suggestionText;

    [Header("Perfil preseleccionado")]
    [SerializeField] private string personName = "Yolanda Nuria";
    [SerializeField] private int age = 24;

    [SerializeField] private string affinity = "alta";
    [SerializeField] private string relationship = "Relación cercana";
    [SerializeField] private string trustLevel = "Confianza elevada";

    [TextArea(2, 4)]
    [SerializeField] private string interests = "Dibujo, música, arqueología";

    [TextArea(2, 4)]
    [SerializeField] private string memory = "Hablasteis sobre su última\nexcavación arqueológica";

    [TextArea(2, 4)]
    [SerializeField] private string suggestion = "Pregúntale qué fue lo que\nmás le gustó de la\nexcavación";

    private void Start()
    {
        ApplyProfileToCards();
    }

    private void OnValidate()
    {
        ApplyProfileToCards();
    }

    private void ApplyProfileToCards()
    {
        if (identityText != null)
        {
            identityText.text =
                personName + "\n" +
                age + " años\n" +
                "Perfil preseleccionado";
        }

        if (affinityText != null)
        {
            affinityText.text =
                "Afinidad: " + affinity + "\n" +
                relationship + "\n" +
                trustLevel;
        }

        if (interestsText != null)
        {
            interestsText.text =
                "Intereses:\n" +
                interests;
        }

        if (memoryText != null)
        {
            memoryText.text =
                "Recuerdo reciente:\n" +
                memory;
        }

        if (suggestionText != null)
        {
            suggestionText.text =
                "Sugerencia:\n" +
                suggestion;
        }
    }
}