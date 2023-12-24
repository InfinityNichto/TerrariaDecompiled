namespace ReLogic.Text;

public interface IFontMetrics
{
	int LineSpacing { get; }

	float CharacterSpacing { get; }

	GlyphMetrics GetCharacterMetrics(char character);
}
