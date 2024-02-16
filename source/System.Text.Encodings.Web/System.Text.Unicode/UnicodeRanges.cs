using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Text.Unicode;

public static class UnicodeRanges
{
	private static UnicodeRange _none;

	private static UnicodeRange _all;

	private static UnicodeRange _u0000;

	private static UnicodeRange _u0080;

	private static UnicodeRange _u0100;

	private static UnicodeRange _u0180;

	private static UnicodeRange _u0250;

	private static UnicodeRange _u02B0;

	private static UnicodeRange _u0300;

	private static UnicodeRange _u0370;

	private static UnicodeRange _u0400;

	private static UnicodeRange _u0500;

	private static UnicodeRange _u0530;

	private static UnicodeRange _u0590;

	private static UnicodeRange _u0600;

	private static UnicodeRange _u0700;

	private static UnicodeRange _u0750;

	private static UnicodeRange _u0780;

	private static UnicodeRange _u07C0;

	private static UnicodeRange _u0800;

	private static UnicodeRange _u0840;

	private static UnicodeRange _u0860;

	private static UnicodeRange _u08A0;

	private static UnicodeRange _u0900;

	private static UnicodeRange _u0980;

	private static UnicodeRange _u0A00;

	private static UnicodeRange _u0A80;

	private static UnicodeRange _u0B00;

	private static UnicodeRange _u0B80;

	private static UnicodeRange _u0C00;

	private static UnicodeRange _u0C80;

	private static UnicodeRange _u0D00;

	private static UnicodeRange _u0D80;

	private static UnicodeRange _u0E00;

	private static UnicodeRange _u0E80;

	private static UnicodeRange _u0F00;

	private static UnicodeRange _u1000;

	private static UnicodeRange _u10A0;

	private static UnicodeRange _u1100;

	private static UnicodeRange _u1200;

	private static UnicodeRange _u1380;

	private static UnicodeRange _u13A0;

	private static UnicodeRange _u1400;

	private static UnicodeRange _u1680;

	private static UnicodeRange _u16A0;

	private static UnicodeRange _u1700;

	private static UnicodeRange _u1720;

	private static UnicodeRange _u1740;

	private static UnicodeRange _u1760;

	private static UnicodeRange _u1780;

	private static UnicodeRange _u1800;

	private static UnicodeRange _u18B0;

	private static UnicodeRange _u1900;

	private static UnicodeRange _u1950;

	private static UnicodeRange _u1980;

	private static UnicodeRange _u19E0;

	private static UnicodeRange _u1A00;

	private static UnicodeRange _u1A20;

	private static UnicodeRange _u1AB0;

	private static UnicodeRange _u1B00;

	private static UnicodeRange _u1B80;

	private static UnicodeRange _u1BC0;

	private static UnicodeRange _u1C00;

	private static UnicodeRange _u1C50;

	private static UnicodeRange _u1C80;

	private static UnicodeRange _u1C90;

	private static UnicodeRange _u1CC0;

	private static UnicodeRange _u1CD0;

	private static UnicodeRange _u1D00;

	private static UnicodeRange _u1D80;

	private static UnicodeRange _u1DC0;

	private static UnicodeRange _u1E00;

	private static UnicodeRange _u1F00;

	private static UnicodeRange _u2000;

	private static UnicodeRange _u2070;

	private static UnicodeRange _u20A0;

	private static UnicodeRange _u20D0;

	private static UnicodeRange _u2100;

	private static UnicodeRange _u2150;

	private static UnicodeRange _u2190;

	private static UnicodeRange _u2200;

	private static UnicodeRange _u2300;

	private static UnicodeRange _u2400;

	private static UnicodeRange _u2440;

	private static UnicodeRange _u2460;

	private static UnicodeRange _u2500;

	private static UnicodeRange _u2580;

	private static UnicodeRange _u25A0;

	private static UnicodeRange _u2600;

	private static UnicodeRange _u2700;

	private static UnicodeRange _u27C0;

	private static UnicodeRange _u27F0;

	private static UnicodeRange _u2800;

	private static UnicodeRange _u2900;

	private static UnicodeRange _u2980;

	private static UnicodeRange _u2A00;

	private static UnicodeRange _u2B00;

