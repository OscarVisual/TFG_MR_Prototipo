using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Meta.XR;
using TMPro;
using Unity.Collections;
using Unity.InferenceEngine;
using UnityEngine;

public class AnonymousPersonSentisDetector : MonoBehaviour
{
    [Header("Meta Passthrough Camera")]
    [SerializeField] private PassthroughCameraAccess cameraAccess;

    [Header("YOLO / Sentis")]
    [SerializeField] private ModelAsset sentisModel;
    [SerializeField] private TextAsset labelsAsset;
    [SerializeField] private BackendType backend = BackendType.CPU;

    [SerializeField, Range(0.01f, 1f)]
    private float scoreThreshold = 0.15f;

    [SerializeField, Range(0.01f, 1f)]
    private float iouThreshold = 0.6f;

    [SerializeField, Min(0.05f)]
    private float inferenceInterval = 0.25f;

    [Header("Connection with our card anchor controller")]
    [SerializeField] private MonoBehaviour anchorController;

    [Tooltip("0 = top of person box, 0.5 = center, 1 = bottom. Around 0.15-0.25 should point near head/upper torso.")]
    [SerializeField, Range(0f, 1f)]
    private float verticalPointInPersonBox = 0.18f;

    [SerializeField] private bool showCardsAfterPlacement = true;
    [SerializeField] private bool logDetections = true;

