using System.Collections.Generic;
using UnityEngine;

// Add this to any object you want to make "dirty" and paint-clean in inspection.
// It creates a runtime dirt mask (RenderTexture) and ensures a dirt-capable material is used.
[DisallowMultipleComponent]
public class DirtPaintable : MonoBehaviour
{
    [Header("Target Renderers")]
    public List<Renderer> targetRenderers = new List<Renderer>();

    [Header("Mask Settings")] 
    [Tooltip("Resolution of the dirt mask (per object)")]
    public int maskResolution = 1024;
    [Tooltip("Initial mask value: 1 = fully dirty, 0 = clean")] 
    [Range(0f, 1f)] public float initialMaskValue = 1f;

    [Header("Visuals")] 
    [Tooltip("Optional dirt texture overlay. If empty, flat tint is used.")]
    public Texture2D dirtTexture;
    [ColorUsage(false)] public Color dirtColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [Range(0f, 1f)] public float dirtIntensity = 1f;

    [Header("Material Auto-Setup")] 
    [Tooltip("If enabled, replaces target renderer materials at runtime with DirtMasked shader copies.")]
    public bool autoWrapMaterialWithDirtShader = true;

    [Header("Debug")]
    [Tooltip("When enabled, logs material/shader info and mask binding on enable/inspection.")]
    public bool debugLog = false;

    private RenderTexture maskRT;
    private static readonly int ID_DirtMask = Shader.PropertyToID("_DirtMask");
    private static readonly int ID_DirtTex = Shader.PropertyToID("_DirtTex");
    private static readonly int ID_DirtColor = Shader.PropertyToID("_DirtColor");
    private static readonly int ID_DirtIntensity = Shader.PropertyToID("_DirtIntensity");
    private static readonly int ID_UseDirtTex = Shader.PropertyToID("_UseDirtTex");
    private Material[] runtimeMaterials; // cloned material instances per renderer