	private static UnicodeRange _u2C00;

	private static UnicodeRange _u2C60;

	private static UnicodeRange _u2C80;

	private static UnicodeRange _u2D00;

	private static UnicodeRange _u2D30;

	private static UnicodeRange _u2D80;

	private static UnicodeRange _u2DE0;

	private static UnicodeRange _u2E00;

	private static UnicodeRange _u2E80;

	private static UnicodeRange _u2F00;

	private static UnicodeRange _u2FF0;

	private static UnicodeRange _u3000;

	private static UnicodeRange _u3040;

	private static UnicodeRange _u30A0;

	private static UnicodeRange _u3100;

	private static UnicodeRange _u3130;

	private static UnicodeRange _u3190;

	private static UnicodeRange _u31A0;

	private static UnicodeRange _u31C0;

	private static UnicodeRange _u31F0;

	private static UnicodeRange _u3200;

	private static UnicodeRange _u3300;

	private static UnicodeRange _u3400;

	private static UnicodeRange _u4DC0;

	private static UnicodeRange _u4E00;

	private static UnicodeRange _uA000;

	private static UnicodeRange _uA490;

	private static UnicodeRange _uA4D0;

	private static UnicodeRange _uA500;

	private static UnicodeRange _uA640;

	private static UnicodeRange _uA6A0;

	private static UnicodeRange _uA700;

	private static UnicodeRange _uA720;

	private static UnicodeRange _uA800;

	private static UnicodeRange _uA830;

	private static UnicodeRange _uA840;

	private static UnicodeRange _uA880;

	private static UnicodeRange _uA8E0;

	private static UnicodeRange _uA900;

	private static UnicodeRange _uA930;

	private static UnicodeRange _uA960;

	private static UnicodeRange _uA980;

	private static UnicodeRange _uA9E0;

	private static UnicodeRange _uAA00;

	private static UnicodeRange _uAA60;

	private static UnicodeRange _uAA80;

	private static UnicodeRange _uAAE0;

	private static UnicodeRange _uAB00;

	private static UnicodeRange _uAB30;

	private static UnicodeRange _uAB70;

	private static UnicodeRange _uABC0;

	private static UnicodeRange _uAC00;

	private static UnicodeRange _uD7B0;

	private static UnicodeRange _uF900;

	private static UnicodeRange _uFB00;

	private static UnicodeRange _uFB50;

	private static UnicodeRange _uFE00;

	private static UnicodeRange _uFE10;

	private static UnicodeRange _uFE20;

	private static UnicodeRange _uFE30;

	private static UnicodeRange _uFE50;

	private static UnicodeRange _uFE70;

	private static UnicodeRange _uFF00;

	private static UnicodeRange _uFFF0;

	public static UnicodeRange None => _none ?? CreateEmptyRange(ref _none);

	public static UnicodeRange All => _all ?? CreateRange(ref _all, '\0', '\uffff');

	public static UnicodeRange BasicLatin => _u0000 ?? CreateRange(ref _u0000, '\0', '\u007f');

	public static UnicodeRange Latin1Supplement => _u0080 ?? CreateRange(ref _u0080, '\u0080', 'ÿ');

	public static UnicodeRange LatinExtendedA => _u0100 ?? CreateRange(ref _u0100, 'Ā', 'ſ');

	public static UnicodeRange LatinExtendedB => _u0180 ?? CreateRange(ref _u0180, 'ƀ', 'ɏ');

	public static UnicodeRange IpaExtensions => _u0250 ?? CreateRange(ref _u0250, 'ɐ', 'ʯ');

	public static UnicodeRange SpacingModifierLetters => _u02B0 ?? CreateRange(ref _u02B0, 'ʰ', '\u02ff');

	public static UnicodeRange CombiningDiacriticalMarks => _u0300 ?? CreateRange(ref _u0300, '\u0300', '\u036f');

	public static UnicodeRange GreekandCoptic => _u0370 ?? CreateRange(ref _u0370, 'Ͱ', 'Ͽ');

	public static UnicodeRange Cyrillic => _u0400 ?? CreateRange(ref _u0400, 'Ѐ', 'ӿ');

	public static UnicodeRange CyrillicSupplement => _u0500 ?? CreateRange(ref _u0500, 'Ԁ', 'ԯ');

