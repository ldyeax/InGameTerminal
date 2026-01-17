using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InGameTerminal.Shaders
{
	[ExecuteAlways]
	public class FillWithRed : MonoBehaviour, ITerminalShader
	{
		private RenderTexture renderTexture;
		[SerializeField]
		private ComputeShader computeShader;
		public void Init(RenderTexture renderTexture)
		{
			this.renderTexture = renderTexture;
			if (!computeShader)
			{
				Debug.LogWarning("Compute Shader not assigned in FillWithRed shader.", this);
				return;
			}
			int k = computeShader.FindKernel("CSMain");
			computeShader.SetTexture(k, "_UIInput", renderTexture);
		}
	}
}
