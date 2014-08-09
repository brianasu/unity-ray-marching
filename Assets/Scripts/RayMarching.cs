using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class RayMarching : MonoBehaviour
{
	[SerializeField]
	private bool
		updateNoise = false;
	[SerializeField]
	private bool
		renderLight = false;
	[SerializeField]
	private float
		noiseStrength = 1f;
	[SerializeField]
	private int
		blurPasses = 0;
	[SerializeField]
	private int
		downscale = 2;
	[SerializeField]
	private LayerMask
		volumeLayer;
	[SerializeField]
	private Light
		sunLight;
	[SerializeField]
	private Shader
		noiseShader;
	[SerializeField]
	private Shader
		compositeShader;
	[SerializeField]
	private Shader
		renderFrontDepthShader;
	[SerializeField]
	private Shader
		renderBackDepthShader;
	[SerializeField]
	private Shader
		rayMarchShader;
	[SerializeField]
	private Vector4
		scale = Vector4.one;
	[SerializeField]
	private Vector4
		octaves = Vector4.one;
	[SerializeField]
	private Vector4
		clipDimensions;
	[SerializeField]
	private int
		volumeTexWidth = 4096;
	[SerializeField]
	public Material
		pixelBlitMaterial;
	private int _slices = 8;
	private Material _rayMarchMaterial;
	private Material _compositeMaterial;
	private Camera _ppCamera;
	private RenderTexture _noiseBuffer;
	
	private void GenerateGPUNoiseBuffer()
	{
		var noiseMaterial = new Material(noiseShader);
		noiseMaterial.SetFloat("_Slices", _slices);
		noiseMaterial.SetFloat("_Strength", noiseStrength);
		noiseMaterial.SetVector("_Scale", scale);
		noiseMaterial.SetVector("_Octaves", octaves);
		noiseMaterial.SetVector("_ClipDims", clipDimensions);

		if(sunLight != null)
		{
			noiseMaterial.SetVector("_LightDir", sunLight.transform.forward);
		}

		if(_noiseBuffer == null)
		{
			_noiseBuffer = RenderTexture.GetTemporary(volumeTexWidth, volumeTexWidth, 0, RenderTextureFormat.ARGB32);
			_noiseBuffer.filterMode = FilterMode.Trilinear;
			_noiseBuffer.generateMips = false;
			_noiseBuffer.hideFlags = HideFlags.HideAndDontSave;
			_noiseBuffer.wrapMode = TextureWrapMode.Repeat;
		}

		Graphics.Blit(_noiseBuffer, _noiseBuffer, noiseMaterial, 0);
		for(int i = 0; i < blurPasses; i++)
		{
			Graphics.Blit(_noiseBuffer, _noiseBuffer, noiseMaterial, 2);
			Graphics.Blit(_noiseBuffer, _noiseBuffer, noiseMaterial, 3);
			Graphics.Blit(_noiseBuffer, _noiseBuffer, noiseMaterial, 4);
		}

		if(renderLight)
		{
			Graphics.Blit(_noiseBuffer, _noiseBuffer, noiseMaterial, 1);
		}
		
		Destroy(noiseMaterial);
	}

	private void BlitParticles(Vector3 part)
	{
		GL.Clear(false, true, Color.clear);
		var texDimensions = volumeTexWidth / _slices;

		GL.PushMatrix();
		pixelBlitMaterial.SetPass(0);
		GL.LoadPixelMatrix(0, volumeTexWidth, 0, volumeTexWidth);
		GL.Begin(GL.QUADS);

		var widthOff = 1f;
		var heightOff = 1f;
 
		var pos = part + Vector3.one * 0.5f;
		var x = pos.x * texDimensions + Mathf.Floor((pos.z * _slices % 1) * _slices) * texDimensions;
		var y = volumeTexWidth - ((pos.y * texDimensions) + (Mathf.Floor(pos.z * _slices)) * texDimensions);

		GL.TexCoord2(0, 0);
		GL.Vertex3(x, y, 0);
		GL.Color(Color.red);

		GL.TexCoord2(0, 1);
		GL.Vertex3(x, y + heightOff, 0);
		GL.Color(Color.red);

		GL.TexCoord2(1, 1);
		GL.Vertex3(x + widthOff, y + heightOff, 0);
		GL.Color(Color.red);

		GL.TexCoord2(1, 0);
		GL.Vertex3(x + widthOff, y, 0);
		GL.Color(Color.red);

		GL.End();
		GL.PopMatrix();
	}

	private void Awake()
	{
		_rayMarchMaterial = new Material(rayMarchShader);
		_compositeMaterial = new Material(compositeShader);
	}

	private void Start()
	{
		GenerateGPUNoiseBuffer();
	}

	private void Update()
	{
		if(updateNoise)
		{
			GenerateGPUNoiseBuffer();
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		var width = source.width / downscale;
		var height = source.height / downscale;

		if(_ppCamera == null)
		{
			var go = new GameObject("PPCamera");
			_ppCamera = go.AddComponent<Camera>();
			_ppCamera.enabled = false;
		}

		_ppCamera.CopyFrom(camera);
		_ppCamera.clearFlags = CameraClearFlags.SolidColor;
		_ppCamera.backgroundColor = Color.white;
		_ppCamera.cullingMask = volumeLayer;

		var frontDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat);
		var backDepth = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat);

		_rayMarchMaterial.SetFloat("_Dimensions", _slices);
		_rayMarchMaterial.SetVector("_LightDir", sunLight.transform.forward);
		_rayMarchMaterial.SetVector("_LightPos", sunLight.transform.position);

		var volumeTarget = RenderTexture.GetTemporary(width, height, 0);

		// Render depths
		_ppCamera.targetTexture = frontDepth;
		_ppCamera.RenderWithShader(renderFrontDepthShader, "RenderType");
		_ppCamera.targetTexture = backDepth;
		_ppCamera.RenderWithShader(renderBackDepthShader, "RenderType");

		// Render volume
		_rayMarchMaterial.SetTexture("_FrontTex", frontDepth);
		_rayMarchMaterial.SetTexture("_BackTex", backDepth);
		_rayMarchMaterial.SetTexture("_MainTex", _noiseBuffer);

		Graphics.Blit(null, volumeTarget, _rayMarchMaterial);

		//Composite
		_compositeMaterial.SetTexture("_BlendTex", volumeTarget);
		Graphics.Blit(source, destination, _compositeMaterial);

		RenderTexture.ReleaseTemporary(volumeTarget);
		RenderTexture.ReleaseTemporary(frontDepth);
		RenderTexture.ReleaseTemporary(backDepth);
	}
}