	public static UnicodeRange Armenian => _u0530 ?? CreateRange(ref _u0530, '\u0530', '֏');

	public static UnicodeRange Hebrew => _u0590 ?? CreateRange(ref _u0590, '\u0590', '\u05ff');

	public static UnicodeRange Arabic => _u0600 ?? CreateRange(ref _u0600, '\u0600', 'ۿ');

	public static UnicodeRange Syriac => _u0700 ?? CreateRange(ref _u0700, '܀', 'ݏ');

	public static UnicodeRange ArabicSupplement => _u0750 ?? CreateRange(ref _u0750, 'ݐ', 'ݿ');

	public static UnicodeRange Thaana => _u0780 ?? CreateRange(ref _u0780, 'ހ', '\u07bf');

	public static UnicodeRange NKo => _u07C0 ?? CreateRange(ref _u07C0, '߀', '߿');

	public static UnicodeRange Samaritan => _u0800 ?? CreateRange(ref _u0800, 'ࠀ', '\u083f');

	public static UnicodeRange Mandaic => _u0840 ?? CreateRange(ref _u0840, 'ࡀ', '\u085f');

	public static UnicodeRange SyriacSupplement => _u0860 ?? CreateRange(ref _u0860, 'ࡠ', '\u086f');

	public static UnicodeRange ArabicExtendedA => _u08A0 ?? CreateRange(ref _u08A0, 'ࢠ', '\u08ff');

	public static UnicodeRange Devanagari => _u0900 ?? CreateRange(ref _u0900, '\u0900', 'ॿ');

	public static UnicodeRange Bengali => _u0980 ?? CreateRange(ref _u0980, 'ঀ', '\u09ff');

	public static UnicodeRange Gurmukhi => _u0A00 ?? CreateRange(ref _u0A00, '\u0a00', '\u0a7f');

	public static UnicodeRange Gujarati => _u0A80 ?? CreateRange(ref _u0A80, '\u0a80', '\u0aff');

	public static UnicodeRange Oriya => _u0B00 ?? CreateRange(ref _u0B00, '\u0b00', '\u0b7f');

	public static UnicodeRange Tamil => _u0B80 ?? CreateRange(ref _u0B80, '\u0b80', '\u0bff');

	public static UnicodeRange Telugu => _u0C00 ?? CreateRange(ref _u0C00, '\u0c00', '౿');

	public static UnicodeRange Kannada => _u0C80 ?? CreateRange(ref _u0C80, 'ಀ', '\u0cff');

	public static UnicodeRange Malayalam => _u0D00 ?? CreateRange(ref _u0D00, '\u0d00', 'ൿ');

	public static UnicodeRange Sinhala => _u0D80 ?? CreateRange(ref _u0D80, '\u0d80', '\u0dff');

	public static UnicodeRange Thai => _u0E00 ?? CreateRange(ref _u0E00, '\u0e00', '\u0e7f');

	public static UnicodeRange Lao => _u0E80 ?? CreateRange(ref _u0E80, '\u0e80', '\u0eff');

	public static UnicodeRange Tibetan => _u0F00 ?? CreateRange(ref _u0F00, 'ༀ', '\u0fff');

	public static UnicodeRange Myanmar => _u1000 ?? CreateRange(ref _u1000, 'က', '႟');

	public static UnicodeRange Georgian => _u10A0 ?? CreateRange(ref _u10A0, 'Ⴀ', 'ჿ');

	public static UnicodeRange HangulJamo => _u1100 ?? CreateRange(ref _u1100, 'ᄀ', 'ᇿ');

	public static UnicodeRange Ethiopic => _u1200 ?? CreateRange(ref _u1200, 'ሀ', '\u137f');

	public static UnicodeRange EthiopicSupplement => _u1380 ?? CreateRange(ref _u1380, 'ᎀ', '\u139f');

	public static UnicodeRange Cherokee => _u13A0 ?? CreateRange(ref _u13A0, 'Ꭰ', '\u13ff');

	public static UnicodeRange UnifiedCanadianAboriginalSyllabics => _u1400 ?? CreateRange(ref _u1400, '᐀', 'ᙿ');

	public static UnicodeRange Ogham => _u1680 ?? CreateRange(ref _u1680, '\u1680', '\u169f');

