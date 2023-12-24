using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ReLogic.Peripherals.RGB.Corsair;

internal class CorsairKeyboard : RgbKeyboard
{
	private readonly CorsairLedColor[] _ledColors;

	private readonly Dictionary<Keys, int> _xnaKeyToIndex = new Dictionary<Keys, int>();

	private static readonly Dictionary<CorsairLedId, Keys> _corsairToXnaKeys = new Dictionary<CorsairLedId, Keys>
	{
		{
			CorsairLedId.CLK_Escape,
			(Keys)27
		},
		{
			CorsairLedId.CLK_F1,
			(Keys)112
		},
		{
			CorsairLedId.CLK_F2,
			(Keys)113
		},
		{
			CorsairLedId.CLK_F3,
			(Keys)114
		},
		{
			CorsairLedId.CLK_F4,
			(Keys)115
		},
		{
			CorsairLedId.CLK_F5,
			(Keys)116
		},
		{
			CorsairLedId.CLK_F6,
			(Keys)117
		},
		{
			CorsairLedId.CLK_F7,
			(Keys)118
		},
		{
			CorsairLedId.CLK_F8,
			(Keys)119
		},
		{
			CorsairLedId.CLK_F9,
			(Keys)120
		},
		{
			CorsairLedId.CLK_F10,
			(Keys)121
		},
		{
			CorsairLedId.CLK_F11,
			(Keys)122
		},
		{
			CorsairLedId.CLK_GraveAccentAndTilde,
			(Keys)192
		},
		{
			CorsairLedId.CLK_1,
			(Keys)49
		},
		{
			CorsairLedId.CLK_2,
			(Keys)50
		},
		{
			CorsairLedId.CLK_3,
			(Keys)51
		},
		{
			CorsairLedId.CLK_4,
			(Keys)52
		},
		{
			CorsairLedId.CLK_5,
			(Keys)53
		},
		{
			CorsairLedId.CLK_6,
			(Keys)54
		},
		{
			CorsairLedId.CLK_7,
			(Keys)55
		},
		{
			CorsairLedId.CLK_8,
			(Keys)56
		},
		{
			CorsairLedId.CLK_9,
			(Keys)57
		},
		{
			CorsairLedId.CLK_0,
			(Keys)48
		},
		{
			CorsairLedId.CLK_MinusAndUnderscore,
			(Keys)189
		},
		{
			CorsairLedId.CLK_Tab,
			(Keys)9
		},
		{
			CorsairLedId.CLK_Q,
			(Keys)81
		},
		{
			CorsairLedId.CLK_W,
			(Keys)87
		},
		{
			CorsairLedId.CLK_E,
			(Keys)69
		},
		{
			CorsairLedId.CLK_R,
			(Keys)82
		},
		{
			CorsairLedId.CLK_T,
			(Keys)84
		},
		{
			CorsairLedId.CLK_Y,
			(Keys)89
		},
		{
			CorsairLedId.CLK_U,
			(Keys)85
		},
		{
			CorsairLedId.CLK_I,
			(Keys)73
		},
		{
			CorsairLedId.CLK_O,
			(Keys)79
		},
		{
			CorsairLedId.CLK_P,
			(Keys)80
		},
		{
			CorsairLedId.CLK_BracketLeft,
			(Keys)219
		},
		{
			CorsairLedId.CLK_CapsLock,
			(Keys)20
		},
		{
			CorsairLedId.CLK_A,
			(Keys)65
		},
		{
			CorsairLedId.CLK_S,
			(Keys)83
		},
		{
			CorsairLedId.CLK_D,
			(Keys)68
		},
		{
			CorsairLedId.CLK_F,
			(Keys)70
		},
		{
			CorsairLedId.CLK_G,
			(Keys)71
		},
		{
			CorsairLedId.CLK_H,
			(Keys)72
		},
		{
			CorsairLedId.CLK_J,
			(Keys)74
		},
		{
			CorsairLedId.CLK_K,
			(Keys)75
		},
		{
			CorsairLedId.CLK_L,
			(Keys)76
		},
		{
			CorsairLedId.CLK_SemicolonAndColon,
			(Keys)186
		},
		{
			CorsairLedId.CLK_ApostropheAndDoubleQuote,
			(Keys)222
		},
		{
			CorsairLedId.CLK_LeftShift,
			(Keys)160
		},
		{
			CorsairLedId.CLK_Z,
			(Keys)90
		},
		{
			CorsairLedId.CLK_X,
			(Keys)88
		},
		{
			CorsairLedId.CLK_C,
			(Keys)67
		},
		{
			CorsairLedId.CLK_V,
			(Keys)86
		},
		{
			CorsairLedId.CLK_B,
			(Keys)66
		},
		{
			CorsairLedId.CLK_N,
			(Keys)78
		},
		{
			CorsairLedId.CLK_M,
			(Keys)77
		},
		{
			CorsairLedId.CLK_CommaAndLessThan,
			(Keys)188
		},
		{
			CorsairLedId.CLK_PeriodAndBiggerThan,
			(Keys)190
		},
		{
			CorsairLedId.CLK_SlashAndQuestionMark,
			(Keys)191
		},
		{
			CorsairLedId.CLK_LeftCtrl,
			(Keys)162
		},
		{
			CorsairLedId.CLK_LeftAlt,
			(Keys)164
		},
		{
			CorsairLedId.CLK_Space,
			(Keys)32
		},
		{
			CorsairLedId.CLK_RightAlt,
			(Keys)165
		},
		{
			CorsairLedId.CLK_Application,
			(Keys)93
		},
		{
			CorsairLedId.CLK_F12,
			(Keys)123
		},
		{
			CorsairLedId.CLK_PrintScreen,
			(Keys)44
		},
		{
			CorsairLedId.CLK_ScrollLock,
			(Keys)145
		},
		{
			CorsairLedId.CLK_PauseBreak,
			(Keys)19
		},
		{
			CorsairLedId.CLK_Insert,
			(Keys)45
		},
		{
			CorsairLedId.CLK_Home,
			(Keys)36
		},
		{
			CorsairLedId.CLK_PageUp,
			(Keys)33
		},
		{
			CorsairLedId.CLK_BracketRight,
			(Keys)221
		},
		{
			CorsairLedId.CLK_Backslash,
			(Keys)226
		},
		{
			CorsairLedId.CLK_Enter,
			(Keys)13
		},
		{
			CorsairLedId.CLK_EqualsAndPlus,
			(Keys)187
		},
		{
			CorsairLedId.CLK_Backspace,
			(Keys)8
		},
		{
			CorsairLedId.CLK_Delete,
			(Keys)46
		},
		{
			CorsairLedId.CLK_End,
			(Keys)35
		},
		{
			CorsairLedId.CLK_PageDown,
			(Keys)34
		},
		{
			CorsairLedId.CLK_RightShift,
			(Keys)161
		},
		{
			CorsairLedId.CLK_RightCtrl,
			(Keys)163
		},
		{
			CorsairLedId.CLK_UpArrow,
			(Keys)38
		},
		{
			CorsairLedId.CLK_LeftArrow,
			(Keys)37
		},
		{
			CorsairLedId.CLK_DownArrow,
			(Keys)40
		},
		{
			CorsairLedId.CLK_RightArrow,
			(Keys)39
		},
		{
			CorsairLedId.CLK_Mute,
			(Keys)173
		},
		{
			CorsairLedId.CLK_Stop,
			(Keys)178
		},
		{
			CorsairLedId.CLK_ScanPreviousTrack,
			(Keys)177
		},
		{
			CorsairLedId.CLK_PlayPause,
			(Keys)179
		},
		{
			CorsairLedId.CLK_ScanNextTrack,
			(Keys)176
		},
		{
			CorsairLedId.CLK_NumLock,
			(Keys)144
		},
		{
			CorsairLedId.CLK_KeypadSlash,
			(Keys)111
		},
		{
			CorsairLedId.CLK_KeypadAsterisk,
			(Keys)106
		},
		{
			CorsairLedId.CLK_KeypadMinus,
			(Keys)109
		},
		{
			CorsairLedId.CLK_KeypadPlus,
			(Keys)107
		},
		{
			CorsairLedId.CLK_Keypad7,
			(Keys)103
		},
		{
			CorsairLedId.CLK_Keypad8,
			(Keys)104
		},
		{
			CorsairLedId.CLK_Keypad9,
			(Keys)105
		},
		{
			CorsairLedId.CLK_Keypad4,
			(Keys)100
		},
		{
			CorsairLedId.CLK_Keypad5,
			(Keys)101
		},
		{
			CorsairLedId.CLK_Keypad6,
			(Keys)102
		},
		{
			CorsairLedId.CLK_Keypad1,
			(Keys)97
		},
		{
			CorsairLedId.CLK_Keypad2,
			(Keys)98
		},
		{
			CorsairLedId.CLK_Keypad3,
			(Keys)99
		},
		{
			CorsairLedId.CLK_Keypad0,
			(Keys)96
		},
		{
			CorsairLedId.CLK_KeypadPeriodAndDelete,
			(Keys)46
		},
		{
			CorsairLedId.CLK_VolumeUp,
			(Keys)175
		},
		{
			CorsairLedId.CLK_VolumeDown,
			(Keys)174
		}
	};

