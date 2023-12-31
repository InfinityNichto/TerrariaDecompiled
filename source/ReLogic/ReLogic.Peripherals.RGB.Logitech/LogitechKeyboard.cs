using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ReLogic.Peripherals.RGB.Logitech;

internal class LogitechKeyboard : RgbKeyboard
{
	private readonly byte[] _colors;

	private readonly KeyName[] _excludedKeys = new KeyName[126];

	private static readonly Dictionary<Keys, KeyName> XnaToLogitechKeys = new Dictionary<Keys, KeyName>
	{
		{
			(Keys)27,
			KeyName.ESC
		},
		{
			(Keys)112,
			KeyName.F1
		},
		{
			(Keys)113,
			KeyName.F2
		},
		{
			(Keys)114,
			KeyName.F3
		},
		{
			(Keys)115,
			KeyName.F4
		},
		{
			(Keys)116,
			KeyName.F5
		},
		{
			(Keys)117,
			KeyName.F6
		},
		{
			(Keys)118,
			KeyName.F7
		},
		{
			(Keys)119,
			KeyName.F8
		},
		{
			(Keys)120,
			KeyName.F9
		},
		{
			(Keys)121,
			KeyName.F10
		},
		{
			(Keys)122,
			KeyName.F11
		},
		{
			(Keys)123,
			KeyName.F12
		},
		{
			(Keys)44,
			KeyName.PRINT_SCREEN
		},
		{
			(Keys)145,
			KeyName.SCROLL_LOCK
		},
		{
			(Keys)19,
			KeyName.PAUSE_BREAK
		},
		{
			(Keys)192,
			KeyName.TILDE
		},
		{
			(Keys)49,
			KeyName.ONE
		},
		{
			(Keys)50,
			KeyName.TWO
		},
		{
			(Keys)51,
			KeyName.THREE
		},
		{
			(Keys)52,
			KeyName.FOUR
		},
		{
			(Keys)53,
			KeyName.FIVE
		},
		{
			(Keys)54,
			KeyName.SIX
		},
		{
			(Keys)55,
			KeyName.SEVEN
		},
		{
			(Keys)56,
			KeyName.EIGHT
		},
		{
			(Keys)57,
			KeyName.NINE
		},
		{
			(Keys)48,
			KeyName.ZERO
		},
		{
			(Keys)189,
			KeyName.MINUS
		},
		{
			(Keys)187,
			KeyName.EQUALS
		},
		{
			(Keys)8,
			KeyName.BACKSPACE
		},
		{
			(Keys)45,
			KeyName.INSERT
		},
		{
			(Keys)36,
			KeyName.HOME
		},
		{
			(Keys)33,
			KeyName.PAGE_UP
		},
		{
			(Keys)144,
			KeyName.NUM_LOCK
		},
		{
			(Keys)111,
			KeyName.NUM_SLASH
		},
		{
			(Keys)106,
			KeyName.NUM_ASTERISK
		},
		{
			(Keys)109,
			KeyName.NUM_MINUS
		},
		{
			(Keys)9,
			KeyName.TAB
		},
		{
			(Keys)81,
			KeyName.Q
		},
		{
			(Keys)87,
			KeyName.W
		},
		{
			(Keys)69,
			KeyName.E
		},
		{
			(Keys)82,
			KeyName.R
		},
		{
			(Keys)84,
			KeyName.T
		},
		{
			(Keys)89,
			KeyName.Y
		},
		{
			(Keys)85,
			KeyName.U
		},
		{
			(Keys)73,
			KeyName.I
		},
		{
			(Keys)79,
			KeyName.O
		},
		{
			(Keys)80,
			KeyName.P
		},
		{
			(Keys)219,
			KeyName.OPEN_BRACKET
		},
		{
			(Keys)221,
			KeyName.CLOSE_BRACKET
		},
		{
			(Keys)226,
			KeyName.BACKSLASH
		},
		{
			(Keys)46,
			KeyName.KEYBOARD_DELETE
		},
		{
			(Keys)35,
			KeyName.END
		},
		{
			(Keys)34,
			KeyName.PAGE_DOWN
		},
		{
			(Keys)103,
			KeyName.NUM_SEVEN
		},
		{
			(Keys)104,
			KeyName.NUM_EIGHT
		},
		{
			(Keys)105,
			KeyName.NUM_NINE
		},
		{
			(Keys)107,
			KeyName.NUM_PLUS
		},
		{
			(Keys)20,
			KeyName.CAPS_LOCK
		},
		{
			(Keys)65,
			KeyName.A
		},
		{
			(Keys)83,
			KeyName.S
		},
		{
			(Keys)68,
			KeyName.D
		},
		{
			(Keys)70,
			KeyName.F
		},
		{
			(Keys)71,
			KeyName.G
		},
		{
			(Keys)72,
			KeyName.H
		},
		{
			(Keys)74,
			KeyName.J
		},
		{
			(Keys)75,
			KeyName.K
		},
		{
			(Keys)76,
			KeyName.L
		},
		{
			(Keys)186,
			KeyName.SEMICOLON
		},
		{
			(Keys)222,
			KeyName.APOSTROPHE
		},
		{
			(Keys)13,
			KeyName.ENTER
		},
		{
			(Keys)100,
			KeyName.NUM_FOUR
		},
		{
			(Keys)101,
			KeyName.NUM_FIVE
		},
		{
			(Keys)102,
			KeyName.NUM_SIX
		},
		{
			(Keys)160,
			KeyName.LEFT_SHIFT
		},
		{
			(Keys)90,
			KeyName.Z
		},
		{
			(Keys)88,
			KeyName.X
		},
		{
			(Keys)67,
			KeyName.C
		},
		{
			(Keys)86,
			KeyName.V
		},
		{
			(Keys)66,
			KeyName.B
		},
		{
			(Keys)78,
			KeyName.N
		},
		{
			(Keys)77,
			KeyName.M
		},
		{
			(Keys)188,
			KeyName.COMMA
		},
		{
			(Keys)190,
			KeyName.PERIOD
		},
		{
			(Keys)191,
			KeyName.FORWARD_SLASH
		},
		{
			(Keys)161,
			KeyName.RIGHT_SHIFT
		},
		{
			(Keys)38,
			KeyName.ARROW_UP
		},
		{
			(Keys)97,
			KeyName.NUM_ONE
		},
		{
			(Keys)98,
			KeyName.NUM_TWO
		},
		{
			(Keys)99,
			KeyName.NUM_THREE
		},
		{
			(Keys)162,
			KeyName.LEFT_CONTROL
		},
		{
			(Keys)91,
			KeyName.LEFT_WINDOWS
		},
		{
			(Keys)164,
			KeyName.LEFT_ALT
		},
		{
			(Keys)32,
			KeyName.SPACE
		},
		{
			(Keys)165,
			KeyName.RIGHT_ALT
		},
		{
			(Keys)92,
			KeyName.RIGHT_WINDOWS
		},
		{
			(Keys)93,
			KeyName.APPLICATION_SELECT
		},
		{
			(Keys)163,
			KeyName.RIGHT_CONTROL
		},
		{
			(Keys)37,
			KeyName.ARROW_LEFT
		},
		{
			(Keys)40,
			KeyName.ARROW_DOWN
		},
		{
			(Keys)39,
			KeyName.ARROW_RIGHT
		},
		{
			(Keys)96,
			KeyName.NUM_ZERO
		}
	};

