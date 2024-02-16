using System.Collections.Generic;
using System.Globalization;

namespace System.Drawing;

public static class ColorTranslator
{
	private static Dictionary<string, Color> s_htmlSysColorTable;

	internal static uint COLORREFToARGB(uint value)
	{
		return ((value & 0xFF) << 16) | (((value >> 8) & 0xFF) << 8) | ((value >> 16) & 0xFFu) | 0xFF000000u;
	}

	public static int ToWin32(Color c)
	{
		return c.R | (c.G << 8) | (c.B << 16);
	}

	public static int ToOle(Color c)
	{
		if (c.IsKnownColor && c.IsSystemColor)
		{
			switch (c.ToKnownColor())
			{
			case KnownColor.ActiveBorder:
				return -2147483638;
			case KnownColor.ActiveCaption:
				return -2147483646;
			case KnownColor.ActiveCaptionText:
				return -2147483639;
			case KnownColor.AppWorkspace:
				return -2147483636;
			case KnownColor.ButtonFace:
				return -2147483633;
			case KnownColor.ButtonHighlight:
				return -2147483628;
			case KnownColor.ButtonShadow:
				return -2147483632;
			case KnownColor.Control:
				return -2147483633;
			case KnownColor.ControlDark:
				return -2147483632;
			case KnownColor.ControlDarkDark:
				return -2147483627;
			case KnownColor.ControlLight:
				return -2147483626;
			case KnownColor.ControlLightLight:
				return -2147483628;
			case KnownColor.ControlText:
				return -2147483630;
			case KnownColor.Desktop:
				return -2147483647;
			case KnownColor.GradientActiveCaption:
				return -2147483621;
			case KnownColor.GradientInactiveCaption:
				return -2147483620;
			case KnownColor.GrayText:
				return -2147483631;
			case KnownColor.Highlight:
				return -2147483635;
			case KnownColor.HighlightText:
				return -2147483634;
			case KnownColor.HotTrack:
				return -2147483622;
			case KnownColor.InactiveBorder:
				return -2147483637;
			case KnownColor.InactiveCaption:
				return -2147483645;
			case KnownColor.InactiveCaptionText:
				return -2147483629;
			case KnownColor.Info:
				return -2147483624;
			case KnownColor.InfoText:
				return -2147483625;
			case KnownColor.Menu:
				return -2147483644;
			case KnownColor.MenuBar:
				return -2147483618;
			case KnownColor.MenuHighlight:
				return -2147483619;
			case KnownColor.MenuText:
				return -2147483641;
			case KnownColor.ScrollBar:
				return int.MinValue;
			case KnownColor.Window:
				return -2147483643;
			case KnownColor.WindowFrame:
				return -2147483642;
			case KnownColor.WindowText:
				return -2147483640;
			}
		}
		return ToWin32(c);
	}

