using System;
using System.ComponentModel;
using System.Globalization;
using Microsoft.Xna.Framework.Design;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Microsoft.Xna.Framework;

[Serializable]
[TypeConverter(typeof(ColorConverter))]
public struct Color : IPackedVector<uint>, IPackedVector, IEquatable<Color>
{
	private uint packedValue;

	public byte R
	{
		get
		{
			return (byte)packedValue;
		}
		set
		{
			packedValue = (packedValue & 0xFFFFFF00u) | value;
		}
	}

	public byte G
	{
		get
		{
			return (byte)(packedValue >> 8);
		}
		set
		{
			packedValue = (packedValue & 0xFFFF00FFu) | (uint)(value << 8);
		}
	}

	public byte B
	{
		get
		{
			return (byte)(packedValue >> 16);
		}
		set
		{
			packedValue = (packedValue & 0xFF00FFFFu) | (uint)(value << 16);
		}
	}

	public byte A
	{
		get
		{
			return (byte)(packedValue >> 24);
		}
		set
		{
			packedValue = (packedValue & 0xFFFFFFu) | (uint)(value << 24);
		}
	}

	[CLSCompliant(false)]
	public uint PackedValue
	{
		get
		{
			return packedValue;
		}
		set
		{
			packedValue = value;
		}
	}

	public static Color Transparent => new Color(0u);

	public static Color AliceBlue => new Color(4294965488u);

	public static Color AntiqueWhite => new Color(4292340730u);

	public static Color Aqua => new Color(4294967040u);

	public static Color Aquamarine => new Color(4292149119u);

	public static Color Azure => new Color(4294967280u);

	public static Color Beige => new Color(4292670965u);

	public static Color Bisque => new Color(4291093759u);

	public static Color Black => new Color(4278190080u);

	public static Color BlanchedAlmond => new Color(4291685375u);

	public static Color Blue => new Color(4294901760u);

	public static Color BlueViolet => new Color(4293012362u);

	public static Color Brown => new Color(4280953509u);

	public static Color BurlyWood => new Color(4287084766u);

	public static Color CadetBlue => new Color(4288716383u);

	public static Color Chartreuse => new Color(4278255487u);

	public static Color Chocolate => new Color(4280183250u);

	public static Color Coral => new Color(4283465727u);

	public static Color CornflowerBlue => new Color(4293760356u);

	public static Color Cornsilk => new Color(4292671743u);

	public static Color Crimson => new Color(4282127580u);

	public static Color Cyan => new Color(4294967040u);

	public static Color DarkBlue => new Color(4287299584u);

	public static Color DarkCyan => new Color(4287335168u);

	public static Color DarkGoldenrod => new Color(4278945464u);

	public static Color DarkGray => new Color(4289309097u);

	public static Color DarkGreen => new Color(4278215680u);

	public static Color DarkKhaki => new Color(4285249469u);

	public static Color DarkMagenta => new Color(4287299723u);

	public static Color DarkOliveGreen => new Color(4281297749u);

	public static Color DarkOrange => new Color(4278226175u);

	public static Color DarkOrchid => new Color(4291572377u);

	public static Color DarkRed => new Color(4278190219u);

	public static Color DarkSalmon => new Color(4286224105u);

	public static Color DarkSeaGreen => new Color(4287347855u);

	public static Color DarkSlateBlue => new Color(4287315272u);

	public static Color DarkSlateGray => new Color(4283387695u);

	public static Color DarkTurquoise => new Color(4291939840u);

	public static Color DarkViolet => new Color(4292018324u);

	public static Color DeepPink => new Color(4287829247u);

	public static Color DeepSkyBlue => new Color(4294950656u);

	public static Color DimGray => new Color(4285098345u);

	public static Color DodgerBlue => new Color(4294938654u);

	public static Color Firebrick => new Color(4280427186u);

	public static Color FloralWhite => new Color(4293982975u);

	public static Color ForestGreen => new Color(4280453922u);

	public static Color Fuchsia => new Color(4294902015u);

	public static Color Gainsboro => new Color(4292664540u);

	public static Color GhostWhite => new Color(4294965496u);

	public static Color Gold => new Color(4278245375u);