	public static UnicodeRange Runic => _u16A0 ?? CreateRange(ref _u16A0, 'ᚠ', '\u16ff');

	public static UnicodeRange Tagalog => _u1700 ?? CreateRange(ref _u1700, 'ᜀ', '\u171f');

	public static UnicodeRange Hanunoo => _u1720 ?? CreateRange(ref _u1720, 'ᜠ', '\u173f');

	public static UnicodeRange Buhid => _u1740 ?? CreateRange(ref _u1740, 'ᝀ', '\u175f');

	public static UnicodeRange Tagbanwa => _u1760 ?? CreateRange(ref _u1760, 'ᝠ', '\u177f');

	public static UnicodeRange Khmer => _u1780 ?? CreateRange(ref _u1780, 'ក', '\u17ff');

	public static UnicodeRange Mongolian => _u1800 ?? CreateRange(ref _u1800, '᠀', '\u18af');

	public static UnicodeRange UnifiedCanadianAboriginalSyllabicsExtended => _u18B0 ?? CreateRange(ref _u18B0, 'ᢰ', '\u18ff');

	public static UnicodeRange Limbu => _u1900 ?? CreateRange(ref _u1900, 'ᤀ', '᥏');

	public static UnicodeRange TaiLe => _u1950 ?? CreateRange(ref _u1950, 'ᥐ', '\u197f');

	public static UnicodeRange NewTaiLue => _u1980 ?? CreateRange(ref _u1980, 'ᦀ', '᧟');

	public static UnicodeRange KhmerSymbols => _u19E0 ?? CreateRange(ref _u19E0, '᧠', '᧿');

	public static UnicodeRange Buginese => _u1A00 ?? CreateRange(ref _u1A00, 'ᨀ', '᨟');

	public static UnicodeRange TaiTham => _u1A20 ?? CreateRange(ref _u1A20, 'ᨠ', '\u1aaf');

	public static UnicodeRange CombiningDiacriticalMarksExtended => _u1AB0 ?? CreateRange(ref _u1AB0, '\u1ab0', '\u1aff');

	public static UnicodeRange Balinese => _u1B00 ?? CreateRange(ref _u1B00, '\u1b00', '\u1b7f');

	public static UnicodeRange Sundanese => _u1B80 ?? CreateRange(ref _u1B80, '\u1b80', 'ᮿ');

	public static UnicodeRange Batak => _u1BC0 ?? CreateRange(ref _u1BC0, 'ᯀ', '᯿');

	public static UnicodeRange Lepcha => _u1C00 ?? CreateRange(ref _u1C00, 'ᰀ', 'ᱏ');

	public static UnicodeRange OlChiki => _u1C50 ?? CreateRange(ref _u1C50, '᱐', '᱿');

	public static UnicodeRange CyrillicExtendedC => _u1C80 ?? CreateRange(ref _u1C80, 'ᲀ', '\u1c8f');

	public static UnicodeRange GeorgianExtended => _u1C90 ?? CreateRange(ref _u1C90, 'Ა', 'Ჿ');

	public static UnicodeRange SundaneseSupplement => _u1CC0 ?? CreateRange(ref _u1CC0, '᳀', '\u1ccf');

	public static UnicodeRange VedicExtensions => _u1CD0 ?? CreateRange(ref _u1CD0, '\u1cd0', '\u1cff');

	public static UnicodeRange PhoneticExtensions => _u1D00 ?? CreateRange(ref _u1D00, 'ᴀ', 'ᵿ');

	public static UnicodeRange PhoneticExtensionsSupplement => _u1D80 ?? CreateRange(ref _u1D80, 'ᶀ', 'ᶿ');

	public static UnicodeRange CombiningDiacriticalMarksSupplement => _u1DC0 ?? CreateRange(ref _u1DC0, '\u1dc0', '\u1dff');

	public static UnicodeRange LatinExtendedAdditional => _u1E00 ?? CreateRange(ref _u1E00, 'Ḁ', 'ỿ');

	public static UnicodeRange GreekExtended => _u1F00 ?? CreateRange(ref _u1F00, 'ἀ', '\u1fff');

