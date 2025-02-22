using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RippleEffectMouse : MonoBehaviour
{
    public AnimationCurve waveform = new AnimationCurve(
        new Keyframe(0.00f, 0.50f, 0, 0),
        new Keyframe(0.05f, 1.00f, 0, 0),
        new Keyframe(0.15f, 0.10f, 0, 0),
        new Keyframe(0.25f, 0.80f, 0, 0),
        new Keyframe(0.35f, 0.30f, 0, 0),
        new Keyframe(0.45f, 0.60f, 0, 0),
        new Keyframe(0.55f, 0.40f, 0, 0),
        new Keyframe(0.65f, 0.55f, 0, 0),
        new Keyframe(0.75f, 0.46f, 0, 0),
        new Keyframe(0.85f, 0.52f, 0, 0),
        new Keyframe(0.99f, 0.50f, 0, 0)
    );

    [Range(0.01f, 1.0f)]
    public float refractionStrength = 0.5f;

    public Color reflectionColor = Color.gray;

    [Range(0.01f, 1.0f)]
    public float reflectionStrength = 0.7f;

    [Range(1.0f, 3.0f)]
    public float waveSpeed = 1.25f;

    [Range(1f, 100f)]
    public float wavesPerSecond = 20f; // Controla cuántas ondas se emiten por segundo

    [Range(0.1f, 5.0f)]
    public float waveZoom = 1.0f;

    [Range(0.1f, 5.0f)]
    public float waveWidth = 1.0f;

    [Range(0.0f, 1.0f)]
    public float waveIntensity = 1.0f;

    [SerializeField, HideInInspector]
    Shader shader;

    class Droplet
    {
        Vector2 position;
        float time;

        public Droplet()
        {
            time = 1000;
        }

        public void Reset()
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.nearClipPlane;
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);

            //  position = new Vector2(Random.value, Random.value);
            float screenRatio = Camera.main.aspect - 1f;
            //Debug.Log(mousePos.x + " - " + mousePos.y + " || " + worldPosition.x + " - " + worldPosition.y + " | " + screenRatio + " | " + Camera.main.aspect);
            position = new Vector2((worldPosition.x * screenRatio) + 0.5f, worldPosition.y + 0.5f);
            time = 0;
        }

        public void Reset(Vector3 mousePos)
        {
            mousePos.z = Camera.main.nearClipPlane;
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
            
            // Convertir las coordenadas de pantalla a UV (0-1)
            position = new Vector2(
                mousePos.x / Screen.width,
                mousePos.y / Screen.height
            );

            // No necesitamos ajustar por el aspect ratio aquí
            time = 0;
            
            Debug.Log($"Touch pos: {mousePos}, UV pos: {position}");
        }


        public void Update()
        {
            time += Time.deltaTime;
        }

        public Vector4 MakeShaderParameter(float aspect)
        {
            return new Vector4(position.x , position.y, time, 0);
        }

    }

    Droplet[] droplets;
    Texture2D gradTexture;
    Material material;
    float timer;
    int dropCount;
    private Dictionary<int, int> touchToDropletIndex = new Dictionary<int, int>();
    private int nextAvailableDropletIndex = 0;
    private Dictionary<int, float> touchTimers = new Dictionary<int, float>();
    private static float mouseTimer = 0f;

    void UpdateShaderParameters()
    {
        var c = GetComponent<Camera>();
        
        material.SetVector("_Params1", new Vector4(1, 1, 1 / waveSpeed, waveZoom));
        material.SetVector("_Params2", new Vector4(c.aspect, 1 / c.aspect, refractionStrength, reflectionStrength));
        material.SetVector("_WaveParams", new Vector4(waveWidth, waveIntensity, 0, 0));
        
        for(int i=0; i<10; i++)
        {
            material.SetVector("_Drop" + (i+1), droplets[i].MakeShaderParameter(c.aspect));
        }
        material.SetColor("_Reflection", reflectionColor);
    }

    void Awake()
    {
        Input.multiTouchEnabled = true;
        droplets = new Droplet[10];
        for(int i = 0; i < 10; i++)
        {
            droplets[i] = new Droplet();
        }

        gradTexture = new Texture2D(2048, 1, TextureFormat.Alpha8, false);
        gradTexture.wrapMode = TextureWrapMode.Clamp;
        gradTexture.filterMode = FilterMode.Bilinear;
        for (var i = 0; i < gradTexture.width; i++)
        {
            var x = 1.0f / gradTexture.width * i;
            var a = waveform.Evaluate(x);
            gradTexture.SetPixel(i, 0, new Color(a, a, a, a));
        }
        gradTexture.Apply();

        material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        material.SetTexture("_GradTex", gradTexture);

        UpdateShaderParameters();
    }

    void Update()
    {
        float emissionInterval = 1f / wavesPerSecond; // Calcula el intervalo entre emisiones
        
        // Manejo de toques móviles
        foreach (Touch touch in Input.touches)
        {
            // Inicializar o actualizar el temporizador para este toque
            if (!touchTimers.ContainsKey(touch.fingerId))
            {
                touchTimers[touch.fingerId] = 0f;
            }

            // Actualizar el temporizador
            touchTimers[touch.fingerId] -= Time.deltaTime;

            if (touch.phase == TouchPhase.Began)
            {
                if (!touchToDropletIndex.ContainsKey(touch.fingerId))
                {
                    touchToDropletIndex[touch.fingerId] = nextAvailableDropletIndex;
                    nextAvailableDropletIndex = (nextAvailableDropletIndex + 1) % droplets.Length;
                }
                EmitWave(touch.position, touch.fingerId);
                touchTimers[touch.fingerId] = emissionInterval;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                if (touchTimers[touch.fingerId] <= 0)
                {
                    EmitWave(touch.position, touch.fingerId);
                    touchTimers[touch.fingerId] = emissionInterval;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                touchToDropletIndex.Remove(touch.fingerId);
                touchTimers.Remove(touch.fingerId);
            }
        }
        // Log de cuantos toques se están realizando
        Debug.Log("Touch count: " + Input.touchCount);


#if UNITY_EDITOR
        mouseTimer -= Time.deltaTime;
        if (Input.GetMouseButton(0))
        {
            if (mouseTimer <= 0)
            {
                EmitWave(Input.mousePosition, 0);
                mouseTimer = emissionInterval;
            }
        }
#endif
        
        foreach (var d in droplets) d.Update();
        UpdateShaderParameters();
    }

    private void EmitWave(Vector3 position, int fingerId)
    {
        if (touchToDropletIndex.ContainsKey(fingerId))
        {
            droplets[touchToDropletIndex[fingerId]].Reset(position);
        }
        else
        {
            int dropletIndex = nextAvailableDropletIndex;
            nextAvailableDropletIndex = (nextAvailableDropletIndex + 1) % droplets.Length;
            droplets[dropletIndex].Reset(position);
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination, material);
    }
}
