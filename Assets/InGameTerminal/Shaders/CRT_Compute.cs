using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InGameTerminal.Shaders
{
	[ExecuteAlways]
	public class CRT_Compute : MonoBehaviour
	{
		[SerializeField]
		private ComputeShader computeShader;
		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float decay = 1.0f;
		private int kernel;
		private int kernel2;
		private RenderTexture buffer;
		private RenderTexture buffer2;
		private RenderTexture input;


/*
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _BaseRadius ("Base Radius (pixels)", Range(0, 10)) = 0
        _FwidthFactor ("Fwidth -> Radius", Range(0, 200)) = 40
        _MaxRadius ("Max Radius (pixels)", Range(0, 32)) = 12
        _Quality ("Quality (0=fast, 1=better)", Range(0, 1)) = 1
    }
*/
		private Shader adaptiveBlur;
		private Material adaptiveBlurMaterial;
		private RenderTexture adaptiveBlurRenderTexture;
		[Range(0.0f, 10.0f)]
		public float adaptiveBlur_baseRadius = 0.0f;
		[Range(0.0f, 200.0f)]
		public float adaptiveBlur_fwidthFactor = 40.0f;
		[Range(0.0f, 32.0f)]
		public float adaptiveBlur_maxRadius = 12.0f;
		[Range(0.0f, 1.0f)]
		public float adaptiveBlur_quality = 1.0f;
		// _DistanceScale
		[Range(0.0f, 2000.0f)]
		public float adaptiveBlur_distanceScale = 100.0f;

		private void OnEnable()
		{
			adaptiveBlur = Shader.Find("InGameTerminal/FWidthAdaptiveBlur");
			if (adaptiveBlur != null)
			{
				adaptiveBlurMaterial = new Material(adaptiveBlur);
			}
			else
			{
				Debug.LogError("Adaptive Blur Shader not found!", this);
			}
			buffer = null;
			input = null;
			if (computeShader == null)
			{
				Debug.LogError("ComputeShader is not assigned!", this);
				enabled = false;
				return;
			}

			kernel = computeShader.FindKernel("CSMain");
			kernel2 = computeShader.FindKernel("CSMain2");
			Debug.Log($"Kernel index: {kernel} {kernel2}", this);
		}
		private void Update()
		{
			if (input && buffer && buffer2)
			{
				computeShader.SetTexture(kernel, "_UIInput", input);
				computeShader.SetFloat("_decay", Time.deltaTime*(1.0f/decay));
				computeShader.SetTexture(kernel, "_Buffer", buffer);
				computeShader.SetTexture(kernel2, "_Buffer", buffer);
				computeShader.SetTexture(kernel2, "_Buffer2", buffer2);
				//this.computeShader.Dispatch(kernel, input.width / 8, input.height / 8, 1);
				this.computeShader.Dispatch(kernel, 1, 24, 1);
				this.computeShader.Dispatch(kernel2, input.width / 8, input.height / 8, 1);
			}
			if (adaptiveBlurMaterial)
			{
				adaptiveBlurMaterial.SetFloat("_BaseRadius", adaptiveBlur_baseRadius);
				adaptiveBlurMaterial.SetFloat("_FwidthFactor", adaptiveBlur_fwidthFactor);
				adaptiveBlurMaterial.SetFloat("_MaxRadius", adaptiveBlur_maxRadius);
				adaptiveBlurMaterial.SetFloat("_Quality", adaptiveBlur_quality);
				adaptiveBlurMaterial.SetFloat("_DistanceScale", adaptiveBlur_distanceScale);
			}
		}
		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
		{
			if (!input)
			{
				input = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				input.enableRandomWrite = true;
				input.Create();
			}
			Graphics.Blit(inTexture, input);
			if (!buffer)
			{
				buffer = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				buffer.enableRandomWrite = true;
				buffer.Create();

				var prev = RenderTexture.active;
				RenderTexture.active = buffer;
				GL.Clear(true, true, Color.black); // Color.black is (0,0,0,1)
				RenderTexture.active = prev;
			}
			if (!buffer2)
			{
				buffer2 = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				buffer2.enableRandomWrite = true;
				buffer2.Create();

				var prev = RenderTexture.active;
				RenderTexture.active = buffer;
				GL.Clear(true, true, Color.black); // Color.black is (0,0,0,1)
				RenderTexture.active = prev;
			}
			// run adaptiveBlur on buffer2

			if (adaptiveBlurMaterial)
			{
				if (!adaptiveBlurRenderTexture)
				{
					adaptiveBlurRenderTexture = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
					adaptiveBlurRenderTexture.enableRandomWrite = true;
					adaptiveBlurRenderTexture.Create();
				}
				Graphics.Blit(buffer2, adaptiveBlurRenderTexture, adaptiveBlurMaterial);
				Graphics.Blit(adaptiveBlurRenderTexture, outTexture);
				//Debug.Log("Applied adaptive blur", this);
			}
			else
			{
				Graphics.Blit(buffer2, outTexture);
				//Debug.Log("Skipped adaptive blur", this);
			}
		}
	}
}