	public static Color Goldenrod => new Color(4280329690u);

	public static Color Gray => new Color(4286611584u);

	public static Color Green => new Color(4278222848u);

	public static Color GreenYellow => new Color(4281335725u);

	public static Color Honeydew => new Color(4293984240u);

	public static Color HotPink => new Color(4290013695u);

	public static Color IndianRed => new Color(4284243149u);

	public static Color Indigo => new Color(4286709835u);

	public static Color Ivory => new Color(4293984255u);

	public static Color Khaki => new Color(4287424240u);

	public static Color Lavender => new Color(4294633190u);

	public static Color LavenderBlush => new Color(4294308095u);

	public static Color LawnGreen => new Color(4278254716u);

	public static Color LemonChiffon => new Color(4291689215u);

	public static Color LightBlue => new Color(4293318829u);

	public static Color LightCoral => new Color(4286611696u);

	public static Color LightCyan => new Color(4294967264u);

	public static Color LightGoldenrodYellow => new Color(4292016890u);

	public static Color LightGreen => new Color(4287688336u);

	public static Color LightGray => new Color(4292072403u);

	public static Color LightPink => new Color(4290885375u);

	public static Color LightSalmon => new Color(4286226687u);

	public static Color LightSeaGreen => new Color(4289376800u);

	public static Color LightSkyBlue => new Color(4294626951u);

	public static Color LightSlateGray => new Color(4288252023u);

	public static Color LightSteelBlue => new Color(4292789424u);

	public static Color LightYellow => new Color(4292935679u);

	public static Color Lime => new Color(4278255360u);

	public static Color LimeGreen => new Color(4281519410u);

	public static Color Linen => new Color(4293325050u);

	public static Color Magenta => new Color(4294902015u);

	public static Color Maroon => new Color(4278190208u);

	public static Color MediumAquamarine => new Color(4289383782u);

	public static Color MediumBlue => new Color(4291624960u);

	public static Color MediumOrchid => new Color(4292040122u);

	public static Color MediumPurple => new Color(4292571283u);

	public static Color MediumSeaGreen => new Color(4285641532u);

	public static Color MediumSlateBlue => new Color(4293814395u);

	public static Color MediumSpringGreen => new Color(4288346624u);

	public static Color MediumTurquoise => new Color(4291613000u);

	public static Color MediumVioletRed => new Color(4286911943u);

	public static Color MidnightBlue => new Color(4285536537u);

	public static Color MintCream => new Color(4294639605u);

	public static Color MistyRose => new Color(4292994303u);

	public static Color Moccasin => new Color(4290110719u);

	public static Color NavajoWhite => new Color(4289584895u);

	public static Color Navy => new Color(4286578688u);

	public static Color OldLace => new Color(4293326333u);

	public static Color Olive => new Color(4278222976u);

	public static Color OliveDrab => new Color(4280520299u);

	public static Color Orange => new Color(4278232575u);

	public static Color OrangeRed => new Color(4278207999u);

	public static Color Orchid => new Color(4292243674u);

	public static Color PaleGoldenrod => new Color(4289390830u);

	public static Color PaleGreen => new Color(4288215960u);

	public static Color PaleTurquoise => new Color(4293848751u);

	public static Color PaleVioletRed => new Color(4287852763u);

	public static Color PapayaWhip => new Color(4292210687u);

	public static Color PeachPuff => new Color(4290370303u);

	public static Color Peru => new Color(4282353101u);

	public static Color Pink => new Color(4291543295u);

	public static Color Plum => new Color(4292714717u);

	public static Color PowderBlue => new Color(4293320880u);

	public static Color Purple => new Color(4286578816u);

	public static Color Red => new Color(4278190335u);

	public static Color RosyBrown => new Color(4287598524u);

	public static Color RoyalBlue => new Color(4292962625u);

	public static Color SaddleBrown => new Color(4279453067u);

	public static Color Salmon => new Color(4285694202u);

	public static Color SandyBrown => new Color(4284523764u);

	public static Color SeaGreen => new Color(4283927342u);

	public static Color SeaShell => new Color(4293850623u);

	public static Color Sienna => new Color(4281160352u);

	public static Color Silver => new Color(4290822336u);