    [Header("Debug")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private bool showDebugText = true;

    private Worker worker;
    private Vector2Int inputSize;
    private int personClassId = 0;
    private bool isRunning;

    private MethodInfo place2DMethod;
    private bool place2DMethodUsesBool;

    private string[] labels = Array.Empty<string>();
    private int inferenceCount = 0;

    private readonly List<Detection> candidatePersonDetections = new();
    private readonly List<Detection> keptPersonDetections = new();

    private struct Detection
    {
        public int classId;
        public Vector4 box;
        public float score;

        public Detection(int classId, Vector4 box, float score)
        {
            this.classId = classId;
            this.box = box;
            this.score = score;
        }
    }

    private void Awake()
    {
        LoadLabels();
        LoadModel();
        CacheAnchorMethod();
    }

    private IEnumerator Start()
    {
        SetStatus("Detector iniciado. Esperando cámara...");

        while (enabled)
        {
            yield return new WaitForSeconds(inferenceInterval);

            if (!isRunning)
            {
                yield return RunInferenceOnce();
            }
        }
    }

    private void OnDestroy()
    {
        try
        {
            worker?.PeekOutput(0)?.CompleteAllPendingOperations();
            worker?.PeekOutput(1)?.CompleteAllPendingOperations();
            worker?.PeekOutput(2)?.CompleteAllPendingOperations();
        }
        catch
        {
            // Ignore shutdown timing issues.
        }

        worker?.Dispose();
        worker = null;
    }

    private void LoadLabels()
    {
        if (labelsAsset == null)
        {
            SetStatus("ERROR: falta Labels Asset. Asumo person = 0.");
            personClassId = 0;
            return;
        }

        labels = labelsAsset.text.Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        for (int i = 0; i < labels.Length; i++)
        {
            labels[i] = labels[i].Trim();

            if (labels[i].Equals("person", StringComparison.OrdinalIgnoreCase))
            {
                personClassId = i;
            }
        }

        Debug.Log($"{nameof(AnonymousPersonSentisDetector)}: person class id = {personClassId}");
    }

    private void LoadModel()
    {
        if (sentisModel == null)
        {
            Debug.LogError($"{nameof(AnonymousPersonSentisDetector)}: sentisModel is missing.");
            SetStatus("ERROR: falta Sentis Model.");
            enabled = false;
            return;
        }

        var model = ModelLoader.Load(sentisModel);
        var inputShape = model.inputs[0].shape;

        inputSize = new Vector2Int(inputShape.Get(2), inputShape.Get(3));
        worker = new Worker(model, backend);

        Debug.Log($"{nameof(AnonymousPersonSentisDetector)}: model loaded. Input size = {inputSize.x}x{inputSize.y}");
        SetStatus($"Modelo cargado. Input: {inputSize.x}x{inputSize.y}. Person id: {personClassId}");
    }

    private IEnumerator RunInferenceOnce()
    {
        if (worker == null)
        {
            SetStatus("ERROR: Worker/modelo no cargado.");
            yield break;
        }

        if (cameraAccess == null)
        {
            SetStatus("ERROR: Camera Access no asignado.");
            yield break;
        }

        if (!cameraAccess.IsPlaying)
        {
            SetStatus("Cámara todavía no está activa: IsPlaying = false.");
            yield break;
        }

        Texture cameraTexture = cameraAccess.GetTexture();

        if (cameraTexture == null)
        {
            SetStatus("Cámara activa, pero GetTexture() devuelve null.");
            yield break;
        }

        isRunning = true;
        inferenceCount++;

        SetStatus($"Inferencia #{inferenceCount}\nCámara OK: {cameraTexture.width}x{cameraTexture.height}\nProcesando...");

        var textureTransform = new TextureTransform().SetDimensions(
            cameraTexture.width,
            cameraTexture.height,
            3
        );

        using var input = new Tensor<float>(
            new TensorShape(1, 3, inputSize.x, inputSize.y)
        );

        TextureConverter.ToTensor(cameraTexture, input, textureTransform);

        worker.Schedule(input);

        var boxesTensor = worker.PeekOutput(0) as Tensor<float>;
        var classIdsTensor = worker.PeekOutput(1) as Tensor<int>;
        var scoresTensor = worker.PeekOutput(2) as Tensor<float>;

        if (boxesTensor == null || classIdsTensor == null || scoresTensor == null)
        {
            SetStatus("ERROR: salidas del modelo inesperadas.\nNo coinciden boxes/classes/scores.");
            Debug.LogError($"{nameof(AnonymousPersonSentisDetector)}: unexpected model outputs.");
            isRunning = false;
            yield break;
        }

        var boxesAwaiter = boxesTensor.ReadbackAndCloneAsync().GetAwaiter();
        while (!boxesAwaiter.IsCompleted)
        {
            yield return null;
        }

        using var boxes = boxesAwaiter.GetResult();

        var classIdsAwaiter = classIdsTensor.ReadbackAndCloneAsync().GetAwaiter();
        while (!classIdsAwaiter.IsCompleted)
        {
            yield return null;
        }

        using var classIds = classIdsAwaiter.GetResult();

        var scoresAwaiter = scoresTensor.ReadbackAndCloneAsync().GetAwaiter();
        while (!scoresAwaiter.IsCompleted)
        {
            yield return null;
        }

        using var scores = scoresAwaiter.GetResult();

        AnalyzeDetections(
            boxes,
            classIds,
            scores,
            out Detection bestAny,
            out bool hasBestAny,
            out Detection bestPerson,
            out bool hasBestPerson,
            out int totalAboveThreshold,
            out int personsAboveThreshold
        );

        if (hasBestPerson)
        {
            Vector2 normalizedImagePoint = GetNormalizedPointFromPersonBox(bestPerson.box);

            SendPointToAnchorController(
                normalizedImagePoint.x,
                normalizedImagePoint.y
            );

            SetStatus(
                $"PERSON DETECTED\n" +
                $"Score: {bestPerson.score:F2}\n" +
                $"Point: {normalizedImagePoint.x:F2}, {normalizedImagePoint.y:F2}\n" +
                $"Total > threshold: {totalAboveThreshold}\n" +
                $"Persons > threshold: {personsAboveThreshold}"
            );
        }
        else
        {
            string bestInfo = hasBestAny
                ? $"{GetLabel(bestAny.classId)} ({bestAny.classId}) score {bestAny.score:F2}"
                : "none";

            SetStatus(
                $"NO PERSON\n" +
                $"Camera: OK {cameraTexture.width}x{cameraTexture.height}\n" +
                $"Inferences: {inferenceCount}\n" +
                $"Total > threshold: {totalAboveThreshold}\n" +
                $"Persons > threshold: {personsAboveThreshold}\n" +
                $"Best any: {bestInfo}"
            );
        }

        isRunning = false;
    }

    private void AnalyzeDetections(
        Tensor<float> boxes,
        Tensor<int> classIds,
        Tensor<float> scores,
        out Detection bestAny,
        out bool hasBestAny,
        out Detection bestPerson,
        out bool hasBestPerson,
        out int totalAboveThreshold,
        out int personsAboveThreshold)
    {
        bestAny = default;
        bestPerson = default;
        hasBestAny = false;
        hasBestPerson = false;
        totalAboveThreshold = 0;
        personsAboveThreshold = 0;

        candidatePersonDetections.Clear();
        keptPersonDetections.Clear();

        NativeArray<int>.ReadOnly classArray = classIds.AsReadOnlyNativeArray();
        NativeArray<float>.ReadOnly scoreArray = scores.AsReadOnlyNativeArray();

        int count = Mathf.Min(classArray.Length, scoreArray.Length);

        for (int i = 0; i < count; i++)
        {
            int classId = classArray[i];
            float score = scoreArray[i];

            if (score < scoreThreshold)
            {
                continue;
            }

            totalAboveThreshold++;

            Vector4 box = GetBox(boxes, i);
            Detection detection = new Detection(classId, box, score);

            if (!hasBestAny || score > bestAny.score)
            {
                bestAny = detection;
                hasBestAny = true;
            }

            if (classId == personClassId)
            {
                personsAboveThreshold++;
                candidatePersonDetections.Add(detection);
            }
        }

        if (candidatePersonDetections.Count == 0)
        {
            return;
        }

        candidatePersonDetections.Sort((a, b) => b.score.CompareTo(a.score));

        foreach (Detection candidate in candidatePersonDetections)
        {
            bool overlapsExisting = false;

            foreach (Detection kept in keptPersonDetections)
            {
                if (CalculateIoU(candidate.box, kept.box) > iouThreshold)
                {
                    overlapsExisting = true;
                    break;
                }
            }

            if (!overlapsExisting)
            {
                keptPersonDetections.Add(candidate);
            }
        }

        if (keptPersonDetections.Count > 0)
        {
            bestPerson = keptPersonDetections[0];
            hasBestPerson = true;
        }
    }

    private Vector4 GetBox(Tensor<float> boxes, int index)
    {
        return new Vector4(
            boxes[index, 0],
            boxes[index, 1],
            boxes[index, 2],
            boxes[index, 3]
        );
    }

    private Vector2 GetNormalizedPointFromPersonBox(Vector4 box)
    {
        float xPixels = (box.x + box.z) * 0.5f;
        float yPixels = Mathf.Lerp(box.y, box.w, verticalPointInPersonBox);

        float normalizedX = Mathf.Clamp01(xPixels / inputSize.x);
        float normalizedY = Mathf.Clamp01(yPixels / inputSize.y);

        return new Vector2(normalizedX, normalizedY);
    }

    private void CacheAnchorMethod()
    {
        if (anchorController == null)
        {
            return;
        }

        Type type = anchorController.GetType();

        place2DMethod = type.GetMethod(
            "PlaceFromNormalizedCameraPoint",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(float), typeof(float), typeof(bool) },
            null
        );

        if (place2DMethod != null)
        {
            place2DMethodUsesBool = true;
            return;
        }

        place2DMethod = type.GetMethod(
            "PlaceFromNormalizedCameraPoint",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new[] { typeof(float), typeof(float) },
            null
        );

        place2DMethodUsesBool = false;

        if (place2DMethod == null)
        {
            Debug.LogError(
                $"{nameof(AnonymousPersonSentisDetector)}: could not find " +
                "PlaceFromNormalizedCameraPoint(float, float) on the selected anchor controller."
            );
        }
    }

