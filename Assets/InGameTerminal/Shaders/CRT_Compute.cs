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
		private RenderTexture buffer;
		private RenderTexture input;
		private void OnEnable()
		{
			if (computeShader == null)
			{
				Debug.LogError("ComputeShader is not assigned!", this);
				enabled = false;
				return;
			}

			kernel = computeShader.FindKernel("CSMain");
			Debug.Log($"Kernel index: {kernel}", this);
		}
		private void Start()
		{
			//material = new Material(computeShader);
		}
		private void Update()
		{
			//RenderTexture currentSource = renderTexture;
			//RenderTexture temp = RenderTexture.GetTemporary(currentSource.width, currentSource.height, 0, currentSource.format);
			//Graphics.Blit(currentSource, temp, material);
			//if (currentSource != renderTexture)
			//{
			//	RenderTexture.ReleaseTemporary(currentSource);
			//}

			// Set texture before each dispatch
			//computeShader.SetTexture(kernel, "_UIInput", renderTexture);
			//this.computeShader.Dispatch(kernel, renderTexture.width / 8, renderTexture.height / 8, 1);

			if (input != null)
			{
				computeShader.SetTexture(kernel, "_UIInput", input);
				computeShader.SetFloat("_decay", Time.deltaTime*(1.0f/decay));
				computeShader.SetTexture(kernel, "_Buffer", buffer);
				this.computeShader.Dispatch(kernel, input.width / 8, input.height / 8, 1);
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
			}
			Graphics.Blit(buffer, outTexture);
		}
	}
}
