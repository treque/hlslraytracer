using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Renderer : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;

    RenderTexture target;
    Camera camera;


    private void Awake()
    {
        camera = GetComponent<Camera>();
    }

    void SetShaderParmameters()
    {
        // equiv of uniform variables?
        RayTracingShader.SetMatrix("CameraToWorld", camera.cameraToWorldMatrix);
        // clip space -> view space
        RayTracingShader.SetMatrix("CameraInverseProjection", camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(0, "SkyboxTexture", SkyboxTexture);
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

        Graphics.Blit(target, destination);
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