	public LogitechKeyboard(DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Logitech, Fragment.FromGrid(new Rectangle(0, 0, 21, 6)), colorProfile)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		_colors = new byte[base.LedCount * 4];
	}

	public override void Present()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (NativeMethods.LogiLedSetTargetDevice(4))
		{
			for (int i = 0; i < base.LedCount; i++)
			{
				Vector4 processedLedColor = GetProcessedLedColor(i);
				_colors[i * 4 + 2] = (byte)(processedLedColor.X * 255f);
				_colors[i * 4 + 1] = (byte)(processedLedColor.Y * 255f);
				_colors[i * 4] = (byte)(processedLedColor.Z * 255f);
				_colors[i * 4 + 3] = byte.MaxValue;
			}
			NativeMethods.LogiLedSetLightingFromBitmap(_colors);
		}
	}

	public override void Render(IEnumerable<RgbKey> keys)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		int listCount = 0;
		foreach (RgbKey key in keys)
		{
			if (XnaToLogitechKeys.TryGetValue(key.Key, out var value))
			{
				Color color = ProcessLedColor(key.CurrentColor);
				_excludedKeys[listCount++] = value;
				NativeMethods.LogiLedSetLightingForKeyWithKeyName(value, ((Color)(ref color)).R * 100 / 255, ((Color)(ref color)).G * 100 / 255, ((Color)(ref color)).B * 100 / 255);
			}
		}
		NativeMethods.LogiLedExcludeKeysFromBitmap(_excludedKeys, listCount);
	}
}
