using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[RequireComponent(typeof(VolPlayer))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(VisualEffect))]

public class VFXToolkitVologramDissolve : MonoBehaviour
{
    private Renderer meshRenderer;

    [SerializeField]
    [Tooltip("Plays effect when the GameObject enters the Start() method.")]
    private bool playOnStart = false;

    [SerializeField]
    [Tooltip("Reverses the dissolve effect which results in a materializing effect.")]
    private bool reverseEffect = false;

    [SerializeField]
    [Tooltip("Defines at which frame the effect will start playing.")]
    private int playAtFrame = 0;

    [Header("Effect Customization")]

    [SerializeField]
    [Tooltip("Determines the speed of which the effect is applied to the object.")]
    [Range(0.01f, 0.1f)]
    private float dissolveRate = 0.05f;

    [SerializeField]
    [Tooltip("Determines the strength of the noise effect. Higher values will result in the object desintegrating into more particles.")]
    [Range(10, 1000)]
    private float noiseScale = 100;

    [SerializeField]
    [ColorUsage(true, true)]
    [Tooltip("Sets the color for the dissolving effect. If 'useDifferentVfxColor' is set to false, this color will also apply to spawning particles VFX.")]
    private Color modelColor;

    [SerializeField]
    [Tooltip("Sets the color for the spawning particles if 'useDifferentVfxColor' is set to true.")]
    private bool useDifferentVfxColor = false;

    [SerializeField]
    [ColorUsage(true, true)]
    [Tooltip("Sets the color for both the spawning particles and the dissolving effect.")]
    private Color vfxColor;

    private float refreshRate = 0.05f;
    private float counter;
    private bool playEffect;
    private Material[] materials;
    private VolPlayer volPlayer;
    private VisualEffect vfx;
    private Mesh mesh;

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        vfx.enabled = false;
        vfx.visualEffectAsset = (VisualEffectAsset)AssetDatabase.LoadAssetAtPath("Assets/VFXToolkit/Assets/Dissolve/VFXToolkitParticlesVFX.vfx", typeof(VisualEffectAsset));
        volPlayer = GetComponent<VolPlayer>();
        meshRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        volPlayer.textureShaderId = "_Albedo";
        volPlayer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/VFXToolkit/Assets/Dissolve/VFXToolkitDissolveMaterial.mat", typeof(Material));
        if (playOnStart)
        {
            playEffect = true;
        }
    }

    private void Update()
    {
        if (!volPlayer.IsPlaying) return;

        if (playAtFrame > VolPluginInterface.VolGetNumFrames() && volPlayer.IsOpen)
        {
            Debug.LogWarning("Chosen frame " + playAtFrame + " is out of bounds. Setting frame index to maximum number allowed for current Vologram. \n The maximum amount of frames for the current Vologram is " + VolPluginInterface.VolGetNumFrames());
            playAtFrame = (int)VolPluginInterface.VolGetNumFrames();
        }

        if (Time.frameCount % ((int)VolPluginInterface.VolGetNumFrames()) == playAtFrame) {
            playEffect = true;
        }

        if (meshRenderer != null && playEffect)
        {
            playEffect = false;
            StartCoroutine(EffectCoroutine());
        }
    }

    private IEnumerator EffectCoroutine()
    {
        if (meshRenderer.materials.Length > 0)
        {
            materials = meshRenderer.materials;
            vfx.SetFloat("duration", vfx.GetFloat("duration") / dissolveRate / 25);
            if (reverseEffect)
            {
                counter = 1;
                dissolveRate = dissolveRate < 0 ? dissolveRate : -dissolveRate;
            }
            else
            {
                counter = 0;
                dissolveRate = dissolveRate > 0 ? dissolveRate : -dissolveRate;
            }

            materials[0].SetFloat("_DissolveAmount", counter);
            mesh = GetComponent<MeshFilter>().mesh;
            vfx.SetMesh("meshRenderer", mesh);
            if (vfx != null)
            {
                vfx.enabled = true;
                vfx.Play();
            }

            while (materials[0].GetFloat("_DissolveAmount") <= 1 && materials[0].GetFloat("_DissolveAmount") >= 0)
            {
                counter += dissolveRate;
                ChangeVfxColor(useDifferentVfxColor ? vfxColor : modelColor);
                for (int i = 0; i < materials.Length; i++)
                {
                    ChangeModelColor(modelColor);                    
                    materials[i].SetFloat("_NoiseScale", noiseScale);
                    materials[i].SetFloat("_DissolveAmount", counter);
                }
                yield return new WaitForSeconds(refreshRate);
            }
            dissolveRate = dissolveRate > 0 ? dissolveRate : -dissolveRate;
        }
    }

    public void ChangePlayState(bool playState)
    {
        playEffect = playState;
    }
    public void UseDifferentColors(bool mode)
    {
        useDifferentVfxColor = mode;
    }

    public void ChangeModelColor(Color modelColor)
    {
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor("_DissolveColor", modelColor);
        }
        if (!useDifferentVfxColor)
        {
            vfx.SetVector4("BaseColor", modelColor);
        }
    }

    public void ChangeVfxColor(Color vfxColor)
    {
        vfx.SetVector4("BaseColor", vfxColor);
    }
}