	public static UnicodeRange GeneralPunctuation => _u2000 ?? CreateRange(ref _u2000, '\u2000', '\u206f');

	public static UnicodeRange SuperscriptsandSubscripts => _u2070 ?? CreateRange(ref _u2070, '⁰', '\u209f');

	public static UnicodeRange CurrencySymbols => _u20A0 ?? CreateRange(ref _u20A0, '₠', '\u20cf');

	public static UnicodeRange CombiningDiacriticalMarksforSymbols => _u20D0 ?? CreateRange(ref _u20D0, '\u20d0', '\u20ff');

	public static UnicodeRange LetterlikeSymbols => _u2100 ?? CreateRange(ref _u2100, '℀', '⅏');

	public static UnicodeRange NumberForms => _u2150 ?? CreateRange(ref _u2150, '⅐', '\u218f');

	public static UnicodeRange Arrows => _u2190 ?? CreateRange(ref _u2190, '←', '⇿');

	public static UnicodeRange MathematicalOperators => _u2200 ?? CreateRange(ref _u2200, '∀', '⋿');

	public static UnicodeRange MiscellaneousTechnical => _u2300 ?? CreateRange(ref _u2300, '⌀', '⏿');

	public static UnicodeRange ControlPictures => _u2400 ?? CreateRange(ref _u2400, '␀', '\u243f');

	public static UnicodeRange OpticalCharacterRecognition => _u2440 ?? CreateRange(ref _u2440, '⑀', '\u245f');

	public static UnicodeRange EnclosedAlphanumerics => _u2460 ?? CreateRange(ref _u2460, '①', '⓿');

	public static UnicodeRange BoxDrawing => _u2500 ?? CreateRange(ref _u2500, '─', '╿');

	public static UnicodeRange BlockElements => _u2580 ?? CreateRange(ref _u2580, '▀', '▟');

	public static UnicodeRange GeometricShapes => _u25A0 ?? CreateRange(ref _u25A0, '■', '◿');

	public static UnicodeRange MiscellaneousSymbols => _u2600 ?? CreateRange(ref _u2600, '☀', '⛿');

	public static UnicodeRange Dingbats => _u2700 ?? CreateRange(ref _u2700, '✀', '➿');

	public static UnicodeRange MiscellaneousMathematicalSymbolsA => _u27C0 ?? CreateRange(ref _u27C0, '⟀', '⟯');

	public static UnicodeRange SupplementalArrowsA => _u27F0 ?? CreateRange(ref _u27F0, '⟰', '⟿');

	public static UnicodeRange BraillePatterns => _u2800 ?? CreateRange(ref _u2800, '⠀', '⣿');

	public static UnicodeRange SupplementalArrowsB => _u2900 ?? CreateRange(ref _u2900, '⤀', '⥿');

	public static UnicodeRange MiscellaneousMathematicalSymbolsB => _u2980 ?? CreateRange(ref _u2980, '⦀', '⧿');

	public static UnicodeRange SupplementalMathematicalOperators => _u2A00 ?? CreateRange(ref _u2A00, '⨀', '⫿');

	public static UnicodeRange MiscellaneousSymbolsandArrows => _u2B00 ?? CreateRange(ref _u2B00, '⬀', '⯿');

	public static UnicodeRange Glagolitic => _u2C00 ?? CreateRange(ref _u2C00, 'Ⰰ', '\u2c5f');

	public static UnicodeRange LatinExtendedC => _u2C60 ?? CreateRange(ref _u2C60, 'Ⱡ', 'Ɀ');

	public static UnicodeRange Coptic => _u2C80 ?? CreateRange(ref _u2C80, 'Ⲁ', '⳿');

	public static UnicodeRange GeorgianSupplement => _u2D00 ?? CreateRange(ref _u2D00, 'ⴀ', '\u2d2f');

	public static UnicodeRange Tifinagh => _u2D30 ?? CreateRange(ref _u2D30, 'ⴰ', '\u2d7f');

	public static UnicodeRange EthiopicExtended => _u2D80 ?? CreateRange(ref _u2D80, 'ⶀ', '\u2ddf');

	public static UnicodeRange CyrillicExtendedA => _u2DE0 ?? CreateRange(ref _u2DE0, '\u2de0', '\u2dff');

