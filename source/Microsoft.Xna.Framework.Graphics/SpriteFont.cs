using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Microsoft.Xna.Framework.Graphics;

public sealed class SpriteFont
{
	internal struct StringProxy
	{
		private string textString;

		private StringBuilder textBuilder;

		public readonly int Length;

		public char this[int index]
		{
			get
			{
				if (textString != null)
				{
					return textString[index];
				}
				return textBuilder[index];
			}
		}

		public StringProxy(string text)
		{
			textString = text;
			textBuilder = null;
			Length = text.Length;
		}

		public StringProxy(StringBuilder text)
		{
			textBuilder = text;
			textString = null;
			Length = text.Length;
		}
	}

	private Texture2D textureValue;

	private List<Rectangle> glyphData;

	private List<Rectangle> croppingData;

	private List<char> characterMap;

	private List<Vector3> kerning;

	private int lineSpacing;

	private float spacing;

	private char? defaultCharacter;

	private ReadOnlyCollection<char> characters;

	public int LineSpacing
	{
		get
		{
			return lineSpacing;
		}
		set
		{
			lineSpacing = value;
		}
	}

	public float Spacing
	{
		get
		{
			return spacing;
		}
		set
		{
			spacing = value;
		}
	}

	public char? DefaultCharacter
	{
		get
		{
			return defaultCharacter;
		}
		set
		{
			if (value.HasValue && !characterMap.Contains(value.Value))
			{
				throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, FrameworkResources.CharacterNotInFont, new object[2]
				{
					value.Value,
					(int)value.Value
				}));
			}
			defaultCharacter = value;
		}
	}

	public ReadOnlyCollection<char> Characters
	{
		get
		{
			if (characters == null)
			{
				characters = new ReadOnlyCollection<char>(characterMap);
			}
			return characters;
		}
	}

	internal SpriteFont(Texture2D texture, List<Rectangle> glyphs, List<Rectangle> cropping, List<char> charMap, int lineSpacing, float spacing, List<Vector3> kerning, char? defaultCharacter)
	{
		textureValue = texture;
		glyphData = glyphs;
		croppingData = cropping;
		characterMap = charMap;
		this.lineSpacing = lineSpacing;
		this.spacing = spacing;
		this.kerning = kerning;
		this.defaultCharacter = defaultCharacter;
	}

	public Vector2 MeasureString(string text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		StringProxy text2 = new StringProxy(text);
		return InternalMeasure(ref text2);
	}

	public Vector2 MeasureString(StringBuilder text)
	{
		if (text == null)
		{
			throw new ArgumentNullException("text");
		}
		StringProxy text2 = new StringProxy(text);
		return InternalMeasure(ref text2);
	}

	private Vector2 InternalMeasure(ref StringProxy text)
	{
		if (text.Length == 0)
		{
			return Vector2.Zero;
		}
		Vector2 zero = Vector2.Zero;
		zero.Y = lineSpacing;
		float val = 0f;
		int num = 0;
		float num2 = 0f;
		bool flag = true;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\r')
			{
				continue;
			}
			if (text[i] == '\n')
			{
				zero.X += Math.Max(num2, 0f);
				num2 = 0f;
				val = Math.Max(zero.X, val);
				zero = Vector2.Zero;
				zero.Y = lineSpacing;
				flag = true;
				num++;
				continue;
			}
			Vector3 vector = kerning[GetIndexForCharacter(text[i])];
			if (flag)
			{
				vector.X = Math.Max(vector.X, 0f);
			}
			else
			{
				zero.X += spacing + num2;
			}
			zero.X += vector.X + vector.Y;
			num2 = vector.Z;
			Rectangle rectangle = croppingData[GetIndexForCharacter(text[i])];
			zero.Y = Math.Max(zero.Y, rectangle.Height);
			flag = false;
		}
		zero.X += Math.Max(num2, 0f);
		zero.Y += num * lineSpacing;
		zero.X = Math.Max(zero.X, val);
		return zero;
	}

	internal void InternalDraw(ref StringProxy text, SpriteBatch spriteBatch, Vector2 textblockPosition, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects spriteEffects, float depth)
	{
		Matrix.CreateRotationZ(rotation, out var result);
		Matrix.CreateTranslation((0f - origin.X) * scale.X, (0f - origin.Y) * scale.Y, 0f, out var result2);
		Matrix.Multiply(ref result2, ref result, out result);
		int num = 1;
		float x = 0f;
		Vector2 vector = default(Vector2);
		bool flag = true;
		if ((spriteEffects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally)
		{
			x = InternalMeasure(ref text).X * scale.X;
			num = -1;
		}
		if ((spriteEffects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically)
		{
			vector.Y = (InternalMeasure(ref text).Y - (float)lineSpacing) * scale.Y;
		}
		else
		{
			vector.Y = 0f;
		}
		vector.X = x;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			switch (c)
			{
			case '\n':
				flag = true;
				vector.X = x;
				if ((spriteEffects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically)
				{
					vector.Y -= (float)lineSpacing * scale.Y;
				}
				else
				{
					vector.Y += (float)lineSpacing * scale.Y;
				}
				continue;
			case '\r':
				continue;
			}
			int indexForCharacter = GetIndexForCharacter(c);
			Vector3 vector2 = kerning[indexForCharacter];
			if (flag)
			{
				vector2.X = Math.Max(vector2.X, 0f);
			}
			else
			{
				vector.X += spacing * scale.X * (float)num;
			}
			vector.X += vector2.X * scale.X * (float)num;
			Rectangle value = glyphData[indexForCharacter];
			Rectangle rectangle = croppingData[indexForCharacter];
			if ((spriteEffects & SpriteEffects.FlipVertically) == SpriteEffects.FlipVertically)
			{
				rectangle.Y = lineSpacing - value.Height - rectangle.Y;
			}
			if ((spriteEffects & SpriteEffects.FlipHorizontally) == SpriteEffects.FlipHorizontally)
			{
				rectangle.X -= rectangle.Width;
			}
			Vector2 position = vector;
			position.X += (float)rectangle.X * scale.X;
			position.Y += (float)rectangle.Y * scale.Y;
			Vector2.Transform(ref position, ref result, out position);
			position += textblockPosition;
			spriteBatch.Draw(textureValue, position, value, color, rotation, Vector2.Zero, scale, spriteEffects, depth);
			flag = false;
			vector.X += (vector2.Y + vector2.Z) * scale.X * (float)num;
		}
	}

	private int GetIndexForCharacter(char character)
	{
		int num = 0;
		int num2 = characterMap.Count - 1;
		while (num <= num2)
		{
			int num3 = num + (num2 - num >> 1);
			char c = characterMap[num3];
			if (c == character)
			{
				return num3;
			}
			if (c < character)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		if (defaultCharacter.HasValue)
		{
			char value = defaultCharacter.Value;
			if (character != value)
			{
				return GetIndexForCharacter(value);
			}
		}
		string message = string.Format(CultureInfo.CurrentCulture, FrameworkResources.CharacterNotInFont, new object[2]
		{
			character,
			(int)character
		});
		throw new ArgumentException(message, "character");
	}
}
