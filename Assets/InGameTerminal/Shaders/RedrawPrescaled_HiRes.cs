using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace InGameTerminal.Shaders
{
	[ExecuteAlways]
	public class RedrawPrescaled_HiRes : MonoBehaviour
	{
		[SerializeField]
		private ComputeShader computeShader;
		[SerializeField]
		private bool round = false;

		private int kernel_RedrawPrescaledInput_HiRes;

		private RenderTexture _UIInput;
		private RenderTexture _UIOutput;

		private const string RedrawPrescaledInput_HiRes_Name = "RedrawPrescaledInput_HiRes";

		private void OnEnable()
		{
			_UIInput = null;
			_UIOutput = null;

			renderCount = 0;
			updateCount = 0;

			if (computeShader == null)
			{
				Debug.LogError("ComputeShader is not assigned!", this);
				enabled = false;
				return;
			}

			try
			{
				kernel_RedrawPrescaledInput_HiRes = computeShader.FindKernel(RedrawPrescaledInput_HiRes_Name);
			}
			catch (ArgumentException)
			{
				Debug.LogError($"Failed to find kernel '{RedrawPrescaledInput_HiRes_Name}'!", this);
				enabled = false;
				return;
			}

#if UNITY_EDITOR
			AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;
#endif
		}
#if UNITY_EDITOR
		private void AssemblyReloadEvents_afterAssemblyReload()
		{
			_UIInput = null;
			_UIOutput = null;

			renderCount = 0;
			updateCount = 0;
		}
#endif

		private void ClearBlack(RenderTexture buffer)
		{
			var prev = RenderTexture.active;
			RenderTexture.active = buffer;
			GL.Clear(true, true, Color.black); // Color.black is (0,0,0,1)
			RenderTexture.active = prev;
		}
		private void Clear(RenderTexture buffer)
		{
			var prev = RenderTexture.active;
			RenderTexture.active = buffer;
			GL.Clear(true, true, Color.clear); // Color.clear is (0,0,0,0)
			RenderTexture.active = prev;
		}

		private int renderCount = 0;
		private int updateCount = 0;

		private void Update()
		{
			if (updateCount >= renderCount)
			{
				return;
			}
			updateCount++;

			if (!_UIInput || !_UIOutput)
			{
				return;
			}

			computeShader.SetTexture(kernel_RedrawPrescaledInput_HiRes, "_UIInput", _UIInput);
			computeShader.SetTexture(kernel_RedrawPrescaledInput_HiRes, "_UIOutput", _UIOutput);
			//Debug.Log($"{_UIInput.width}x{_UIInput.height} -> {_UIOutput.width}x{_UIOutput.height}");
			computeShader.SetBool("_Round", round);
			computeShader.Dispatch(kernel_RedrawPrescaledInput_HiRes, 1, 24, 1);
		}
		private void LateUpdate()
		{

		}
		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
		{
			renderCount++;
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
				ClearBlack(_UIOutput);
			}

			Graphics.Blit(_UIOutput, outTexture);
		}
	}
}
