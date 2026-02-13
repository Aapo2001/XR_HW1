using UnityEngine;


/// <summary>
/// Attach this script to the Magnifying Glass root GameObject.
/// It creates a secondary camera at the lens position that renders a zoomed-in
/// view onto a RenderTexture, which is then displayed on the lens mesh.
///
/// The lens camera follows the lens position but always matches the Main Camera's
/// rotation, so you see "straight through" the lens regardless of how it's tilted.
///
/// SETUP (in Unity Editor):
/// 1. Add this component to the Magnifying Glass GameObject (the root with the mesh).
/// 2. Assign the "Lens" child Transform to the "lensTransform" field.
///    (This is where the camera will be positioned — the center of the lens glass.)
/// 3. Assign the Lens and Lens2 child MeshRenderers to the "lensRenderers" array.
///    The script will create a material with the RenderTexture and apply it to them.
/// 4. Set the Main Camera's culling mask to EXCLUDE layer 6 ("HiddenObject").
///    The lens camera will include ALL layers, so hidden objects are only visible through the lens.
/// </summary>
public class MagnifyingLensCamera : MonoBehaviour
{
    [Header("Lens Setup")]
    [Tooltip("The Transform at the center of the lens glass (child 'Lens' object).")]
    public Transform lensTransform;

    [Tooltip("MeshRenderers of the lens glass surfaces (Lens and Lens2).")]
    public MeshRenderer[] lensRenderers;

    [Header("Zoom Settings")]
    [Tooltip("Field of view for the lens camera. Lower = more zoom.")]
    public float zoomFOV = 30f;

    [Tooltip("Resolution of the RenderTexture (square).")]
    public int renderTextureSize = 512;

    [Header("Camera Settings")]
    [Tooltip("Near clip plane for the lens camera.")]
    public float nearClip = 0.01f;

    [Tooltip("Far clip plane for the lens camera.")]
    public float farClip = 1000f;

    private Camera _lensCamera;
    private Camera _mainCamera;
    private RenderTexture _renderTexture;
    private Material _lensMaterial;



   void Start()
    {
        // Find the main VR camera
        _mainCamera = Camera.main;

        // Create the RenderTexture
        _renderTexture = new RenderTexture(renderTextureSize, renderTextureSize, 24)
        {
            antiAliasing = 2
        };
        _renderTexture.Create();

        // Create the lens camera as a child of this object
        var camObj = new GameObject("LensCamera");
        camObj.transform.SetParent(transform, false);
        _lensCamera = camObj.AddComponent<Camera>();

        // Configure the lens camera
        _lensCamera.fieldOfView = zoomFOV;
        _lensCamera.nearClipPlane = nearClip;
        _lensCamera.farClipPlane = farClip;
        _lensCamera.targetTexture = _renderTexture;
        _lensCamera.clearFlags = CameraClearFlags.Skybox;
        _lensCamera.depth = -2; // Render before main camera

        // Lens camera sees ALL layers including HiddenObject (layer 6)
        _lensCamera.cullingMask = ~0; // Everything

        // Disable audio listener if one gets added
        var listener = camObj.GetComponent<AudioListener>();
        if (listener != null)
            Destroy(listener);

        // Create an Unlit material to display the RenderTexture on the lens
        _lensMaterial = new Material(Shader.Find("Unlit/Texture"))
        {
            mainTexture = _renderTexture
        };

        // Apply the material to each lens renderer
        if (lensRenderers == null) return;
        foreach (var meshRenderer in lensRenderers)
        {
            if (meshRenderer != null)
            {
                meshRenderer.material = _lensMaterial;
            }
        }
    }

    public void LateUpdate()
    {
        if (!_lensCamera || !_mainCamera) return;

        // Position the lens camera at the lens center
        _lensCamera.transform.position = lensTransform ? lensTransform.position :
            // Fallback: use this object's position
            transform.position;

        // The camera rotation matches the main camera's rotation,
        // NOT the lens orientation. This means you always look "straight through"
        // the lens regardless of how the lens is tilted.
        _lensCamera.transform.rotation = _mainCamera.transform.rotation;
    }

    public void OnDestroy()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
        }
        if (_lensMaterial != null)
        {
            Destroy(_lensMaterial);
        }
    }
}