	public static Color FromOle(int oleColor)
	{
		if (((uint)oleColor & 0x80000000u) != 0)
		{
			switch (oleColor)
			{
			case -2147483638:
				return Color.FromKnownColor(KnownColor.ActiveBorder);
			case -2147483646:
				return Color.FromKnownColor(KnownColor.ActiveCaption);
			case -2147483639:
				return Color.FromKnownColor(KnownColor.ActiveCaptionText);
			case -2147483636:
				return Color.FromKnownColor(KnownColor.AppWorkspace);
			case -2147483633:
				return Color.FromKnownColor(KnownColor.Control);
			case -2147483632:
				return Color.FromKnownColor(KnownColor.ControlDark);
			case -2147483627:
				return Color.FromKnownColor(KnownColor.ControlDarkDark);
			case -2147483626:
				return Color.FromKnownColor(KnownColor.ControlLight);
			case -2147483628:
				return Color.FromKnownColor(KnownColor.ControlLightLight);
			case -2147483630:
				return Color.FromKnownColor(KnownColor.ControlText);
			case -2147483647:
				return Color.FromKnownColor(KnownColor.Desktop);
			case -2147483621:
				return Color.FromKnownColor(KnownColor.GradientActiveCaption);
			case -2147483620:
				return Color.FromKnownColor(KnownColor.GradientInactiveCaption);
			case -2147483631:
				return Color.FromKnownColor(KnownColor.GrayText);
			case -2147483635:
				return Color.FromKnownColor(KnownColor.Highlight);
			case -2147483634:
				return Color.FromKnownColor(KnownColor.HighlightText);
			case -2147483622:
				return Color.FromKnownColor(KnownColor.HotTrack);
			case -2147483637:
				return Color.FromKnownColor(KnownColor.InactiveBorder);
			case -2147483645:
				return Color.FromKnownColor(KnownColor.InactiveCaption);
			case -2147483629:
				return Color.FromKnownColor(KnownColor.InactiveCaptionText);
			case -2147483624:
				return Color.FromKnownColor(KnownColor.Info);
			case -2147483625:
				return Color.FromKnownColor(KnownColor.InfoText);
			case -2147483644:
				return Color.FromKnownColor(KnownColor.Menu);
			case -2147483618:
				return Color.FromKnownColor(KnownColor.MenuBar);
			case -2147483619:
				return Color.FromKnownColor(KnownColor.MenuHighlight);
			case -2147483641:
				return Color.FromKnownColor(KnownColor.MenuText);
			case int.MinValue:
				return Color.FromKnownColor(KnownColor.ScrollBar);
			case -2147483643:
				return Color.FromKnownColor(KnownColor.Window);
			case -2147483642:
				return Color.FromKnownColor(KnownColor.WindowFrame);
			case -2147483640:
				return Color.FromKnownColor(KnownColor.WindowText);
			}
		}
		return KnownColorTable.ArgbToKnownColor(COLORREFToARGB((uint)oleColor));
	}

	public static Color FromWin32(int win32Color)
	{
		return FromOle(win32Color);
	}

	public static Color FromHtml(string htmlColor)
	{
		Color value = Color.Empty;
		if (htmlColor == null || htmlColor.Length == 0)
		{
			return value;
		}
		if (htmlColor[0] == '#' && (htmlColor.Length == 7 || htmlColor.Length == 4))
		{
			if (htmlColor.Length == 7)
			{
				value = Color.FromArgb(Convert.ToInt32(htmlColor.Substring(1, 2), 16), Convert.ToInt32(htmlColor.Substring(3, 2), 16), Convert.ToInt32(htmlColor.Substring(5, 2), 16));
			}
			else
			{
				string text = char.ToString(htmlColor[1]);
				string text2 = char.ToString(htmlColor[2]);
				string text3 = char.ToString(htmlColor[3]);
				value = Color.FromArgb(Convert.ToInt32(text + text, 16), Convert.ToInt32(text2 + text2, 16), Convert.ToInt32(text3 + text3, 16));
			}
		}
		if (value.IsEmpty && string.Equals(htmlColor, "LightGrey", StringComparison.OrdinalIgnoreCase))
		{
			value = Color.LightGray;
		}
		if (value.IsEmpty)
		{
			if (s_htmlSysColorTable == null)
			{
				InitializeHtmlSysColorTable();
			}
			s_htmlSysColorTable.TryGetValue(htmlColor.ToLowerInvariant(), out value);
		}
		if (value.IsEmpty)
		{
			try
			{
				value = ColorConverterCommon.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
			}
			catch (Exception ex)
			{
				throw new ArgumentException(ex.Message, "htmlColor", ex);
			}
		}
		return value;
	}

