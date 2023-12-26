namespace ReLogic.OS;

public interface IClipboard
{
	string Value { get; set; }

	string MultiLineValue { get; }
}
