using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InGameTerminal.Shaders
{
	[ExecuteAlways]
	public class VT320_First_Pass_Compute : MonoBehaviour
	{
		[SerializeField]
		private ComputeShader computeShader;
		[SerializeField]
		private int kernel;
		private RenderTexture UI_Input;
		private RenderTexture UI_Output;
		private void OnEnable()
		{
			UI_Input = null;
			UI_Output = null;

			if (computeShader == null)
			{
				Debug.LogError("ComputeShader is not assigned!", this);
				enabled = false;
				return;
			}

			kernel= computeShader.FindKernel("CSMain");
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
		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
		{
			if (!UI_Input)
			{
				UI_Input = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				UI_Input.enableRandomWrite = true;
				UI_Input.Create();
			}
			Graphics.Blit(inTexture, UI_Input);

			if (!UI_Output)
			{
				UI_Output = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				UI_Output.enableRandomWrite = true;
				UI_Output.Create();
				ClearBlack(UI_Output);
			}
			Graphics.Blit(UI_Output, outTexture);
		}
	}
}
