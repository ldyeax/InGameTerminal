using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InGameTerminal.Shaders
{
	[ExecuteAlways]
	public class Phosphor : MonoBehaviour
	{
		[SerializeField]
		private ComputeShader computeShader;
		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float _Decay = 1.0f;
		private int kernel_Phosphor;
		private RenderTexture _UIInput;
		private RenderTexture _UIOutput;

		private const string PhosphorKernelName = "Phosphor";

		private void OnEnable()
		{
			_UIInput = null;
			_UIOutput = null;

			if (computeShader == null)
			{
				Debug.LogError("ComputeShader is not assigned!", this);
				enabled = false;
				return;
			}

			try
			{
				kernel_Phosphor = computeShader.FindKernel(PhosphorKernelName);
			}
			catch (ArgumentException)
			{
				Debug.LogError($"Failed to find kernel '{PhosphorKernelName}' in ComputeShader!", this);
				enabled = false;
				return;
			}
		}

		private void Update()
		{
			if (_UIInput && _UIOutput && computeShader)
			{
				computeShader.SetTexture(kernel_Phosphor, "_UIInput", _UIInput);
				computeShader.SetTexture(kernel_Phosphor, "_UIOutput", _UIOutput);
				computeShader.SetFloat("_Decay", Time.unscaledDeltaTime / _Decay);
				computeShader.Dispatch(kernel_Phosphor, _UIInput.width / 8, _UIInput.height / 8, 1);
			}
		}
		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
		{
			if (!_UIInput)
			{
				_UIInput = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				_UIInput.enableRandomWrite = true;
				_UIInput.Create();
			}
			Graphics.Blit(inTexture, _UIInput);
			if (!_UIOutput)
			{
				_UIOutput = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				_UIOutput.enableRandomWrite = true;
				_UIOutput.Create();
			}

			Graphics.Blit(_UIOutput, outTexture);
		}
	}
}