	public static string ToHtml(Color c)
	{
		string result = string.Empty;
		if (c.IsEmpty)
		{
			return result;
		}
		if (!c.IsSystemColor)
		{
			result = ((!c.IsNamedColor) ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : ((!(c == Color.LightGray)) ? c.Name : "LightGrey"));
		}
		else
		{
			switch (c.ToKnownColor())
			{
			case KnownColor.ActiveBorder:
				result = "activeborder";
				break;
			case KnownColor.ActiveCaption:
			case KnownColor.GradientActiveCaption:
				result = "activecaption";
				break;
			case KnownColor.AppWorkspace:
				result = "appworkspace";
				break;
			case KnownColor.Desktop:
				result = "background";
				break;
			case KnownColor.Control:
			case KnownColor.ControlLight:
				result = "buttonface";
				break;
			case KnownColor.ControlDark:
				result = "buttonshadow";
				break;
			case KnownColor.ControlText:
				result = "buttontext";
				break;
			case KnownColor.ActiveCaptionText:
				result = "captiontext";
				break;
			case KnownColor.GrayText:
				result = "graytext";
				break;
			case KnownColor.Highlight:
			case KnownColor.HotTrack:
				result = "highlight";
				break;
			case KnownColor.HighlightText:
			case KnownColor.MenuHighlight:
				result = "highlighttext";
				break;
			case KnownColor.InactiveBorder:
				result = "inactiveborder";
				break;
			case KnownColor.InactiveCaption:
			case KnownColor.GradientInactiveCaption:
				result = "inactivecaption";
				break;
			case KnownColor.InactiveCaptionText:
				result = "inactivecaptiontext";
				break;
			case KnownColor.Info:
				result = "infobackground";
				break;
			case KnownColor.InfoText:
				result = "infotext";
				break;
			case KnownColor.Menu:
			case KnownColor.MenuBar:
				result = "menu";
				break;
			case KnownColor.MenuText:
				result = "menutext";
				break;
			case KnownColor.ScrollBar:
				result = "scrollbar";
				break;
			case KnownColor.ControlDarkDark:
				result = "threeddarkshadow";
				break;
			case KnownColor.ControlLightLight:
				result = "buttonhighlight";
				break;
			case KnownColor.Window:
				result = "window";
				break;
			case KnownColor.WindowFrame:
				result = "windowframe";
				break;
			case KnownColor.WindowText:
				result = "windowtext";
				break;
			}
		}
		return result;
	}

	private static void InitializeHtmlSysColorTable()
	{
		s_htmlSysColorTable = new Dictionary<string, Color>(27)
		{
			["activeborder"] = Color.FromKnownColor(KnownColor.ActiveBorder),
			["activecaption"] = Color.FromKnownColor(KnownColor.ActiveCaption),
			["appworkspace"] = Color.FromKnownColor(KnownColor.AppWorkspace),
			["background"] = Color.FromKnownColor(KnownColor.Desktop),
			["buttonface"] = Color.FromKnownColor(KnownColor.Control),
			["buttonhighlight"] = Color.FromKnownColor(KnownColor.ControlLightLight),
			["buttonshadow"] = Color.FromKnownColor(KnownColor.ControlDark),
			["buttontext"] = Color.FromKnownColor(KnownColor.ControlText),
			["captiontext"] = Color.FromKnownColor(KnownColor.ActiveCaptionText),
			["graytext"] = Color.FromKnownColor(KnownColor.GrayText),
			["highlight"] = Color.FromKnownColor(KnownColor.Highlight),
			["highlighttext"] = Color.FromKnownColor(KnownColor.HighlightText),
			["inactiveborder"] = Color.FromKnownColor(KnownColor.InactiveBorder),
			["inactivecaption"] = Color.FromKnownColor(KnownColor.InactiveCaption),
			["inactivecaptiontext"] = Color.FromKnownColor(KnownColor.InactiveCaptionText),
			["infobackground"] = Color.FromKnownColor(KnownColor.Info),
			["infotext"] = Color.FromKnownColor(KnownColor.InfoText),
			["menu"] = Color.FromKnownColor(KnownColor.Menu),
			["menutext"] = Color.FromKnownColor(KnownColor.MenuText),
			["scrollbar"] = Color.FromKnownColor(KnownColor.ScrollBar),
			["threeddarkshadow"] = Color.FromKnownColor(KnownColor.ControlDarkDark),
			["threedface"] = Color.FromKnownColor(KnownColor.Control),
			["threedhighlight"] = Color.FromKnownColor(KnownColor.ControlLight),
			["threedlightshadow"] = Color.FromKnownColor(KnownColor.ControlLightLight),
			["window"] = Color.FromKnownColor(KnownColor.Window),
			["windowframe"] = Color.FromKnownColor(KnownColor.WindowFrame),
			["windowtext"] = Color.FromKnownColor(KnownColor.WindowText)
		};
	}
}
