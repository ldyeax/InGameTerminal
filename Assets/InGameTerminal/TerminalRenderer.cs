/**
 * Attributes are composited in this order:
 * - Base glyph fetch
 * - Italic shear
 * - Bold OR-shift
 * - Underline row force
 * - Reverse video
 * - Blink mask
 * 
 * Horizontal line at 17,7
 * Vertical line at 24,7
 **/

using InGameTerminal;
using InGameTerminal.Elements;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace InGameTerminal
{
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class TerminalRenderer : MonoBehaviour
	{
		private ITerminalDefinition _terminalDefinition;

		private TerminalState terminalState;
		private bool firstUpdate = true;
		private List<TerminalCommand> terminalCommands = new List<TerminalCommand>();

		[SerializeField]
		private Terminal terminal;
// 		List<TerminalCommand> terminalCommands = new List<TerminalCommand>();
		//private void BuildTerminalCommands()
		//{
		//	terminalCommands.Clear();
		//	terminalCommands.Add(new TerminalCommand()
		//	{
		//		CommandType = TerminalCommandType.HomeCursor
		//	});
		//	if (firstUpdate)
		//	{
		//		terminalCommands.Add(new TerminalCommand()
		//		{
		//			CommandType = TerminalCommandType.EraseInDisplay
		//		});
		//		return;
		//	}

		//	Vector2Int cursorPosition = default;
			
		//	for (int y = 0; y < terminal.Height; y++)
		//	{
		//		for (int x = 0; x < terminal.Width; x++)
		//		{
		//			var cell = terminalBuffer[x, y];
		//			var previousCell = previousTerminalBuffer[x, y];
		//			if (cell != previousCell)
		//			{
		//				//Debug.Log($"Cell changed at {x},{y} from '{previousCell.GetChar(_terminalDefinition)}' to '{cell.GetChar(_terminalDefinition)}'");
		//				if (cursorPosition.x != x || cursorPosition.y != y)
		//				{
		//					terminalCommands.Add(new TerminalCommand()
		//					{
		//						CommandType = TerminalCommandType.MoveTo,
		//						X = x,
		//						Y = y
		//					});
		//				}
		//				terminalCommands.Add(new TerminalCommand()
		//				{
		//					Char = cell.GetChar(_terminalDefinition),
		//					X = x,
		//					Y = y
		//				});
		//				cursorPosition.x++;
		//				if (cursorPosition.x > terminal.Width)
		//				{
		//					cursorPosition.x = 0;
		//					cursorPosition.y++;
		//				}
		//				if (cursorPosition.y > terminal.Height)
		//				{
		//					cursorPosition.y = 0;
		//				}
		//			}
		//		}
		//	}
		//}

		
		private Canvas _unityCanvas;
		private CanvasRenderer _canvasRenderer;
		
		private void Awake()
		{
			_canvasRenderer = Util.GetOrCreateComponent<CanvasRenderer>(gameObject);

			if (_mesh == null)
			{
				_mesh = new Mesh();
				_mesh.MarkDynamic();
			}
		}

		#region mesh
		private Mesh _mesh;
		private void InitMesh()
		{
			_mesh.Clear();
			vertices.Clear();
			uvs.Clear();
			colors.Clear();
			triangles.Clear();
			vertexOffset = 0;

			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					InitChar(x, y, '%');
				}
			}

			_mesh.SetVertices(vertices);
			_mesh.SetUVs(0, uvs);
			_mesh.SetColors(colors);
			_mesh.SetTriangles(triangles, 0);
			_mesh.RecalculateBounds();
			
			_canvasRenderer.SetMesh(_mesh);
			_canvasRenderer.SetMaterial(_terminalDefinition.Atlas, null);
		}

		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> triangles = new List<int>();
		List<Color32> colors = new List<Color32>();

		int vertexOffset = 0;
		private void InitChar(int terminalX, int terminalY, char c)
		{
			var atlastXY = _terminalDefinition.CharToXY(c);
			var atlasX = atlastXY.x;
			var atlasY = atlastXY.y;
			float pixelX = terminalX * _terminalDefinition.GlyphWidth;
			float pixelY = terminalY * _terminalDefinition.GlyphHeight;

			// Calculate UVs
			float uvLeft = 1.0f - (float)atlasX / _terminalDefinition.AtlasCols;
			float uvRight = 1.0f - (float)(atlasX + 1) / _terminalDefinition.AtlasCols;
			float uvTop = 1.0f - (float)atlasY / _terminalDefinition.AtlasRows;
			float uvBottom = 1.0f - (float)(atlasY + 1) / _terminalDefinition.AtlasRows;

			// Calculate vertex positions
			float xPos = pixelX;
			float yPos = -pixelY; // Flip Y for UI space

			_terminalDefinition.AlignX(ref xPos);
			_terminalDefinition.AlignY(ref yPos);

			// Add quad vertices (top-left origin)
			vertices.Add(new Vector3(xPos, yPos, 0));
			vertices.Add(new Vector3(xPos + _terminalDefinition.GlyphWidth, yPos, 0));
			vertices.Add(new Vector3(xPos + _terminalDefinition.GlyphWidth, yPos - _terminalDefinition.GlyphHeight, 0));
			vertices.Add(new Vector3(xPos, yPos - _terminalDefinition.GlyphHeight, 0));

			// Add UVs
			uvs.Add(new Vector2(uvLeft, uvTop));
			uvs.Add(new Vector2(uvRight, uvTop));
			uvs.Add(new Vector2(uvRight, uvBottom));
			uvs.Add(new Vector2(uvLeft, uvBottom));

			// Add colors (white by default)
			colors.Add(Color.white);
			colors.Add(Color.white);
			colors.Add(Color.white);
			colors.Add(Color.white);

			// Add triangles (two triangles per quad)
			triangles.Add(vertexOffset + 0);
			triangles.Add(vertexOffset + 1);
			triangles.Add(vertexOffset + 2);

			triangles.Add(vertexOffset + 0);
			triangles.Add(vertexOffset + 2);
			triangles.Add(vertexOffset + 3);

			vertexOffset += 4;
		}

		/// <summary>
		/// Find the existing character quad in the mesh and update its UVs to match the given atlas coordinates
		/// </summary>
		private void DrawCharToMesh(
			int atlasX,
			int atlasY,
			int terminalX,
			int terminalY
		)
		{
			// Each cell has 4 vertices, calculate the starting index
			int cellIndex = terminalY * terminal.Width + terminalX;
			int vertexIndex = cellIndex * 4;

			// Calculate UVs
			float uvLeft = (float)atlasX / _terminalDefinition.AtlasCols;
			float uvRight = (float)(atlasX + 1) / _terminalDefinition.AtlasCols;
			float uvTop = 1.0f - (float)atlasY / _terminalDefinition.AtlasRows;
			float uvBottom = 1.0f - (float)(atlasY + 1) / _terminalDefinition.AtlasRows;

			// Update UVs for the quad
			uvs[vertexIndex + 0] = new Vector2(uvLeft, uvTop);
			uvs[vertexIndex + 1] = new Vector2(uvRight, uvTop);
			uvs[vertexIndex + 2] = new Vector2(uvRight, uvBottom);
			uvs[vertexIndex + 3] = new Vector2(uvLeft, uvBottom);
		}
		#endregion mesh


		private void DrawBuffer()
		{
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;

			ref TerminalBufferValue testCell = ref terminalBuffer[0, 0];
			testCell.SetChar(_terminalDefinition, '&');

			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					var cell = terminalBuffer[x, y];
					var previousCell = previousTerminalBuffer[x, y];

					//if (cell != previousCell || forceRedraw)
					{
						DrawCharToMesh(cell.AtlasX, cell.AtlasY, x, y);
					}
				}
			}
		}
		private void UpdateUVs()
		{
			// Update the mesh UVs
			_mesh.SetUVs(0, uvs);
			_mesh.UploadMeshData(false);
			
			// Force the canvas renderer to update
			_canvasRenderer.SetMesh(_mesh);
		}
		private void DrawTerminalCommands(List<TerminalCommand> terminalCommands)
		{
			Vector2Int position = default;
			bool italic = false;
			bool bold = false;
			bool underline = false;
			bool inverted = false;
			bool blink = false;

			Vector2Int spaceXY = _terminalDefinition.CharToXY(' ');

			foreach (var command in terminalCommands)
			{
				switch (command.CommandType)
				{
					case TerminalCommandType.Char:
						if (position.x >= 0 && position.x < terminal.Width &&
							position.y >= 0 && position.y < terminal.Height)
						{
							Vector2Int atlasXY = _terminalDefinition.CharToXY(command.Char);
							DrawCharToMesh(atlasXY.x, atlasXY.y, position.x, position.y);
						}
						position.x++;
						if (position.x >= terminal.Width)
						{
							position.x = 0;
							position.y++;
						}
						if (position.y >= terminal.Height)
						{
							position.y = 0;
						}
						break;

					case TerminalCommandType.Up:
						position.y--;
						if (position.y < 0)
						{
							position.y = terminal.Height - 1;
						}
						break;

					case TerminalCommandType.Down:
						position.y++;
						if (position.y >= terminal.Height)
						{
							position.y = 0;
						}
						break;

					case TerminalCommandType.Left:
						position.x--;
						if (position.x < 0)
						{
							position.x = terminal.Width - 1;
						}
						break;

					case TerminalCommandType.Right:
						position.x++;
						if (position.x >= terminal.Width)
						{
							position.x = 0;
						}
						break;

					case TerminalCommandType.MoveTo:
						position.x = command.X;
						position.y = command.Y;
						break;

					case TerminalCommandType.CarriageReturn:
						position.x = 0;
						break;

					case TerminalCommandType.LineFeed:
						position.y++;
						if (position.y >= terminal.Height)
						{
							position.y = 0;
						}
						break;

					case TerminalCommandType.Italic:
						italic = !italic;
						break;

					case TerminalCommandType.Bold:
						bold = !bold;
						break;

					case TerminalCommandType.Underline:
						underline = !underline;
						break;

					case TerminalCommandType.Invert:
						inverted = !inverted;
						break;

					case TerminalCommandType.Blink:
						blink = !blink;
						break;

					case TerminalCommandType.EL:
						// Erase in Line - clear from cursor to end of line
						
						for (int x = position.x; x < terminal.Width; x++)
						{
							DrawCharToMesh(spaceXY.x, spaceXY.y, x, position.y);
						}
						break;

					case TerminalCommandType.EraseInDisplay:
						// Erase in Display - clear from cursor to end of display
						for (int y = position.y; y < terminal.Height; y++)
						{
							int startX = (y == position.y) ? position.x : 0;
							for (int x = startX; x < terminal.Width; x++)
							{
								DrawCharToMesh(spaceXY.x, spaceXY.y, x, y);
							}
						}
						break;

					case TerminalCommandType.HomeCursor:
						position.x = 0;
						position.y = 0;
						break;
				}
			}
		}
		
		


		
		private void OnDestroy()
		{
			if (_mesh != null)
			{
				DestroyImmediate(_mesh);
			}
		}


		public bool DebugUpdate = false;
		public bool DebugReadyToUpdate = false;

		private void Update()
		{
			if (DebugUpdate && !DebugReadyToUpdate)
			{
				return;
			}
			DebugReadyToUpdate = false;
			if (!terminal)
			{
				terminal = GetComponent<Terminal>();
			}
			if (!terminal)
			{
				Debug.Log($"TerminalRenderer on GameObject '{gameObject.name}' is missing a Terminal component.");
				return;
			}
			if (!_unityCanvas)
			{
				_unityCanvas = terminal.GetCanvas();
			}
			if (_terminalDefinition == null)
			{
				_terminalDefinition = terminal.TerminalDefinition;
			}
			if (_terminalDefinition == null)
			{
				Debug.Log($"TerminalRenderer on GameObject '{gameObject.name}' is missing a TerminalDefinition.");
				return;
			}
			ref var terminalState = ref this.terminalState;
			ref var terminalBuffer = ref terminalState.terminalBuffer;
			ref var previousTerminalBuffer = ref terminalState.previousTerminalBuffer;
			if (terminalBuffer == null)
			{
				firstUpdate = true;
				terminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				previousTerminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				InitMesh();
			}
			terminal.BuildBuffer(ref terminalState, firstUpdate);
			terminal.BuildTerminalCommands(ref terminalState, terminalCommands, firstUpdate);
			//StringBuilder terminalCommandsDebug = new();
			//terminalCommandsDebug.Append("Terminal commands: [");
			//foreach (var cmd in terminalCommands)
			//{
			//	terminalCommandsDebug.Append($"{cmd.CommandType} {cmd.X} {cmd.Y}, ");
			//}
			//terminalCommandsDebug.Append("]");
			//Debug.Log(terminalCommandsDebug, this);
			DrawTerminalCommands(terminalCommands);
			//DrawBuffer();
			UpdateUVs();
			firstUpdate = false;
		}

		private void OnEnable()
		{
			firstUpdate = true;
		}

	}
}
