using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class HeadsetCameraPermissionAndPreview : MonoBehaviour
{
    private const string HeadsetCameraPermission = "horizonos.permission.HEADSET_CAMERA";

    [Header("Referencias")]
    [SerializeField] private Behaviour passthroughCameraAccessComponent;
    [SerializeField] private Material targetMaterial;

    [Header("Material")]
    [SerializeField] private string texturePropertyName = "_BaseMap";

    [Header("Tiempos")]
    [SerializeField] private float permissionTimeoutSeconds = 15f;
    [SerializeField] private float cameraReadyTimeoutSeconds = 10f;

    private void Awake()
    {
        ResolvePassthroughCameraAccessIfNeeded();

        if (passthroughCameraAccessComponent != null)
        {
            // Lo apagamos al principio para que no intente arrancar antes de tener permiso.
            passthroughCameraAccessComponent.enabled = false;
        }
    }

    private IEnumerator Start()
    {
        yield return RequestCameraPermissionIfNeeded();

        if (!HasCameraPermission())
        {
            Debug.LogWarning("HeadsetCameraPermissionAndPreview: no hay permiso de cámara.");
            yield break;
        }

        if (passthroughCameraAccessComponent == null)
        {
            Debug.LogWarning("HeadsetCameraPermissionAndPreview: no se encontró PassthroughCameraAccess.");
            yield break;
        }

        passthroughCameraAccessComponent.enabled = true;

        // Esperamos un poco a que el componente inicialice la cámara.
        yield return WaitForCameraAndApplyTexture();
    }

    private IEnumerator RequestCameraPermissionIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (HasCameraPermission())
        {
            Debug.Log("HeadsetCameraPermissionAndPreview: permiso de cámara ya concedido.");
            yield break;
        }

        Debug.Log("HeadsetCameraPermissionAndPreview: solicitando permiso de cámara...");
        Permission.RequestUserPermission(HeadsetCameraPermission);

        float elapsed = 0f;

        while (!HasCameraPermission() && elapsed < permissionTimeoutSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
#else
        yield return null;
#endif
    }

    private IEnumerator WaitForCameraAndApplyTexture()
    {
        float elapsed = 0f;

        while (elapsed < cameraReadyTimeoutSeconds)
        {
            Texture cameraTexture = TryGetCameraTexture();

            if (cameraTexture != null && targetMaterial != null)
            {
                targetMaterial.SetTexture(texturePropertyName, cameraTexture);
                Debug.Log("HeadsetCameraPermissionAndPreview: textura de cámara aplicada al material.");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning("HeadsetCameraPermissionAndPreview: no se pudo obtener textura de cámara a tiempo.");
    }

    private Texture TryGetCameraTexture()
    {
        if (passthroughCameraAccessComponent == null)
            return null;

        System.Type type = passthroughCameraAccessComponent.GetType();

        // Intentamos leer IsPlaying si existe.
        System.Reflection.PropertyInfo isPlayingProperty = type.GetProperty("IsPlaying");

        if (isPlayingProperty != null)
        {
            object isPlayingValue = isPlayingProperty.GetValue(passthroughCameraAccessComponent);

            if (isPlayingValue is bool isPlaying && !isPlaying)
            {
                return null;
            }
        }

        // Intentamos llamar a GetTexture().
        System.Reflection.MethodInfo getTextureMethod = type.GetMethod("GetTexture");

        if (getTextureMethod == null)
        {
            Debug.LogWarning("HeadsetCameraPermissionAndPreview: no existe GetTexture() en PassthroughCameraAccess.");
            return null;
        }

        object textureObject = getTextureMethod.Invoke(passthroughCameraAccessComponent, null);
        return textureObject as Texture;
    }

    private bool HasCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(HeadsetCameraPermission);
#else
        return true;
#endif
    }

    private void ResolvePassthroughCameraAccessIfNeeded()
    {
        if (passthroughCameraAccessComponent != null)
            return;

        Behaviour[] behaviours = GetComponents<Behaviour>();

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == this)
                continue;

            if (behaviour.GetType().Name.Contains("PassthroughCameraAccess"))
            {
                passthroughCameraAccessComponent = behaviour;
                return;
            }
        }
    }
}
