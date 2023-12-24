using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ReLogic.Graphics;

public class DynamicSpriteFontReader : ContentTypeReader<DynamicSpriteFont>
{
	protected override DynamicSpriteFont Read(ContentReader input, DynamicSpriteFont existingInstance)
	{
		float spacing = ((BinaryReader)(object)input).ReadSingle();
		int lineSpacing = ((BinaryReader)(object)input).ReadInt32();
		char defaultCharacter = ((BinaryReader)(object)input).ReadChar();
		DynamicSpriteFont dynamicSpriteFont = new DynamicSpriteFont(spacing, lineSpacing, defaultCharacter);
		int num = ((BinaryReader)(object)input).ReadInt32();
		FontPage[] array = new FontPage[num];
		for (int i = 0; i < num; i++)
		{
			Texture2D texture = input.ReadObject<Texture2D>();
			List<Rectangle> glyphs = input.ReadObject<List<Rectangle>>();
			List<Rectangle> padding = input.ReadObject<List<Rectangle>>();
			List<char> characters = input.ReadObject<List<char>>();
			List<Vector3> kerning = input.ReadObject<List<Vector3>>();
			array[i] = new FontPage(texture, glyphs, padding, characters, kerning);
		}
		dynamicSpriteFont.SetPages(array);
		return dynamicSpriteFont;
	}
}
