//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UnityEngine;

//namespace InGameTerminal.Shaders
//{
//	[ExecuteAlways]
//	public class RedrawPrescaled : MonoBehaviour
//	{
//		[SerializeField]
//		private ComputeShader computeShader;

//		[SerializeField]
//		[Range(-3.0f, 0.0f)]
//		private float startYOffset = 0.0f;
//		[SerializeField]
//		[Range(0.0f, 3.0f)]
//		private float endYOffset = 0.0f;
//		[Range(0.0f, 4.0f)]
//		public float SampleScaleY = 1.0f;
//		[Range(-33.0f, 33.0f)]
//		public float SampleOffsetY = 0.0f;

//		private int kernel_RedrawPrescaledInput;
//		private int kernel_RedrawPrescaledInput_HiRes;

//		private RenderTexture _UIInput;
//		private RenderTexture _HiResBuffer;
//		private RenderTexture _UIOutput;

//		public bool HiRes;

//		private const string RedrawPrescaledInput_Name = "RedrawPrescaledInput";
//		private const string RedrawPrescaledInput_HiRes_Name = "RedrawPrescaledInput_HiRes";

//		private void OnEnable()
//		{
//			_UIInput = null;
//			_UIOutput = null;
//			_HiResBuffer = null;

//			renderCount = 0;
//			updateCount = 0;

//			if (computeShader == null)
//			{
//				Debug.LogError("ComputeShader is not assigned!", this);
//				enabled = false;
//				return;
//			}

//			try
//			{
//				kernel_RedrawPrescaledInput = computeShader.FindKernel(RedrawPrescaledInput_Name);
//			}
//			catch (ArgumentException)
//			{
//				Debug.LogError($"Failed to find kernel '{RedrawPrescaledInput_Name}'!", this);
//				enabled = false;
//				return;
//			}

//			try
//			{
//				kernel_RedrawPrescaledInput_HiRes = computeShader.FindKernel(RedrawPrescaledInput_HiRes_Name);
//			}
//			catch (ArgumentException)
//			{
//				Debug.LogError($"Failed to find kernel '{RedrawPrescaledInput_HiRes_Name}'!", this);
//				enabled = false;
//				return;
//			}
//		}

//		private void ClearBlack(RenderTexture buffer)
//		{
//			var prev = RenderTexture.active;
//			RenderTexture.active = buffer;
//			GL.Clear(true, true, Color.black); // Color.black is (0,0,0,1)
//			RenderTexture.active = prev;
//		}
//		private void Clear(RenderTexture buffer)
//		{
//			var prev = RenderTexture.active;
//			RenderTexture.active = buffer;
//			GL.Clear(true, true, Color.clear); // Color.clear is (0,0,0,0)
//			RenderTexture.active = prev;
//		}

//		private int renderCount = 0;
//		private int updateCount = 0;


//		private void Update()
//		{
//			if (updateCount >= renderCount)
//			{
//				return;
//			}
//			updateCount++;

//			if (!_UIInput || !_UIOutput)
//			{
//				return;
//			}

//			//if (HiRes && !_HiResBuffer)
//			//{
//			//	return;
//			//}

//			//computeShader.SetFloat("_StartYOffset", startYOffset);
//			//computeShader.SetFloat("_EndYOffset", endYOffset);
//			//computeShader.SetFloat("_SampleScaleY", SampleScaleY);
//			//computeShader.SetFloat("_SampleOffsetY", SampleOffsetY);
//			////computeShader.SetBool("_HiRes", HiRes);
//			//computeShader.SetTexture(kernel_RedrawPrescaledInput, "_UIInput", _UIInput);
//			//computeShader.SetTexture(kernel_RedrawPrescaledInput, "_UIOutput", _UIOutput);
//			//computeShader.SetTexture(kernel_RedrawPrescaledInput, "_HiResBuffer", _HiResBuffer);

//			computeShader.SetFloat("_StartYOffset", startYOffset);
//			computeShader.SetFloat("_EndYOffset", endYOffset);
//			computeShader.SetFloat("_SampleScaleY", SampleScaleY);
//			computeShader.SetFloat("_SampleOffsetY", SampleOffsetY);
//		}
//		private void LateUpdate()
//		{

//		}
//		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
//		{
//			renderCount++;
//			if (!_UIInput)
//			{
//				_UIInput = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
//				_UIInput.enableRandomWrite = true;
//				_UIInput.Create();
//			}
//			Graphics.Blit(inTexture, _UIInput);
//			if (!_UIOutput)
//			{
//				_UIOutput = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
//				_UIOutput.enableRandomWrite = true;
//				_UIOutput.Create();
//				ClearBlack(_UIOutput);
//			}
//			if (HiRes && !_HiResBuffer)
//			{
//				_HiResBuffer = new RenderTexture(inTexture.width, inTexture.height * 4, 0, RenderTextureFormat.ARGB32);
//				_HiResBuffer.enableRandomWrite = true;
//				_HiResBuffer.Create();
//				ClearBlack(_HiResBuffer);
//			}

//			Graphics.Blit(_UIOutput, outTexture);
//		}
//	}
//}
