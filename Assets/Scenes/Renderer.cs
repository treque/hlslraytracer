using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Renderer : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public int RayBounces = 8;
    public Light DirectionalLight;

    RenderTexture target;
    Camera camera;

    // AA (jitter)
    uint currentSample = 0;
    Material addMaterial;

    void Awake()
    {
        camera = GetComponent<Camera>();
    }

    private void Update()
    {
        // si la camera bouge pas on va pas resample..
        if (transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        }
        if (DirectionalLight.transform.hasChanged)
        {
            currentSample = 0;
            DirectionalLight.transform.hasChanged = false;
        }
    }

    void SetShaderParmameters()
    {
        // equiv of uniform variables
        RayTracingShader.SetMatrix("_CameraToWorld", camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix("_CameraInverseProjection", camera.projectionMatrix.inverse);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 l = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        RayTracingShader.SetInt("_RayBounces", RayBounces);
        RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);
    }

    // called whenevr camera is finished rendering
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParmameters();
        Render(destination);
    }

    void Render(RenderTexture destination)
    {
        // making sure we have a render target
        InitRenderTexture();

        RayTracingShader.SetTexture(0, "Result", target);
        // spawning a thread per pixel
        // thread group size by default is [numthreads(8,8,1)] which means in our case we will have one thread group per 8x8 pixel
        int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (addMaterial == null) addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, destination, addMaterial);
        currentSample++;
    }

     void InitRenderTexture()
    {
        if (target == null || target.width != Screen.width || target.height != Screen.height)
        {
            if (target != null) target.Release();

            target = new RenderTexture(Screen.width,
                Screen.height,
                0,
                RenderTextureFormat.ARGBFloat,
                RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
    }
}