	private CorsairKeyboard(Fragment fragment, CorsairLedPosition[] ledPositions, DeviceColorProfile colorProfile)
		: base(RgbDeviceVendor.Corsair, fragment, colorProfile)
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		_ledColors = new CorsairLedColor[base.LedCount];
		for (int i = 0; i < ledPositions.Length; i++)
		{
			_ledColors[i].LedId = ledPositions[i].LedId;
			if (_corsairToXnaKeys.TryGetValue(ledPositions[i].LedId, out var value))
			{
				_xnaKeyToIndex[value] = i;
			}
		}
	}

	public override void Present()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < base.LedCount; i++)
		{
			Vector4 processedLedColor = GetProcessedLedColor(i);
			_ledColors[i].R = (int)(processedLedColor.X * 255f);
			_ledColors[i].G = (int)(processedLedColor.Y * 255f);
			_ledColors[i].B = (int)(processedLedColor.Z * 255f);
		}
		if (_ledColors.Length != 0)
		{
			NativeMethods.CorsairSetLedsColorsAsync(_ledColors.Length, _ledColors, IntPtr.Zero, IntPtr.Zero);
		}
	}

	public static CorsairKeyboard Create(int deviceIndex, DeviceColorProfile colorProfile)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		CorsairLedPosition[] ledPositionsForMouseMatOrKeyboard = CorsairHelper.GetLedPositionsForMouseMatOrKeyboard(deviceIndex);
		return new CorsairKeyboard(CorsairHelper.CreateFragment(ledPositionsForMouseMatOrKeyboard, Vector2.Zero), ledPositionsForMouseMatOrKeyboard, colorProfile);
	}

	public override void Render(IEnumerable<RgbKey> keys)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		foreach (RgbKey key in keys)
		{
			if (_xnaKeyToIndex.TryGetValue(key.Key, out var value))
			{
				int index = value;
				Color currentColor = key.CurrentColor;
				SetLedColor(index, ((Color)(ref currentColor)).ToVector4());
			}
		}
	}
}
