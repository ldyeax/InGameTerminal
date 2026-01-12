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
		private struct TerminalBufferValue
		{
			private ITerminalDefinition _terminalDefinition;
			public TerminalBufferValue(ITerminalDefinition terminalDefinition)
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
			public readonly char GetChar(ITerminalDefinition terminalDefinition)
			{
				return terminalDefinition.XYToChar(AtlasX, AtlasY);
			}
			public void SetChar(ITerminalDefinition terminalDefinition, char c)
			{
				Vector2Int charXY = terminalDefinition.CharToXY(c);
				AtlasX = charXY.x;
				AtlasY = charXY.y;
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

		enum TerminalCommandType
		{
			Char = 0,
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
		List<TerminalCommand> terminalCommands = new List<TerminalCommand>();
		private void BuildTerminalCommands()
		{
			terminalCommands.Clear();
			terminalCommands.Add(new TerminalCommand()
			{
				CommandType = TerminalCommandType.HomeCursor
			});
			if (firstUpdate)
			{
				terminalCommands.Add(new TerminalCommand()
				{
					CommandType = TerminalCommandType.EraseInDisplay
				});
				return;
			}

			Vector2Int cursorPosition = default;
			
			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					var cell = terminalBuffer[x, y];
					var previousCell = previousTerminalBuffer[x, y];
					if (cell != previousCell)
					{
						Debug.Log($"Cell changed at {x},{y} from '{previousCell.GetChar(_terminalDefinition)}' to '{cell.GetChar(_terminalDefinition)}'");
						if (cursorPosition.x != x || cursorPosition.y != y)
						{
							terminalCommands.Add(new TerminalCommand()
							{
								CommandType = TerminalCommandType.MoveTo,
								X = x,
								Y = y
							});
						}
						terminalCommands.Add(new TerminalCommand()
						{
							Char = cell.GetChar(_terminalDefinition),
							X = x,
							Y = y
						});
						cursorPosition.x++;
						if (cursorPosition.x > terminal.Width)
						{
							cursorPosition.x = 0;
							cursorPosition.y++;
						}
						if (cursorPosition.y > terminal.Height)
						{
							cursorPosition.y = 0;
						}
					}
				}
			}
		}

		
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

		#region Draw x to buffer
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
			int connectorID = -1
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

		private void DrawConnectedAreaToBuffer(
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
		#endregion Draw x to buffer

		private void DrawBuffer()
		{
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
			
			forceRedraw = false;
		}
		private void DrawTerminalCommands()
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
		private int nextConnectorID = 1;
		private void BuildBufferFromChildren(RectTransform rectTransform, TerminalBufferValue currentState)
		{
			int childCount = rectTransform.childCount;
			for (int i_outer = 0; i_outer < childCount; i_outer++)
			{
				var child = rectTransform.GetChild(i_outer);
				if (!child.gameObject.activeInHierarchy)
				{
					continue;
				}
				var element = child.GetComponent<Element>();
				Vector2Int position = element.GetTerminalPosition(_terminalDefinition);
				RectInt bounds = element.GetTerminalBounds(_terminalDefinition);

				if (bounds.x < 0 || bounds.xMax > terminal.Width || bounds.y < 0 || bounds.yMax > terminal.Height)
				{
					continue;
				}

				if (element is Elements.Text text)
				{
					string contents = text.Contents;
					if (string.IsNullOrEmpty(contents))
						continue;

					for (int i = 0; i < contents.Length; i++)
					{
						char c = contents[i];

						// Bounds check before writing
						if (position.x < 0 || position.x >= terminal.Width ||
							position.y < 0 || position.y >= terminal.Height)
						{
							Debug.Log("Bounds check");
							goto endText;
						}

						ref TerminalBufferValue cell = ref terminalBuffer[
							position.x,
							position.y
						];
						cell.SetChar(_terminalDefinition, c);
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
							Debug.Log("Bounds check 2");
							goto endText;
						}
					}
				endText:
					BuildBufferFromChildren(text.RectTransform, currentState);
					continue;
				}
				if (element is Elements.HorizontalLine hline)
				{
					for (int y = bounds.yMin; y < bounds.yMax; y++)
					{
						for (int x = bounds.xMin; x < bounds.xMax; x++)
						{
							if (x < 0 || x >= terminal.Width || y < 0 || y >= terminal.Height)
								goto endHorizontalLine;

							ref TerminalBufferValue cell = ref terminalBuffer[
								x,
								y
							];
							cell.AtlasX = _terminalDefinition.HorizontalLineX;
							cell.AtlasY = _terminalDefinition.HorizontalLineY;
						}
					}
				endHorizontalLine:
					BuildBufferFromChildren(hline.RectTransform, currentState);
					continue;
				}
				if (element is ConnectedLinesGroup connectedLinesGroup)
				{
					currentState.ConnectorID = nextConnectorID++;
					BuildBufferFromChildren(connectedLinesGroup.RectTransform, currentState);
					continue;
				}
				if (element is ConnectedLine line)
				{
					for (int y = bounds.yMin; y < bounds.yMax; y++)
					{
						for (int x = bounds.xMin; x < bounds.xMax; x++)
						{
							if (x < 0 || x >= terminal.Width || y < 0 || y >= terminal.Height)
								goto endConnectedLine;

							ref TerminalBufferValue cell = ref terminalBuffer[x, y];
							cell.ConnectorID = currentState.ConnectorID;
							// Set initial glyph - will be resolved by ActualizeConnectedLinesToBuffer
							cell.AtlasX = _terminalDefinition.HorizontalLineX;
							cell.AtlasY = _terminalDefinition.HorizontalLineY;
						}
					}

				endConnectedLine:
					BuildBufferFromChildren(line.RectTransform, currentState);
					continue;
				}
				if (element is Elements.Box box)
				{
					DrawBoxToBuffer(
						bounds.xMin,
						bounds.yMin,
						bounds.xMax - 1,
						bounds.yMax - 1
					);
					BuildBufferFromChildren(box.RectTransform, currentState);
					continue;
				}
			}
		}

		private void SwapAndClearBuffer()
		{
			(terminalBuffer, previousTerminalBuffer) = (previousTerminalBuffer, terminalBuffer);
			for (int y = 0; y < terminal.Height; y++)
			{
				for (int x = 0; x < terminal.Width; x++)
				{
					terminalBuffer[x, y] = default;
				}
			}
		}
		private void BuildBuffer()
		{
			if (firstUpdate)
			{
				for (int y = 0; y < terminal.Height; y++)
				{
					for (int x = 0; x < terminal.Width; x++)
					{
						ref TerminalBufferValue previousChar = ref previousTerminalBuffer[x, y];
						ref TerminalBufferValue currentChar = ref terminalBuffer[x, y];
						previousChar = default;
						previousChar.SetChar(_terminalDefinition, ' ');
						currentChar = default;
						currentChar.SetChar(_terminalDefinition, ' ');
					}
				}
				return;
			}
			SwapAndClearBuffer();

			TerminalBufferValue currentState = default;

			BuildBufferFromChildren(
				_rectTransform,
				currentState
			);

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

		private RectTransform _rectTransform = null;
		bool firstUpdate = true;
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
			if (!_rectTransform)
			{
				_rectTransform = terminal.RectTransform;
			}
			if (!_rectTransform)
			{
				Debug.Log($"Terminal is missing a RectTransform", this);
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
			if (terminalBuffer == null)
			{
				terminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				previousTerminalBuffer = new TerminalBufferValue[terminal.Width, terminal.Height];
				InitMesh();
			}

			BuildBuffer();
			Debug.Log($"PreviousBuffer at y=0 x=1: '{previousTerminalBuffer[1,0].GetChar(_terminalDefinition)}'", this);
			Debug.Log($"Current Buffer at y=0 x=1: '{terminalBuffer[1,0].GetChar(_terminalDefinition)}'", this);
			BuildTerminalCommands();
			StringBuilder terminalCommandsDebug = new();
			terminalCommandsDebug.Append("Terminal commands: [");
			foreach (var cmd in terminalCommands)
			{
				terminalCommandsDebug.Append($"{cmd.CommandType} {cmd.X} {cmd.Y}, ");
			}
			terminalCommandsDebug.Append("]");
			Debug.Log(terminalCommandsDebug, this);
			DrawTerminalCommands();
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
