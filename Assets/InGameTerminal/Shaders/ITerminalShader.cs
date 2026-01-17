using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace InGameTerminal.Shaders
{
	public interface ITerminalShader
	{
		public void Init(RenderTexture renderTexture);
	}
}
