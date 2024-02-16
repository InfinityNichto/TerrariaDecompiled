namespace System.Xml.Serialization;

internal sealed class ImportStructWorkItem
{
	private readonly StructModel _model;

	private readonly StructMapping _mapping;

	internal StructModel Model => _model;

	internal StructMapping Mapping => _mapping;

	internal ImportStructWorkItem(StructModel model, StructMapping mapping)
	{
		_model = model;
		_mapping = mapping;
	}
}