	public static Color SkyBlue => new Color(4293643911u);

	public static Color SlateBlue => new Color(4291648106u);

	public static Color SlateGray => new Color(4287660144u);

	public static Color Snow => new Color(4294638335u);

	public static Color SpringGreen => new Color(4286578432u);

	public static Color SteelBlue => new Color(4290019910u);

	public static Color Tan => new Color(4287411410u);

	public static Color Teal => new Color(4286611456u);

	public static Color Thistle => new Color(4292394968u);

	public static Color Tomato => new Color(4282868735u);

	public static Color Turquoise => new Color(4291878976u);

	public static Color Violet => new Color(4293821166u);

	public static Color Wheat => new Color(4289978101u);

	public static Color White => new Color(uint.MaxValue);

	public static Color WhiteSmoke => new Color(4294309365u);

	public static Color Yellow => new Color(4278255615u);

	public static Color YellowGreen => new Color(4281519514u);

	private Color(uint packedValue)
	{
		this.packedValue = packedValue;
	}

	public Color(int r, int g, int b)
	{
		if (((uint)(r | g | b) & 0xFFFFFF00u) != 0)
		{
			r = ClampToByte64(r);
			g = ClampToByte64(g);
			b = ClampToByte64(b);
		}
		g <<= 8;
		b <<= 16;
		packedValue = (uint)(r | g | b) | 0xFF000000u;
	}

	public Color(int r, int g, int b, int a)
	{
		if (((uint)(r | g | b | a) & 0xFFFFFF00u) != 0)
		{
			r = ClampToByte32(r);
			g = ClampToByte32(g);
			b = ClampToByte32(b);
			a = ClampToByte32(a);
		}
		g <<= 8;
		b <<= 16;
		a <<= 24;
		packedValue = (uint)(r | g | b | a);
	}

	public Color(float r, float g, float b)
	{
		packedValue = PackHelper(r, g, b, 1f);
	}

	public Color(float r, float g, float b, float a)
	{
		packedValue = PackHelper(r, g, b, a);
	}

	public Color(Vector3 vector)
	{
		packedValue = PackHelper(vector.X, vector.Y, vector.Z, 1f);
	}

	public Color(Vector4 vector)
	{
		packedValue = PackHelper(vector.X, vector.Y, vector.Z, vector.W);
	}

	void IPackedVector.PackFromVector4(Vector4 vector)
	{
		packedValue = PackHelper(vector.X, vector.Y, vector.Z, vector.W);
	}

	public static Color FromNonPremultiplied(Vector4 vector)
	{
		Color result = default(Color);
		result.packedValue = PackHelper(vector.X * vector.W, vector.Y * vector.W, vector.Z * vector.W, vector.W);
		return result;
	}

	public static Color FromNonPremultiplied(int r, int g, int b, int a)
	{
		r = ClampToByte64((long)r * (long)a / 255);
		g = ClampToByte64((long)g * (long)a / 255);
		b = ClampToByte64((long)b * (long)a / 255);
		a = ClampToByte32(a);
		g <<= 8;
		b <<= 16;
		a <<= 24;
		Color result = default(Color);
		result.packedValue = (uint)(r | g | b | a);
		return result;
	}

	private static uint PackHelper(float vectorX, float vectorY, float vectorZ, float vectorW)
	{
		uint num = PackUtils.PackUNorm(255f, vectorX);
		uint num2 = PackUtils.PackUNorm(255f, vectorY) << 8;
		uint num3 = PackUtils.PackUNorm(255f, vectorZ) << 16;
		uint num4 = PackUtils.PackUNorm(255f, vectorW) << 24;
		return num | num2 | num3 | num4;
	}

	private static int ClampToByte32(int value)
	{
		if (value < 0)
		{
			return 0;
		}
		if (value > 255)
		{
			return 255;
		}
		return value;
	}

	private static int ClampToByte64(long value)
	{
		if (value < 0)
		{
			return 0;
		}
		if (value > 255)
		{
			return 255;
		}
		return (int)value;
	}

