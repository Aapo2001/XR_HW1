using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MagnifyingLensCamera : MonoBehaviour
{
    public RenderTexture renderTexture;
    public float lensFOV = 20f;
    private Camera _mainCamera;
    private Camera _lensCamera;
    private int _renderTextureId;

    private void Start()
    {
        if (!renderTexture)
        {
            Debug.LogError("MagnifyingLensCamera requires a RenderTexture assignment.", this);
            enabled = false;
            return;
        }
        _mainCamera = Camera.main;
        _renderTextureId = renderTexture.GetInstanceID();
        _lensCamera = CreateLensCamera();
        SetupLensCamera();
    }

    private Camera CreateLensCamera()
    {
        var parent = transform.parent ? transform.parent : transform;
        var camObj = new GameObject("LensCamera");
        camObj.transform.SetParent(parent, false);
        camObj.transform.localScale = Vector3.one;

        return camObj.AddComponent<Camera>();
    }


    private void SetupLensCamera()
    {
        _lensCamera.targetTexture = renderTexture;                                                        
        _lensCamera.fieldOfView = lensFOV;                                                                
        _lensCamera.nearClipPlane = 0.01f;                                                                
        _lensCamera.farClipPlane = 1000f;                                                                 
        _lensCamera.clearFlags = CameraClearFlags.SolidColor;                                             
        _lensCamera.backgroundColor = Color.black;                                                        
        _lensCamera.depth = 100f;                                                                         
        _lensCamera.enabled = true;                                                                       
                                                                                                  
        var baseMask = _mainCamera ? _mainCamera.cullingMask : _lensCamera.cullingMask;                   
        _lensCamera.cullingMask = baseMask | (1 << 6);                                                    
                                                                                                  
        var urpData = _lensCamera.GetUniversalAdditionalCameraData();                                     
        if (urpData)                                                                              
        {                                                                                         
            urpData.renderType = CameraRenderType.Base;                                           
        }                                                                                         
    }
    private void LateUpdate()
    {
        if (!_mainCamera || !_lensCamera) return;
        
        _lensCamera.transform.position = transform.position;
        _lensCamera.transform.rotation = _mainCamera.transform.rotation;
    }

    private void OnDestroy()
    {
        if (_renderTextureId == 0) return;
        _lensCamera =  null;
        _renderTextureId = 0;
        _mainCamera = null;
    }
}
