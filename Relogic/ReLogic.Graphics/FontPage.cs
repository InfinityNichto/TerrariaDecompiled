using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ReLogic.Graphics;

internal sealed class FontPage
{
	public readonly Texture2D Texture;

	public readonly List<Rectangle> Glyphs;

	public readonly List<Rectangle> Padding;

	public readonly List<char> Characters;

	public readonly List<Vector3> Kerning;

	public FontPage(Texture2D texture, List<Rectangle> glyphs, List<Rectangle> padding, List<char> characters, List<Vector3> kerning)
	{
		Texture = texture;
		Glyphs = glyphs;
		Padding = padding;
		Characters = characters;
		Kerning = kerning;
	}
}