	public static UnicodeRange SupplementalPunctuation => _u2E00 ?? CreateRange(ref _u2E00, '⸀', '\u2e7f');

	public static UnicodeRange CjkRadicalsSupplement => _u2E80 ?? CreateRange(ref _u2E80, '⺀', '\u2eff');

	public static UnicodeRange KangxiRadicals => _u2F00 ?? CreateRange(ref _u2F00, '⼀', '\u2fdf');

	public static UnicodeRange IdeographicDescriptionCharacters => _u2FF0 ?? CreateRange(ref _u2FF0, '⿰', '\u2fff');

	public static UnicodeRange CjkSymbolsandPunctuation => _u3000 ?? CreateRange(ref _u3000, '\u3000', '〿');

	public static UnicodeRange Hiragana => _u3040 ?? CreateRange(ref _u3040, '\u3040', 'ゟ');

	public static UnicodeRange Katakana => _u30A0 ?? CreateRange(ref _u30A0, '゠', 'ヿ');

	public static UnicodeRange Bopomofo => _u3100 ?? CreateRange(ref _u3100, '\u3100', 'ㄯ');

	public static UnicodeRange HangulCompatibilityJamo => _u3130 ?? CreateRange(ref _u3130, '\u3130', '\u318f');

	public static UnicodeRange Kanbun => _u3190 ?? CreateRange(ref _u3190, '㆐', '㆟');

	public static UnicodeRange BopomofoExtended => _u31A0 ?? CreateRange(ref _u31A0, 'ㆠ', 'ㆿ');

	public static UnicodeRange CjkStrokes => _u31C0 ?? CreateRange(ref _u31C0, '㇀', '\u31ef');

	public static UnicodeRange KatakanaPhoneticExtensions => _u31F0 ?? CreateRange(ref _u31F0, 'ㇰ', 'ㇿ');

	public static UnicodeRange EnclosedCjkLettersandMonths => _u3200 ?? CreateRange(ref _u3200, '㈀', '㋿');

	public static UnicodeRange CjkCompatibility => _u3300 ?? CreateRange(ref _u3300, '㌀', '㏿');

	public static UnicodeRange CjkUnifiedIdeographsExtensionA => _u3400 ?? CreateRange(ref _u3400, '㐀', '䶿');

	public static UnicodeRange YijingHexagramSymbols => _u4DC0 ?? CreateRange(ref _u4DC0, '䷀', '䷿');

	public static UnicodeRange CjkUnifiedIdeographs => _u4E00 ?? CreateRange(ref _u4E00, '一', '\u9fff');

	public static UnicodeRange YiSyllables => _uA000 ?? CreateRange(ref _uA000, 'ꀀ', '\ua48f');

	public static UnicodeRange YiRadicals => _uA490 ?? CreateRange(ref _uA490, '꒐', '\ua4cf');

	public static UnicodeRange Lisu => _uA4D0 ?? CreateRange(ref _uA4D0, 'ꓐ', '꓿');

	public static UnicodeRange Vai => _uA500 ?? CreateRange(ref _uA500, 'ꔀ', '\ua63f');

	public static UnicodeRange CyrillicExtendedB => _uA640 ?? CreateRange(ref _uA640, 'Ꙁ', '\ua69f');

	public static UnicodeRange Bamum => _uA6A0 ?? CreateRange(ref _uA6A0, 'ꚠ', '\ua6ff');

	public static UnicodeRange ModifierToneLetters => _uA700 ?? CreateRange(ref _uA700, '\ua700', 'ꜟ');

	public static UnicodeRange LatinExtendedD => _uA720 ?? CreateRange(ref _uA720, '\ua720', 'ꟿ');

	public static UnicodeRange SylotiNagri => _uA800 ?? CreateRange(ref _uA800, 'ꠀ', '\ua82f');

	public static UnicodeRange CommonIndicNumberForms => _uA830 ?? CreateRange(ref _uA830, '꠰', '\ua83f');

	public static UnicodeRange Phagspa => _uA840 ?? CreateRange(ref _uA840, 'ꡀ', '\ua87f');

	public static UnicodeRange Saurashtra => _uA880 ?? CreateRange(ref _uA880, '\ua880', '\ua8df');

