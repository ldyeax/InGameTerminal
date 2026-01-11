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

namespace InGameTerminal
{
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class TerminalRenderer : MonoBehaviour
	{
		private TerminalDefinition _terminalDefinition;
		private struct TerminalBufferValue
		{
			private TerminalDefinition _terminalDefinition;
			public TerminalBufferValue(TerminalDefinition terminalDefinition)
			{
				_terminalDefinition = terminalDefinition;
				AtlasX = 0;
				AtlasY = 0;
				ConnectorID = 0;
				Italic = false;
				Bold = false;
				Underline = false;
				Inverted = false;
				Blink = false;
			}
			public char Char
			{
				get
				{
					int index = AtlasY * _terminalDefinition.AtlasCols + AtlasX;
					return (char)index;
				}
				set
				{
					Vector2Int charXY = _terminalDefinition.CharToXY(value);
					AtlasX = charXY.x;
					AtlasY = charXY.y;
				}
			}
			public int AtlasX;
			public int AtlasY;
			public int ConnectorID;
			public bool Italic;
			public bool Bold;
			public bool Underline;
			public bool Inverted;
			public bool Blink;
			public static bool operator ==(TerminalBufferValue a, TerminalBufferValue b)
			{
				return
					a.ConnectorID == b.ConnectorID &&
					a.Italic == b.Italic &&
					a.Bold == b.Bold &&
					a.Underline == b.Underline &&
					a.Inverted == b.Inverted &&
					a.Blink == b.Blink;
			}
			public static bool operator !=(TerminalBufferValue a, TerminalBufferValue b)
			{
				return !(a == b);
			}
			public override readonly bool Equals(object obj)
			{
				if (obj is TerminalBufferValue other)
				{
					return this == other;
				}
				return false;
			}
			public override readonly int GetHashCode()
			{
				return base.GetHashCode();
			}
		}

		private TerminalBufferValue[,] terminalBuffer = null;
		private TerminalBufferValue[,] previousTerminalBuffer = null;
		private TerminalBufferValue lastBufferValueState = default;

		enum TerminalCommandType
		{
			Up,
			Down,
			Left,
			Right,
			MoveTo,
			CarriageReturn,
			LineFeed,
			Italic,
			Bold,
			Underline,
			Invert,
			Blink,
			EL,
			EraseInDisplay,
			HomeCursor,
		}
		struct TerminalCommand
		{
			public char Char;
			public TerminalCommandType CommandType;
			public int X;
			public int Y;
		}

		private void ActualizeConnectedLinesToBuffer()
		{
			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					ref TerminalBufferValue cell = ref terminalBuffer[x, y];
					if (cell.ConnectorID != 0)
					{
						int aboveID = 0;
						if (y > 0)
						{
							aboveID = terminalBuffer[x, y - 1].ConnectorID;
						}
						int belowID = 0;
						if (y < terminal.Height - 1)
						{
							belowID = terminalBuffer[x, y + 1].ConnectorID;
						}
						int leftID = 0;
						if (x > 0)
						{
							leftID = terminalBuffer[x - 1, y].ConnectorID;
						}
						int rightID = 0;
						if (x < terminal.Width - 1)
						{
							rightID = terminalBuffer[x + 1, y].ConnectorID;
						}

						bool matchAbove = aboveID == cell.ConnectorID;
						bool matchBelow = belowID == cell.ConnectorID;
						bool matchLeft = leftID == cell.ConnectorID;
						bool matchRight = rightID == cell.ConnectorID;

						if (matchAbove)
						{
							if (matchBelow)
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// All four directions
										cell.AtlasX = _terminalDefinition.CrossX;
										cell.AtlasY = _terminalDefinition.CrossY;
									}
									else
									{
										// Above, below, left (no right) - LeftTee
										cell.AtlasX = _terminalDefinition.LeftTeeX;
										cell.AtlasY = _terminalDefinition.LeftTeeY;
									}
								}
								else if (matchRight)
								{
									// Above, below, right (no left) - RightTee
									cell.AtlasX = _terminalDefinition.RightTeeX;
									cell.AtlasY = _terminalDefinition.RightTeeY;
								}
								else
								{
									// Above and below only - Vertical line
									cell.AtlasX = _terminalDefinition.VerticalLineX;
									cell.AtlasY = _terminalDefinition.VerticalLineY;
								}
							}
							else // no below
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// Above, left, right (no below) - UpTee
										cell.AtlasX = _terminalDefinition.UpTeeX;
										cell.AtlasY = _terminalDefinition.UpTeeY;
									}
									else
									{
										// Above, left (no below, no right) - BottomRightCorner
										cell.AtlasX = _terminalDefinition.BottomRightCornerX;
										cell.AtlasY = _terminalDefinition.BottomRightCornerY;
									}
								}
								else if (matchRight)
								{
									// Above, right (no below, no left) - BottomLeftCorner
									cell.AtlasX = _terminalDefinition.BottomLeftCornerX;
									cell.AtlasY = _terminalDefinition.BottomLeftCornerY;
								}
								else
								{
									// Above only - Vertical line
									cell.AtlasX = _terminalDefinition.VerticalLineX;
									cell.AtlasY = _terminalDefinition.VerticalLineY;
								}
							}
						}
						else // no above
						{
							if (matchBelow)
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// Below, left, right (no above) - DownTee
										cell.AtlasX = _terminalDefinition.DownTeeX;
										cell.AtlasY = _terminalDefinition.DownTeeY;
									}
									else
									{
										// Below, left (no above, no right) - TopRightCorner
										cell.AtlasX = _terminalDefinition.TopRightCornerX;
										cell.AtlasY = _terminalDefinition.TopRightCornerY;
									}
								}
								else if (matchRight)
								{
									// Below, right (no above, no left) - TopLeftCorner
									cell.AtlasX = _terminalDefinition.TopLeftCornerX;
									cell.AtlasY = _terminalDefinition.TopLeftCornerY;
								}
								else
								{
									// Below only - Vertical line
									cell.AtlasX = _terminalDefinition.VerticalLineX;
									cell.AtlasY = _terminalDefinition.VerticalLineY;
								}
							}
							else // no above, no below
							{
								if (matchLeft)
								{
									if (matchRight)
									{
										// Left and right only - Horizontal line
										cell.AtlasX = _terminalDefinition.HorizontalLineX;
										cell.AtlasY = _terminalDefinition.HorizontalLineY;
									}
									else
									{
										// Left only - Horizontal line
										cell.AtlasX = _terminalDefinition.HorizontalLineX;
										cell.AtlasY = _terminalDefinition.HorizontalLineY;
									}
								}
								else if (matchRight)
								{
									// Right only - Horizontal line
									cell.AtlasX = _terminalDefinition.HorizontalLineX;
									cell.AtlasY = _terminalDefinition.HorizontalLineY;
								}
								else
								{
									// No matches - default to vertical line
									cell.AtlasX = _terminalDefinition.VerticalLineX;
									cell.AtlasY = _terminalDefinition.VerticalLineY;
								}
							}
						}
					}
				}
			}
		}

		[SerializeField]
		private Terminal terminal;
		private void BuildTerminalCommands(List<TerminalCommand> output)
		{
			var currentState = lastBufferValueState;
			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					var cell = terminalBuffer[x, y];
					var previousCell = previousTerminalBuffer[x, y];
					if (cell != previousCell || forceRedraw)
					{
						// Move cursor if needed
						if (output.Count == 0 ||
							output.Last().X != x ||
							output.Last().Y != y)
						{
							output.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.MoveTo,
								X = x,
								Y = y
							});
						}
						// Apply attribute changes
						if (cell.Italic != currentState.Italic)
						{
							output.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.Italic
							});
						}
						if (cell.Bold != currentState.Bold)
						{
							output.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.Bold
							});
						}
						if (cell.Underline != currentState.Underline)
						{
							output.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.Underline
							});
						}
						if (cell.Inverted != currentState.Inverted)
						{
							output.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.Invert
							});
						}
						if (cell.Blink != currentState.Blink)
						{
							output.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.Blink
							});
						}
						if (cell.Char != currentState.Char)
						{
							output.Add(new TerminalCommand()
							{
								Char = cell.Char
							});
						}
						currentState = cell;
					}
				}
			}
		}

		
		private Canvas _unityCanvas;
		private CanvasRenderer _canvasRenderer;
		private Mesh _mesh;
		
		private List<Element> elementPool = new();
		private List<Elements.ConnectedLinesGroup> connectedLinesGroupPool = new();
		private int nextConnectorID = 1;

		
		private void Awake()
		{
			_canvasRenderer = Util.GetOrCreateComponent<CanvasRenderer>(gameObject);

			if (_mesh == null)
			{
				_mesh = new Mesh();
				_mesh.MarkDynamic();
			}
		}
		private bool forceRedraw = false;
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
		private void Update()
		{
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
			if (!_terminalDefinition)
			{
				_terminalDefinition = terminal.TerminalDefinition;
			}
			if (!_terminalDefinition)
			{
				Debug.Log($"TerminalRenderer on GameObject '{gameObject.name}' is missing a TerminalDefinition.");
				return;
			}
			if (terminalBuffer == null)
			{
				terminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				previousTerminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				forceRedraw = true;
				InitMesh();
			}

			terminal.GetComponentsInChildren<Element>(elementPool);

			BuildBuffer();
			DrawBuffer();
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
			float uvLeft = (float)atlasX / _terminalDefinition.AtlasCols;
			float uvRight = (float)(atlasX + 1) / _terminalDefinition.AtlasCols;
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

		private void DrawHorizontalLineToBuffer(
			int terminalY,
			int startTerminalX,
			int endTerminalX,
			int connectorID = 0
		)
		{
			for (int x = startTerminalX; x <= endTerminalX; x++)
			{
				ref TerminalBufferValue cell = ref terminalBuffer[x, terminalY];
				cell.AtlasX = _terminalDefinition.HorizontalLineX;
				cell.AtlasY = _terminalDefinition.HorizontalLineY;
				cell.ConnectorID = connectorID;
			}
		}

		private void DrawVerticalLineToBuffer(
			int terminalX,
			int startTerminalY,
			int endTerminalY,
			int connectorID = 0
		)
		{
			for (int y = startTerminalY; y <= endTerminalY; y++)
			{
				ref TerminalBufferValue cell = ref terminalBuffer[terminalX, y];
				cell.AtlasX = _terminalDefinition.VerticalLineX;
				cell.AtlasY = _terminalDefinition.VerticalLineY;
				cell.ConnectorID = connectorID;
			}
		}

		private void DrawTopLeftCornerToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.TopLeftCornerX;
			cell.AtlasY = _terminalDefinition.TopLeftCornerY;
			cell.ConnectorID = connectorID;
		}

		private void DrawTopRightCornerToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.TopRightCornerX;
			cell.AtlasY = _terminalDefinition.TopRightCornerY;
			cell.ConnectorID = connectorID;
		}

		private void DrawBottomLeftCornerToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.BottomLeftCornerX;
			cell.AtlasY = _terminalDefinition.BottomLeftCornerY;
			cell.ConnectorID = connectorID;
		}

		private void DrawBottomRightCornerToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.BottomRightCornerX;
			cell.AtlasY = _terminalDefinition.BottomRightCornerY;
			cell.ConnectorID = connectorID;
		}

		private void DrawLeftTeeToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.LeftTeeX;
			cell.AtlasY = _terminalDefinition.LeftTeeY;
			cell.ConnectorID = connectorID;
		}

		private void DrawRightTeeToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.RightTeeX;
			cell.AtlasY = _terminalDefinition.RightTeeY;
			cell.ConnectorID = connectorID;
		}

		private void DrawUpTeeToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.UpTeeX;
			cell.AtlasY = _terminalDefinition.UpTeeY;
			cell.ConnectorID = connectorID;
		}

		private void DrawDownTeeToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.DownTeeX;
			cell.AtlasY = _terminalDefinition.DownTeeY;
			cell.ConnectorID = connectorID;
		}

		private void DrawCrossToBuffer(
			int terminalX,
			int terminalY,
			int connectorID = 0
		)
		{
			ref TerminalBufferValue cell = ref terminalBuffer[terminalX, terminalY];
			cell.AtlasX = _terminalDefinition.CrossX;
			cell.AtlasY = _terminalDefinition.CrossY;
			cell.ConnectorID = connectorID;
		}

		private void DrawBoxToBuffer(
			int startTerminalX,
			int startTerminalY,
			int endTerminalX,
			int endTerminalY,
			int connectorID = 0
		)
		{
			// Draw horizontal lines
			DrawHorizontalLineToBuffer(startTerminalY, startTerminalX + 1, endTerminalX - 1, connectorID);
			DrawHorizontalLineToBuffer(endTerminalY, startTerminalX + 1, endTerminalX - 1, connectorID);
			
			// Draw vertical lines
			DrawVerticalLineToBuffer(startTerminalX, startTerminalY + 1, endTerminalY - 1, connectorID);
			DrawVerticalLineToBuffer(endTerminalX, startTerminalY + 1, endTerminalY - 1, connectorID);
			
			// Draw corners
			DrawTopLeftCornerToBuffer(startTerminalX, startTerminalY, connectorID);
			DrawTopRightCornerToBuffer(endTerminalX, startTerminalY, connectorID);
			DrawBottomLeftCornerToBuffer(startTerminalX, endTerminalY, connectorID);
			DrawBottomRightCornerToBuffer(endTerminalX, endTerminalY, connectorID);
		}

		private void DrawConnectedArea(
			int startTerminalX,
			int startTerminalY,
			int endTerminalX,
			int endTerminalY,
			int connectorID
		)
		{
			for (int y = startTerminalY; y <= endTerminalY; y++)
			{
				for (int x = startTerminalX; x <= endTerminalX; x++)
				{
					ref TerminalBufferValue cell = ref terminalBuffer[x, y];
					cell.ConnectorID = connectorID;
				}
			}
		}

		private void DrawBuffer()
		{
			ref TerminalBufferValue testCell = ref terminalBuffer[0, 0];
			
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
			
			// Update the mesh UVs
			_mesh.SetUVs(0, uvs);
			_mesh.UploadMeshData(false);
			
			// Force the canvas renderer to update
			_canvasRenderer.SetMesh(_mesh);
			
			forceRedraw = false;
		}

		private void BuildBuffer()
		{
			// Reset connector ID counter each frame
			nextConnectorID = 1;
			
			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					previousTerminalBuffer[x, y] = terminalBuffer[x, y];
					ref TerminalBufferValue cell = ref terminalBuffer[x, y];
					cell = new TerminalBufferValue(_terminalDefinition);
					cell.Char = ' ';
				}
			}

			foreach (var element in elementPool)
			{
				if (element is Elements.Text text)
				{
					string contents = text.Contents;
					if (string.IsNullOrEmpty(contents))
						continue;

					Vector2Int position = element.GetTerminalPosition(_terminalDefinition);
					RectInt bounds = element.GetTerminalBounds(_terminalDefinition);

					for (int i = 0; i < contents.Length; i++)
					{
						char c = contents[i];
						
						// Bounds check before writing
						if (position.x < 0 || position.x >= terminal.Width ||
							position.y < 0 || position.y >= terminal.Height)
						{
							break;
						}
						
						ref TerminalBufferValue cell = ref terminalBuffer[
							position.x,
							position.y
						];
						cell.Char = c;
						cell.ConnectorID = 0;

						position.x++;

						// Wrap to next line if past the right edge of the element bounds
						if (position.x >= bounds.xMax)
						{
							position.x = bounds.xMin;
							position.y++;
						}
						// Stop if past the bottom edge of the element bounds
						if (position.y >= bounds.yMax)
						{
							break;
						}
					}
				}
				if (element is Elements.HorizontalLine hline)
				{
					RectInt bounds = element.GetTerminalBounds(_terminalDefinition);
					for (int y = bounds.yMin; y < bounds.yMax; y++)
					{
						for (int x = bounds.xMin; x < bounds.xMax; x++)
						{
							if (x < 0 || x >= terminal.Width || y < 0 || y >= terminal.Height)
								continue;
								
							ref TerminalBufferValue cell = ref terminalBuffer[
								x,
								y
							];
							cell.AtlasX = _terminalDefinition.HorizontalLineX;
							cell.AtlasY = _terminalDefinition.HorizontalLineY;
						}
					}
				}
				if (element is ConnectedLinesGroup connectedLinesGroup)
				{
					int connectorID = nextConnectorID++;
					var childLines = connectedLinesGroup.GetChildLines();

					foreach (var line in childLines)
					{
						RectInt bounds = line.GetTerminalBounds(_terminalDefinition);

						for (int y = bounds.yMin; y < bounds.yMax; y++)
						{
							for (int x = bounds.xMin; x < bounds.xMax; x++)
							{
								if (x < 0 || x >= terminal.Width || y < 0 || y >= terminal.Height)
									continue;

								ref TerminalBufferValue cell = ref terminalBuffer[x, y];
								cell.ConnectorID = connectorID;
								// Set initial glyph - will be resolved by ActualizeConnectedLinesToBuffer
								cell.AtlasX = _terminalDefinition.HorizontalLineX;
								cell.AtlasY = _terminalDefinition.HorizontalLineY;
							}
						}
					}
				}
			}

			// Resolve connected line characters based on neighbors
			ActualizeConnectedLinesToBuffer();
		}
		
		private void OnDestroy()
		{
			if (_mesh != null)
			{
				DestroyImmediate(_mesh);
			}
		}
	}
}
