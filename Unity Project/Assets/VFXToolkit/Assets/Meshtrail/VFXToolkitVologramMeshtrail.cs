using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VolPlayer))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class VFXToolkitVologramMeshtrail : MonoBehaviour
{

    [SerializeField]
    [Tooltip("Plays effect when the GameObject enters the Start() method.")]
    private bool playOnStart;

    [SerializeField]
    [Tooltip("Defines the time this object will emit trails. Value will be reset upon setting isTrailActive = true")]
    private float activeTime = 2f;

    [SerializeField]
    [Tooltip("Defines at which frame the effect will start playing.")]
    private int playAtFrame = 0;

    [SerializeField]
    [Tooltip("Defines if the effect should be played if a certain moving threshold is reached (Using transform.position).")]
    private bool playIfMoving;

    [SerializeField]
    [Tooltip("The threshold to activate the effect if object is moving and 'playifMoving' equals true.")]
    [Range(0.01f, 1)]
    private float thresholdChange = 0.05f;

    [Header("Effect Customization")]
    [SerializeField]
    [Tooltip("Defines the time interval between each of the trails created. Lower values produce more frequent trails.")]
    [Range(0.01f, 1)]
    private float refreshRate = 0.1f;

    [SerializeField]
    [ColorUsage(true, true)]
    [Tooltip("Sets the color for both the spawning particles and the dissolving effect.")]
    private Color trailColor;

    [SerializeField]
    [Range(1, 10)]
    [Tooltip("Defines the Fresnel power of the spawned trail.")]
    private float fresnelPower = 2;

    private bool playEffect;
    private float trailTransparency = 1;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private float trailDestroyDelay = 0.5f;
    private float shaderFadeRate = 0.1f;  
    private float shaderVarRefreshRate = 0.05f;
    private VolPlayer volPlayer;
    private Vector3 lastTransformPos;

    private Transform trailTransform;
    private void Awake()
    {
        trailTransform = GetComponent<Transform>();
        volPlayer = GetComponent<VolPlayer>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        if (playOnStart)
        {
            playEffect = true;
        }
        lastTransformPos = transform.position;
    }

    private void Update()
    {
        if (!volPlayer.IsPlaying) return;

        if (playAtFrame > VolPluginInterface.VolGetNumFrames() && volPlayer.IsOpen)
        {
            Debug.LogWarning("Chosen frame " + playAtFrame + " is out of bounds setting frame index to maximum number allowed for current Vologram. \n The maximum amount of frames for the current Vologram is " + VolPluginInterface.VolGetNumFrames());
            playAtFrame = (int)VolPluginInterface.VolGetNumFrames();
        }

        if (Time.frameCount % ((int)VolPluginInterface.VolGetNumFrames()) == playAtFrame)
        {
            playEffect = true;
        }

        var diffVector = transform.position - lastTransformPos;
        if (diffVector.magnitude >= thresholdChange && playIfMoving)
        {
            lastTransformPos = transform.position;
            playEffect = true;
        }

        if (volPlayer.IsPlaying && playEffect && meshRenderer != null)
        {
            playEffect = false;
            StartCoroutine(TrailCoroutine(activeTime));
        }

        
    }

    IEnumerator TrailCoroutine(float time)
    {
        while (time > 0)
        {
            time -= refreshRate;
            GameObject gameObj = new GameObject();
            gameObj.name = "(Meshtrail) " + this.gameObject.name;
            //gameObj.transform.SetParent(this.transform);
            gameObj.transform.SetPositionAndRotation(trailTransform.position, trailTransform.rotation);
            MeshRenderer gameObjRenderer = gameObj.AddComponent<MeshRenderer>();
            MeshFilter gameObjFilter = gameObj.AddComponent<MeshFilter>();
            gameObjFilter.mesh = meshFilter.mesh;
            gameObjRenderer.material = (Material)AssetDatabase.LoadAssetAtPath("Assets/VFXToolkit/Assets/Meshtrail/VFXToolkitGlowMaterial.mat", typeof(Material));
            StartCoroutine(AnimateMaterial(gameObjRenderer.material, 0, shaderFadeRate, shaderVarRefreshRate));

            Destroy(gameObj, trailDestroyDelay);
            yield return new WaitForSeconds(refreshRate);
        }
    }

    private Material SetMaterialProperties(Material targetMaterial)
    {
        targetMaterial.SetColor("_mainColor", trailColor);
        targetMaterial.SetFloat("_fresnelPower", fresnelPower);
        targetMaterial.SetFloat("_alpha", trailTransparency);
        return targetMaterial;
    }

    private IEnumerator AnimateMaterial(Material mat, float goal, float rate, float refreshRate)
    {

        mat = SetMaterialProperties(mat);
        float valueToAnimate = mat.GetFloat("_alpha");

        while (valueToAnimate >= goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat("_alpha", valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }

    public void ChangePlayState(bool playState)
    {
        playEffect = playState;
    }
}
