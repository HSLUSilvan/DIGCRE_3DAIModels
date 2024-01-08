using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(VisualEffect))]

public class VFXToolkitRegularDissolve : MonoBehaviour
{
    [SerializeField]
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
    [Tooltip("Play VFX.")]
    private bool playVFX = true;

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
    private Material[] materials = new Material[1];
    private VisualEffect vfx;
    private Mesh mesh;
    private Texture texture;

    private void Awake()
    {
        vfx = GetComponent<VisualEffect>();
        vfx.enabled = false;
        vfx.visualEffectAsset = (VisualEffectAsset)AssetDatabase.LoadAssetAtPath("Assets/VFXToolkit/Assets/Dissolve/VFXToolkitParticlesVFX.vfx", typeof(VisualEffectAsset));
        meshRenderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        materials[0] = (Material)AssetDatabase.LoadAssetAtPath("Assets/VFXToolkit/Assets/Dissolve/VFXToolkitDissolveMaterial.mat", typeof(Material));
        texture = meshRenderer.material.GetTexture("_BaseMap");
        if (playOnStart)
        {
            playEffect = true;
        }
    }

    private void Update()
    {
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
            
            meshRenderer.materials = materials;
            meshRenderer.materials[0].SetTexture("_Albedo", texture);
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

            meshRenderer.materials[0].SetFloat("_DissolveAmount", counter);
            mesh = GetComponent<MeshFilter>().mesh;
            vfx.SetMesh("meshRenderer", mesh);
            if (vfx != null && playVFX)
            {
                vfx.enabled = true;
                vfx.Play();
            }

            while (meshRenderer.materials[0].GetFloat("_DissolveAmount") <= 1 && meshRenderer.materials[0].GetFloat("_DissolveAmount") >= 0)
            {
                counter += dissolveRate;
                ChangeVfxColor(useDifferentVfxColor ? vfxColor : modelColor);
                ChangeModelColor(modelColor);
                meshRenderer.materials[0].SetFloat("_NoiseScale", noiseScale);
                meshRenderer.materials[0].SetFloat("_DissolveAmount", counter);
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
        
          meshRenderer.materials[0].SetColor("_DissolveColor", modelColor);
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