	public static UnicodeRange DevanagariExtended => _uA8E0 ?? CreateRange(ref _uA8E0, '\ua8e0', '\ua8ff');

	public static UnicodeRange KayahLi => _uA900 ?? CreateRange(ref _uA900, '꤀', '꤯');

	public static UnicodeRange Rejang => _uA930 ?? CreateRange(ref _uA930, 'ꤰ', '꥟');

	public static UnicodeRange HangulJamoExtendedA => _uA960 ?? CreateRange(ref _uA960, 'ꥠ', '\ua97f');

	public static UnicodeRange Javanese => _uA980 ?? CreateRange(ref _uA980, '\ua980', '꧟');

	public static UnicodeRange MyanmarExtendedB => _uA9E0 ?? CreateRange(ref _uA9E0, 'ꧠ', '\ua9ff');

	public static UnicodeRange Cham => _uAA00 ?? CreateRange(ref _uAA00, 'ꨀ', '꩟');

	public static UnicodeRange MyanmarExtendedA => _uAA60 ?? CreateRange(ref _uAA60, 'ꩠ', 'ꩿ');

	public static UnicodeRange TaiViet => _uAA80 ?? CreateRange(ref _uAA80, 'ꪀ', '꫟');

	public static UnicodeRange MeeteiMayekExtensions => _uAAE0 ?? CreateRange(ref _uAAE0, 'ꫠ', '\uaaff');

	public static UnicodeRange EthiopicExtendedA => _uAB00 ?? CreateRange(ref _uAB00, '\uab00', '\uab2f');

	public static UnicodeRange LatinExtendedE => _uAB30 ?? CreateRange(ref _uAB30, 'ꬰ', '\uab6f');

	public static UnicodeRange CherokeeSupplement => _uAB70 ?? CreateRange(ref _uAB70, 'ꭰ', 'ꮿ');

	public static UnicodeRange MeeteiMayek => _uABC0 ?? CreateRange(ref _uABC0, 'ꯀ', '\uabff');

	public static UnicodeRange HangulSyllables => _uAC00 ?? CreateRange(ref _uAC00, '가', '\ud7af');

	public static UnicodeRange HangulJamoExtendedB => _uD7B0 ?? CreateRange(ref _uD7B0, 'ힰ', '\ud7ff');

	public static UnicodeRange CjkCompatibilityIdeographs => _uF900 ?? CreateRange(ref _uF900, '豈', '\ufaff');

	public static UnicodeRange AlphabeticPresentationForms => _uFB00 ?? CreateRange(ref _uFB00, 'ﬀ', 'ﭏ');

	public static UnicodeRange ArabicPresentationFormsA => _uFB50 ?? CreateRange(ref _uFB50, 'ﭐ', '\ufdff');

	public static UnicodeRange VariationSelectors => _uFE00 ?? CreateRange(ref _uFE00, '\ufe00', '\ufe0f');

	public static UnicodeRange VerticalForms => _uFE10 ?? CreateRange(ref _uFE10, '︐', '\ufe1f');

	public static UnicodeRange CombiningHalfMarks => _uFE20 ?? CreateRange(ref _uFE20, '\ufe20', '\ufe2f');

	public static UnicodeRange CjkCompatibilityForms => _uFE30 ?? CreateRange(ref _uFE30, '︰', '\ufe4f');

	public static UnicodeRange SmallFormVariants => _uFE50 ?? CreateRange(ref _uFE50, '﹐', '\ufe6f');

	public static UnicodeRange ArabicPresentationFormsB => _uFE70 ?? CreateRange(ref _uFE70, 'ﹰ', '\ufeff');

	public static UnicodeRange HalfwidthandFullwidthForms => _uFF00 ?? CreateRange(ref _uFF00, '\uff00', '\uffef');

	public static UnicodeRange Specials => _uFFF0 ?? CreateRange(ref _uFFF0, '\ufff0', '\uffff');

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static UnicodeRange CreateEmptyRange([NotNull] ref UnicodeRange range)
	{
		Volatile.Write(ref range, new UnicodeRange(0, 0));
		return range;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static UnicodeRange CreateRange([NotNull] ref UnicodeRange range, char first, char last)
	{
		Volatile.Write(ref range, UnicodeRange.Create(first, last));
		return range;
	}
}
