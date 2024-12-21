using UnityEngine;

public class HeatDiffusion : MonoBehaviour
{
    private static readonly int GradientMapId = Shader.PropertyToID("gradientMap");

    [Header("Simulation Parameters")]
    [SerializeField] private float _diffusionRate = 1.0f;
    [SerializeField] private float _heightInfluence = 1.0f;
    [SerializeField] private float _minWeight = 0.1f;
    [SerializeField] private ComputeShader _heatDiffusionShader;
    
    [Header("Input Textures")]
    [SerializeField] private Texture2D _heightMap;
    
    // Simulation textures
    public RenderTexture _gradientMap;
    public RenderTexture _heatMapA;
    public RenderTexture _heatMapB;
    private bool _pingPong = true;
    
    // Shader property IDs
    private readonly int _heightMapId = Shader.PropertyToID("heightMap");
    private readonly int _heatMapInId = Shader.PropertyToID("heatMapIn");
    private readonly int _heatMapOutId = Shader.PropertyToID("heatMapOut");
    private readonly int _deltaTimeId = Shader.PropertyToID("deltaTime");
    private readonly int _diffusionRateId = Shader.PropertyToID("diffusionRate");
    private readonly int _heightInfluenceId = Shader.PropertyToID("heightInfluence");

    public Material _material;
    
    [SerializeField] Transform _windDirection;
    [SerializeField] float _windSpeed = 1.0f;
    
    public Vector2 windDirection;
    private ComputeBuffer resultBuffer;
    private void Start()
    {
        InitializeTextures();
        BakeGradientmap();
        InitializeHeatMap();
        Initializematerial();
    }

    private void InitializeTextures()
    {
        // Create two render textures for ping-pong buffering
        _gradientMap = CreateRenderTexture();
        _heatMapA = CreateRenderTexture();
        _heatMapB = CreateRenderTexture();
        
        // Create buffer for a single float
        resultBuffer = new ComputeBuffer(1, sizeof(float));
        
        // Set the buffer in the shader
        _heatDiffusionShader.SetBuffer(1, "debugBuffer", resultBuffer);
    }

    private void BakeGradientmap()
    {
        _heatDiffusionShader.SetTexture(0, _heightMapId, _heightMap);
        _heatDiffusionShader.SetTexture(0, GradientMapId, _gradientMap);
        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt(_heightMap.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(_heightMap.height / 8.0f);
        _heatDiffusionShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
    }

    private void Initializematerial()
    {
        _material.SetTexture("_HeightMap", _heightMap);
    }
    
    private RenderTexture CreateRenderTexture()
    {
        RenderTexture rt = new RenderTexture(_heightMap.width, _heightMap.height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            useMipMap = false,
            autoGenerateMips = false,
            wrapMode = TextureWrapMode.Clamp
        };
        rt.Create();
        return rt;
    }
    
    private void InitializeHeatMap()
    {
        // Example: Set initial heat values
        // You might want to modify this based on your needs
        RenderTexture.active = _heatMapA;
        GL.Clear(true, true, Color.black);

        Vector2 center = new Vector2(0.5f, 0.5f);
        // Optional: Add some initial heat sources
        // You can modify this or expose it as parameters
        Texture2D initialHeat = new Texture2D(_heightMap.width, _heightMap.height);
        for (int x = 0; x < _heightMap.width; x++)
        {
            for (int y = 0; y < _heightMap.height; y++)
            {
                initialHeat.SetPixel(x, y, Color.black);
                
                float dist = Vector2.Distance(new Vector2(x/(float)_heightMap.width, y/(float)_heightMap.height), center);
                float fire = dist < .05f ? 1 : 0;
                
                // Example: Add some heat sources
                //if (Random.value > 0.97f)
                //{
                    initialHeat.SetPixel(x, y, new Color(fire, 0, 0, 1));
                //}
                
            }
        }
        initialHeat.Apply();
        Graphics.Blit(initialHeat, _heatMapA);
        Destroy(initialHeat);
    }
    
    public bool update = true;
    private void FixedUpdate()
    {
        if(!update) return;
        //if(update) update = false;
        
        SimulateHeatDiffusion();
        _material.SetTexture("_HeatMap", GetCurrentHeatMap());
    }
    
    private void SimulateHeatDiffusion()
    {
        // Set shader parameters
        _heatDiffusionShader.SetTexture(1, _heightMapId, _heightMap);
        _heatDiffusionShader.SetTexture(1, _heatMapInId, GetCurrentHeatMap());
        _heatDiffusionShader.SetTexture(1, _heatMapOutId, GetNextHeatMap());
        
        _heatDiffusionShader.SetFloat(_deltaTimeId, Time.fixedDeltaTime);
        _heatDiffusionShader.SetFloat("minWeight", _minWeight);
        _heatDiffusionShader.SetFloat(_diffusionRateId, _diffusionRate);
        _heatDiffusionShader.SetFloat(_heightInfluenceId, _heightInfluence);
        
        windDirection = new Vector2(_windDirection.forward.x, _windDirection.forward.z);
        windDirection.Normalize();
        
        _heatDiffusionShader.SetVector("windDirection", windDirection);
        _heatDiffusionShader.SetFloat("windSpeed", _windSpeed);
        
        // Dispatch the compute shader
        int threadGroupsX = Mathf.CeilToInt(_heightMap.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(_heightMap.height / 8.0f);
        _heatDiffusionShader.Dispatch(1, threadGroupsX, threadGroupsY, 1);
        
        // Read back the data
        float[] result = new float[1];
        resultBuffer.GetData(result);
        
        Debug.Log("Result: " + result[0]);
        
        // Swap buffers
        _pingPong = !_pingPong;
    }
    
    private RenderTexture GetCurrentHeatMap()
    {
        return _pingPong ? _heatMapA : _heatMapB;
    }
    
    private RenderTexture GetNextHeatMap()
    {
        return _pingPong ? _heatMapB : _heatMapA;
    }
    
    private void OnDestroy()
    {
        // Cleanup
        if (_heatMapA != null) _heatMapA.Release();
        if (_heatMapB != null) _heatMapB.Release();
        
        resultBuffer.Release();
    }
    
    // Optional: Methods to interact with the simulation
    public void AddHeatSource(Vector2 position, float radius, float intensity)
    {
        // Create a temporary compute shader to add heat
        // This is just an example - you might want to modify this based on your needs
        ComputeShader addHeatShader = Resources.Load<ComputeShader>("AddHeatSource");
        if (addHeatShader != null)
        {
            addHeatShader.SetTexture(0, "HeatMap", GetCurrentHeatMap());
            addHeatShader.SetVector("Position", position);
            addHeatShader.SetFloat("Radius", radius);
            addHeatShader.SetFloat("Intensity", intensity);
            
            int threadGroupsX = Mathf.CeilToInt(_heightMap.width / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(_heightMap.height / 8.0f);
            addHeatShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
        }
    }
    
    public float GetHeatAt(Vector2 position)
    {
        // Sample the heat map at the given position
        // This is just an example - you might want to modify this based on your needs
        RenderTexture.active = GetCurrentHeatMap();
        Texture2D temp = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        temp.ReadPixels(new Rect(position.x * _heightMap.width, position.y * _heightMap.height, 1, 1), 0, 0);
        temp.Apply();
        float heat = temp.GetPixel(0, 0).r;
        Destroy(temp);
        return heat;
    }
}