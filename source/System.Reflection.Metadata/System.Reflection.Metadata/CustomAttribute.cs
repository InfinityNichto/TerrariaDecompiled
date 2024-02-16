using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

public readonly struct CustomAttribute
{
	private readonly MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private CustomAttributeHandle Handle => CustomAttributeHandle.FromRowId(RowId);

	private MethodDefTreatment Treatment => (MethodDefTreatment)(_treatmentAndRowId >> 24);

	public EntityHandle Constructor => _reader.CustomAttributeTable.GetConstructor(Handle);

	public EntityHandle Parent => _reader.CustomAttributeTable.GetParent(Handle);

	public BlobHandle Value
	{
		get
		{
			if (Treatment == MethodDefTreatment.None)
			{
				return _reader.CustomAttributeTable.GetValue(Handle);
			}
			return GetProjectedValue();
		}
	}

	internal CustomAttribute(MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	public CustomAttributeValue<TType> DecodeValue<TType>(ICustomAttributeTypeProvider<TType> provider)
	{
		return new CustomAttributeDecoder<TType>(provider, _reader).DecodeValue(Constructor, Value);
	}

	private BlobHandle GetProjectedValue()
	{
		CustomAttributeValueTreatment customAttributeValueTreatment = _reader.CalculateCustomAttributeValueTreatment(Handle);
		if (customAttributeValueTreatment == CustomAttributeValueTreatment.None)
		{
			return _reader.CustomAttributeTable.GetValue(Handle);
		}
		return GetProjectedValue(customAttributeValueTreatment);
	}

	private BlobHandle GetProjectedValue(CustomAttributeValueTreatment treatment)
	{
		BlobHandle.VirtualIndex virtualIndex;
		bool flag;
		switch (treatment)
		{
		case CustomAttributeValueTreatment.AttributeUsageVersionAttribute:
		case CustomAttributeValueTreatment.AttributeUsageDeprecatedAttribute:
			virtualIndex = BlobHandle.VirtualIndex.AttributeUsage_AllowMultiple;
			flag = true;
			break;
		case CustomAttributeValueTreatment.AttributeUsageAllowMultiple:
			virtualIndex = BlobHandle.VirtualIndex.AttributeUsage_AllowMultiple;
			flag = false;
			break;
		case CustomAttributeValueTreatment.AttributeUsageAllowSingle:
			virtualIndex = BlobHandle.VirtualIndex.AttributeUsage_AllowSingle;
			flag = false;
			break;
		default:
			return default(BlobHandle);
		}
		BlobHandle value = _reader.CustomAttributeTable.GetValue(Handle);
		BlobReader blobReader = _reader.GetBlobReader(value);
		if (blobReader.Length != 8)
		{
			return value;
		}
		if (blobReader.ReadInt16() != 1)
		{
			return value;
		}
		AttributeTargets attributeTargets = ProjectAttributeTargetValue(blobReader.ReadUInt32());
		if (flag)
		{
			attributeTargets |= AttributeTargets.Constructor | AttributeTargets.Property;
		}
		return BlobHandle.FromVirtualIndex(virtualIndex, (ushort)attributeTargets);
	}

	private static AttributeTargets ProjectAttributeTargetValue(uint rawValue)
	{
		if (rawValue == uint.MaxValue)
		{
			return AttributeTargets.All;
		}
		AttributeTargets attributeTargets = (AttributeTargets)0;
		if ((rawValue & (true ? 1u : 0u)) != 0)
		{
			attributeTargets |= AttributeTargets.Delegate;
		}
		if ((rawValue & 2u) != 0)
		{
			attributeTargets |= AttributeTargets.Enum;
		}
		if ((rawValue & 4u) != 0)
		{
			attributeTargets |= AttributeTargets.Event;
		}
		if ((rawValue & 8u) != 0)
		{
			attributeTargets |= AttributeTargets.Field;
		}
		if ((rawValue & 0x10u) != 0)
		{
			attributeTargets |= AttributeTargets.Interface;
		}
		if ((rawValue & 0x40u) != 0)
		{
			attributeTargets |= AttributeTargets.Method;
		}
		if ((rawValue & 0x80u) != 0)
		{
			attributeTargets |= AttributeTargets.Parameter;
		}
		if ((rawValue & 0x100u) != 0)
		{
			attributeTargets |= AttributeTargets.Property;
		}
		if ((rawValue & 0x200u) != 0)
		{
			attributeTargets |= AttributeTargets.Class;
		}
		if ((rawValue & 0x400u) != 0)
		{
			attributeTargets |= AttributeTargets.Struct;
		}
		return attributeTargets;
	}
}