	public Vector3 ToVector3()
	{
		Vector3 result = default(Vector3);
		result.X = PackUtils.UnpackUNorm(255u, packedValue);
		result.Y = PackUtils.UnpackUNorm(255u, packedValue >> 8);
		result.Z = PackUtils.UnpackUNorm(255u, packedValue >> 16);
		return result;
	}

	public Vector4 ToVector4()
	{
		Vector4 result = default(Vector4);
		result.X = PackUtils.UnpackUNorm(255u, packedValue);
		result.Y = PackUtils.UnpackUNorm(255u, packedValue >> 8);
		result.Z = PackUtils.UnpackUNorm(255u, packedValue >> 16);
		result.W = PackUtils.UnpackUNorm(255u, packedValue >> 24);
		return result;
	}

	public static Color Lerp(Color value1, Color value2, float amount)
	{
		uint num = value1.packedValue;
		uint num2 = value2.packedValue;
		int num3 = (byte)num;
		int num4 = (byte)(num >> 8);
		int num5 = (byte)(num >> 16);
		int num6 = (byte)(num >> 24);
		int num7 = (byte)num2;
		int num8 = (byte)(num2 >> 8);
		int num9 = (byte)(num2 >> 16);
		int num10 = (byte)(num2 >> 24);
		int num11 = (int)PackUtils.PackUNorm(65536f, amount);
		int num12 = num3 + ((num7 - num3) * num11 >> 16);
		int num13 = num4 + ((num8 - num4) * num11 >> 16);
		int num14 = num5 + ((num9 - num5) * num11 >> 16);
		int num15 = num6 + ((num10 - num6) * num11 >> 16);
		Color result = default(Color);
		result.packedValue = (uint)(num12 | (num13 << 8) | (num14 << 16) | (num15 << 24));
		return result;
	}

	public static Color Multiply(Color value, float scale)
	{
		uint num = value.packedValue;
		uint num2 = (byte)num;
		uint num3 = (byte)(num >> 8);
		uint num4 = (byte)(num >> 16);
		uint num5 = (byte)(num >> 24);
		scale *= 65536f;
		uint num6 = ((!(scale < 0f)) ? ((!(scale > 16777215f)) ? ((uint)scale) : 16777215u) : 0u);
		num2 = num2 * num6 >> 16;
		num3 = num3 * num6 >> 16;
		num4 = num4 * num6 >> 16;
		num5 = num5 * num6 >> 16;
		if (num2 > 255)
		{
			num2 = 255u;
		}
		if (num3 > 255)
		{
			num3 = 255u;
		}
		if (num4 > 255)
		{
			num4 = 255u;
		}
		if (num5 > 255)
		{
			num5 = 255u;
		}
		Color result = default(Color);
		result.packedValue = num2 | (num3 << 8) | (num4 << 16) | (num5 << 24);
		return result;
	}

	public static Color operator *(Color value, float scale)
	{
		uint num = value.packedValue;
		uint num2 = (byte)num;
		uint num3 = (byte)(num >> 8);
		uint num4 = (byte)(num >> 16);
		uint num5 = (byte)(num >> 24);
		scale *= 65536f;
		uint num6 = ((!(scale < 0f)) ? ((!(scale > 16777215f)) ? ((uint)scale) : 16777215u) : 0u);
		num2 = num2 * num6 >> 16;
		num3 = num3 * num6 >> 16;
		num4 = num4 * num6 >> 16;
		num5 = num5 * num6 >> 16;
		if (num2 > 255)
		{
			num2 = 255u;
		}
		if (num3 > 255)
		{
			num3 = 255u;
		}
		if (num4 > 255)
		{
			num4 = 255u;
		}
		if (num5 > 255)
		{
			num5 = 255u;
		}
		Color result = default(Color);
		result.packedValue = num2 | (num3 << 8) | (num4 << 16) | (num5 << 24);
		return result;
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.CurrentCulture, "{{R:{0} G:{1} B:{2} A:{3}}}", R, G, B, A);
	}

	public override int GetHashCode()
	{
		return packedValue.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is Color)
		{
			return Equals((Color)obj);
		}
		return false;
	}

	public bool Equals(Color other)
	{
		return packedValue.Equals(other.packedValue);
	}

	public static bool operator ==(Color a, Color b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(Color a, Color b)
	{
		return !a.Equals(b);
	}
}
