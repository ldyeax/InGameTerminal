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
	public class VT320_Fancy_2 : MonoBehaviour
	{
		[SerializeField]
		private Shader shader;

		private Material bufferMaterial;
		private RenderTexture buffer;

		[SerializeField]
		private Color _Color;
		[SerializeField]
		[Range(0.0f, 1.0f)]
		private float _PixelSnap;
		[SerializeField]
		[Range(0.0f, 0.5f)]
		private float _ScanlineGap;
		[Range(0.0f, 2.0f)]
		[SerializeField]
		private float _PixelRoundness;
		[Range(0.1f, 10.0f)]
		[SerializeField]
		private float _RoundnessAspect;
		[Range(1.0f, 16.0f)]
		[SerializeField]
		private float _VerticalSpan;
		[Range(0.0f, 1.0f)]
		[SerializeField]
		private float _PassThrough;
		[Range(-1.0f, 1.0f)]
		[SerializeField]
		private float _Threshold;

		private const string RedrawPrescaledInput_HiRes_Name = "RedrawPrescaledInput_HiRes";

		private void OnEnable()
		{
			bufferMaterial = null;
			buffer = null;

			if (shader == null)
			{
				Debug.LogError("Shader is not assigned!", this);
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
			bufferMaterial = null;
			buffer = null;
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

		public void OnRenderImage(RenderTexture inTexture, RenderTexture outTexture)
		{
			if (!bufferMaterial)
			{
				bufferMaterial = new Material(shader);
			}
			if (!buffer)
			{
				buffer = new RenderTexture(inTexture.width, inTexture.height, 0, RenderTextureFormat.ARGB32);
				buffer.enableRandomWrite = true;
				buffer.Create();
				ClearBlack(buffer);
			}

			bufferMaterial.SetColor("_Color", _Color);
			bufferMaterial.SetFloat("_PixelSnap", _PixelSnap);
			bufferMaterial.SetFloat("_ScanlineGap", _ScanlineGap);
			bufferMaterial.SetFloat("_PixelRoundness", _PixelRoundness);
			bufferMaterial.SetFloat("_RoundnessAspect", _RoundnessAspect);
			bufferMaterial.SetFloat("_VerticalSpan", _VerticalSpan);
			bufferMaterial.SetFloat("_PassThrough", _PassThrough);
			bufferMaterial.SetFloat("_Threshold", _Threshold);

			Graphics.Blit(inTexture, buffer, bufferMaterial);
			Graphics.Blit(buffer, outTexture);
		}
	}
}
