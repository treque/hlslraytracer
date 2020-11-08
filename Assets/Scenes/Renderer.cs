using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Renderer : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;
    public int RayBounces = 8;
    public Light DirectionalLight;

    public Vector2 SphereRadii = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 100;
    public float SpherePlacementRadius = 100.0f;

    RenderTexture target;
    Camera camera;

    ComputeBuffer _sphereBuffer;

    // AA (jitter)
    uint currentSample = 0;
    Material addMaterial;

    struct Sphere
    {
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    };

    void Awake()
    {
        camera = GetComponent<Camera>();
    }

    void OnEnable()
    {
        currentSample = 0;
        SetUpSpheres();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null) _sphereBuffer.Release();
    }

    void SetUpSpheres()
    {
        List<Sphere> spheres = new List<Sphere>();
        for (int i =0; spheres.Count < SpheresMax; ++i)
        {
            Sphere sphere = new Sphere();
            sphere.radius = SphereRadii.x + Random.value * (SphereRadii.y - SphereRadii.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

            bool skip = false;
            foreach (Sphere otherSphere in spheres)
            {
                float minDist = sphere.radius + otherSphere.radius;
                if (Vector3.SqrMagnitude(sphere.position - otherSphere.position) < minDist * minDist) skip = true;
            }

            if (!skip)
            {
                Color color = Random.ColorHSV();
                bool metallic = Random.value < 0.5f;
                sphere.albedo = metallic ? Vector3.zero : new Vector3(color.r, color.g, color.b);
                sphere.specular = metallic ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
                spheres.Add(sphere);
            }
        }

        _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
        _sphereBuffer.SetData(spheres);

    }

    void Update()
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
        RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
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