    void Awake()
    {
        if (targetRenderers.Count == 0)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r) targetRenderers.Add(r);
        }

        CreateMask();
        SetupMaterials();
        PushProperties();
        if (debugLog) DebugReport("Awake");
    }

    void OnEnable()
    {
        // Re-apply in case materials were instanced/changed elsewhere (e.g., highlight)
        if (maskRT) PushProperties();
        if (debugLog) DebugReport("OnEnable");
    }

    void OnDestroy()
    {
        if (maskRT)
        {
            if (maskRT.IsCreated()) maskRT.Release();
            Destroy(maskRT);
        }
    }

    void CreateMask()
    {
        if (maskRT)
        {
            if (maskRT.IsCreated()) maskRT.Release();
            DestroyImmediate(maskRT);
        }
        maskRT = new RenderTexture(maskResolution, maskResolution, 0, RenderTextureFormat.R8);
        maskRT.name = $"DirtMask_{name}";
        maskRT.useMipMap = false;
        maskRT.wrapMode = TextureWrapMode.Clamp;
        maskRT.filterMode = FilterMode.Bilinear;
        maskRT.Create();

        // Fill with initial value (white = dirty)
        var prev = RenderTexture.active;
        RenderTexture.active = maskRT;
        GL.Clear(false, true, new Color(initialMaskValue, initialMaskValue, initialMaskValue, initialMaskValue));
        RenderTexture.active = prev;
    }

    void SetupMaterials()
    {
        if (!autoWrapMaterialWithDirtShader) return;
        Shader dirtShader = Shader.Find("Custom/DirtMaskedSurface");
        if (!dirtShader)
        {
            Debug.LogError("[DirtPaintable] Shader 'Custom/DirtMaskedSurface' not found. Please add it under Assets/Shaders.");
            return;
        }

        var mats = new List<Material>();
        foreach (var rend in targetRenderers)
        {
            if (!rend) continue;
            var srcArray = rend.sharedMaterials;
            if (srcArray == null || srcArray.Length == 0)
            {
                // fallback single
                var m = new Material(dirtShader);
                m.name = rend.name + "_DirtMasked";
                rend.material = m;
                mats.Add(rend.material);
                continue;
            }

            var dstArray = new Material[srcArray.Length];
            for (int i = 0; i < srcArray.Length; i++)
            {
                var srcMat = srcArray[i];
                var m = new Material(dirtShader);
                m.name = (srcMat ? srcMat.name : rend.name) + "_DirtMasked";
                if (srcMat && srcMat.HasProperty("_MainTex"))
                    m.SetTexture("_MainTex", srcMat.GetTexture("_MainTex"));
                dstArray[i] = m;
            }
            rend.materials = dstArray; // assign preserving submeshes
            mats.AddRange(rend.materials);
        }
        runtimeMaterials = mats.ToArray();
    }

    void PushProperties()
    {
        foreach (var rend in targetRenderers)
        {
            if (!rend) continue;
            var mats = rend.materials; // all submesh materials (instances)
            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (!mat) continue;
                if (mat.HasProperty(ID_DirtMask)) mat.SetTexture(ID_DirtMask, maskRT);
                if (mat.HasProperty(ID_DirtTex))
                {
                    if (dirtTexture)
                    {
                        mat.SetTexture(ID_DirtTex, dirtTexture);
                        if (mat.HasProperty(ID_UseDirtTex)) mat.SetFloat(ID_UseDirtTex, 1f);
                    }
                    else
                    {
                        if (mat.HasProperty(ID_UseDirtTex)) mat.SetFloat(ID_UseDirtTex, 0f);
                    }
                }
                if (mat.HasProperty(ID_DirtColor)) mat.SetColor(ID_DirtColor, dirtColor);
                if (mat.HasProperty(ID_DirtIntensity)) mat.SetFloat(ID_DirtIntensity, dirtIntensity);
            }
        }
    }

    // Allow external systems (e.g., inspection) to re-apply bindings if a new material instance was created
    public void ReapplyAllProperties()
    {
        PushProperties();
        if (debugLog) DebugReport("ReapplyAllProperties");
    }

    private void DebugReport(string ctx)
    {
        foreach (var rend in targetRenderers)
        {
            if (!rend) continue;
            var mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                bool hasMaskProp = m.HasProperty(ID_DirtMask);
                var maskTex = hasMaskProp ? m.GetTexture(ID_DirtMask) : null;
                Debug.Log($"[DirtPaintable:{ctx}] Renderer='{rend.name}' Mat[{i}]='{m.name}' Shader='{m.shader.name}' DirtMaskBound={(maskTex!=null)}", this);
            }
        }
    }

    // Paint API
    static Material s_BrushMat;
    static int ID_BrushUV = Shader.PropertyToID("_BrushUV");
    static int ID_BrushRadius = Shader.PropertyToID("_BrushRadius");
    static int ID_BrushStrength = Shader.PropertyToID("_BrushStrength");

    void EnsureBrushMaterial()
    {
        if (s_BrushMat == null)
        {
            var sh = Shader.Find("Hidden/DirtBrush");
            if (!sh)
            {
                Debug.LogError("[DirtPaintable] Shader 'Hidden/DirtBrush' not found. Add it under Assets/Shaders.");
                return;
            }
            s_BrushMat = new Material(sh);
            s_BrushMat.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    public void PaintAtUV(Vector2 uv, float radiusUV, float strength)
    {
        if (!maskRT) return;
        EnsureBrushMaterial();
        if (!s_BrushMat) return;

        s_BrushMat.SetVector(ID_BrushUV, new Vector4(uv.x, uv.y, 0, 0));
        s_BrushMat.SetFloat(ID_BrushRadius, radiusUV);
        s_BrushMat.SetFloat(ID_BrushStrength, Mathf.Clamp01(strength));

        RenderTexture tmp = RenderTexture.GetTemporary(maskRT.width, maskRT.height, 0, maskRT.format);
        Graphics.Blit(maskRT, tmp);                   // copy current -> tmp
        Graphics.Blit(tmp, maskRT, s_BrushMat, 0);    // apply brush into mask
        RenderTexture.ReleaseTemporary(tmp);
    }

    public void ResetMask(float value = 1f)
    {
        if (!maskRT) return;
        var prev = RenderTexture.active;
        RenderTexture.active = maskRT;
        GL.Clear(false, true, new Color(value, value, value, value));
        RenderTexture.active = prev;
    }
}