    private void SendPointToAnchorController(float normalizedX, float normalizedY)
    {
        if (anchorController == null)
        {
            SetStatus("ERROR: Anchor Controller no asignado.");
            return;
        }

        if (place2DMethod == null)
        {
            CacheAnchorMethod();
        }

        if (place2DMethod == null)
        {
            SetStatus("ERROR: no encuentro PlaceFromNormalizedCameraPoint en el controlador.");
            return;
        }

        if (place2DMethodUsesBool)
        {
            place2DMethod.Invoke(
                anchorController,
                new object[] { normalizedX, normalizedY, showCardsAfterPlacement }
            );
        }
        else
        {
            place2DMethod.Invoke(
                anchorController,
                new object[] { normalizedX, normalizedY }
            );
        }
    }

    private float CalculateIoU(Vector4 a, Vector4 b)
    {
        float x1 = Mathf.Max(a.x, b.x);
        float y1 = Mathf.Max(a.y, b.y);
        float x2 = Mathf.Min(a.z, b.z);
        float y2 = Mathf.Min(a.w, b.w);

        float intersectionWidth = Mathf.Max(0f, x2 - x1);
        float intersectionHeight = Mathf.Max(0f, y2 - y1);
        float intersectionArea = intersectionWidth * intersectionHeight;

        float areaA = Mathf.Max(0f, a.z - a.x) * Mathf.Max(0f, a.w - a.y);
        float areaB = Mathf.Max(0f, b.z - b.x) * Mathf.Max(0f, b.w - b.y);

        float unionArea = areaA + areaB - intersectionArea;

        if (unionArea <= 0f)
        {
            return 0f;
        }

        return intersectionArea / unionArea;
    }

    private string GetLabel(int classId)
    {
        if (classId >= 0 && classId < labels.Length)
        {
            return labels[classId];
        }

        return $"class_{classId}";
    }

    private void SetStatus(string message)
    {
        if (showDebugText && debugText != null)
        {
            debugText.text = message;
        }

        if (logDetections)
        {
            Debug.Log($"{nameof(AnonymousPersonSentisDetector)}: {message}");
        }
    }
}
