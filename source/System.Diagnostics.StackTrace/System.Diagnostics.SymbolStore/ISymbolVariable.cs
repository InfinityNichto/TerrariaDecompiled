namespace System.Diagnostics.SymbolStore;

public interface ISymbolVariable
{
	string Name { get; }

	object Attributes { get; }

	SymAddressKind AddressKind { get; }

	int AddressField1 { get; }

	int AddressField2 { get; }

	int AddressField3 { get; }

	int StartOffset { get; }

	int EndOffset { get; }

	byte[] GetSignature();
}
