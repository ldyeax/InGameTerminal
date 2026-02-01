#if CRT_COMPUTE

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
		[SerializeField]
		[Range(-3.0f, 0.0f)]
		private float startYOffset = 0.0f;
		[SerializeField]
		[Range(0.0f, 3.0f)]
		private float endYOffset = 0.0f;
		private int kernel_RedrawPrescaledInput;
		private int kernel_RedrawPrescaledInput_HiRes;
		private int kernel_Phosphor;
		private RenderTexture buffer;
		private RenderTexture buffer2;
		private RenderTexture input;
		private RenderTexture hiResBuffer;

		public bool RedrawPrescaledInput;
		public bool Phosphor;

		[Range(0.0f, 4.0f)]
		public float SampleScaleY = 1.0f;
		[Range(-33.0f, 33.0f)]
		public float SampleOffsetY = 0.0f;

		[SerializeField]
		private Camera mainCamera;

		public bool HiRes;

		private void OnEnable()
		{
			if (!mainCamera)
			{
				Debug.LogError("Main Camera not set", this);
				enabled = false;
				return;
			}
			buffer = null;
			buffer2 = null;
			input = null;

			redraw_input = null;
			redraw_output = null;
			phosphor_input = null;
			phosphor_output = null;

			runRedraw = false;
			runPhosphor = false;

			renderCount = 0;
			updateCount = 0;

			if (computeShader == null)
			{
				Debug.LogError("ComputeShader is not assigned!", this);
				enabled = false;
				return;
			}

			kernel_RedrawPrescaledInput = computeShader.FindKernel("RedrawPrescaledInput");
			kernel_RedrawPrescaledInput_HiRes = computeShader.FindKernel("RedrawPrescaledInput_HiRes");
			kernel_Phosphor = computeShader.FindKernel("Phosphor");
		}
		private RenderTexture outputBuffer
		{
			get
			{
				if (RedrawPrescaledInput)
				{
					if (Phosphor)
					{
						return buffer2;
					}
					return buffer;
				}
				if (Phosphor)
				{
					return buffer;
				}
				return null;
			}
		}
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

		private bool runRedraw = false;
		private bool runPhosphor = false;
		private RenderTexture redraw_input = null;
		private RenderTexture redraw_output = null;
		private RenderTexture phosphor_input = null;
		private RenderTexture phosphor_output = null;

		private void Update()
		{
			if (updateCount >= renderCount)
			{
				return;
			}
			updateCount++;

			redraw_input = input;
			redraw_output = buffer;
			if (HiRes)
			{
				redraw_output = hiResBuffer;
			}
			phosphor_input = input;
			phosphor_output = buffer;
			runRedraw = redraw_input && redraw_output && RedrawPrescaledInput;
			if (runRedraw)
			{
				phosphor_input = buffer;
				phosphor_output = buffer2;
				computeShader.SetTexture(kernel_RedrawPrescaledInput, "_UIInput", redraw_input);
				computeShader.SetTexture(kernel_RedrawPrescaledInput, "_Buffer", redraw_output);
				computeShader.SetFloat("_StartYOffset", startYOffset);
				computeShader.SetFloat("_EndYOffset", endYOffset);
				computeShader.SetFloat("_SampleScaleY", SampleScaleY);
				computeShader.SetFloat("_SampleOffsetY", SampleOffsetY);
				this.computeShader.Dispatch(kernel_RedrawPrescaledInput, 1, 24, 1);
			}
			runPhosphor = phosphor_input && phosphor_output && Phosphor;
			if (runPhosphor)
			{
				computeShader.SetFloat("_Decay", Time.unscaledDeltaTime * (1.0f / decay));
				computeShader.SetTexture(kernel_Phosphor, "_Buffer", phosphor_input);
				computeShader.SetTexture(kernel_Phosphor, "_Buffer2", phosphor_output);
				this.computeShader.Dispatch(kernel_Phosphor, input.width / 8, input.height / 8, 1);
			}

			if (runRedraw)
			{
				//Clear(redraw_output);
			}
		}
		private void LateUpdate()
		{

		}
		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
		{
			renderCount++;
			if (!input)
			{
				input = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				input.enableRandomWrite = true;
				input.Create();
			}
			Graphics.Blit(inTexture, input);
			if (!HiRes && !buffer)
			{
				buffer = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				buffer.enableRandomWrite = true;
				buffer.Create();
				ClearBlack(buffer);
			}
			if (HiRes && !hiResBuffer)
			{
				hiResBuffer = new RenderTexture(inTexture.width, inTexture.height*3, 0, RenderTextureFormat.ARGB32);
				hiResBuffer.enableRandomWrite = true;
				hiResBuffer.Create();
				ClearBlack(hiResBuffer);
			}
			if (!buffer2)
			{
				buffer2 = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				buffer2.enableRandomWrite = true;
				buffer2.Create();
				ClearBlack(buffer2);
			}
			if (outputBuffer)
			{
				Graphics.Blit(outputBuffer, outTexture);
			}
			else
			{
				Graphics.Blit(inTexture, outTexture);
			}

		}
	}
}

#endif
