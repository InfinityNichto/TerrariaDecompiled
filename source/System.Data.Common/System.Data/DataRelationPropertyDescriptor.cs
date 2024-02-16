using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class DataRelationPropertyDescriptor : PropertyDescriptor
{
	internal DataRelation Relation { get; }

	public override Type ComponentType => typeof(DataRowView);

	public override bool IsReadOnly => false;

	public override Type PropertyType => typeof(IBindingList);

	internal DataRelationPropertyDescriptor(DataRelation dataRelation)
		: base(dataRelation.RelationName, null)
	{
		Relation = dataRelation;
	}

	public override bool Equals([NotNullWhen(true)] object other)
	{
		if (other is DataRelationPropertyDescriptor)
		{
			DataRelationPropertyDescriptor dataRelationPropertyDescriptor = (DataRelationPropertyDescriptor)other;
			return dataRelationPropertyDescriptor.Relation == Relation;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Relation.GetHashCode();
	}

	public override bool CanResetValue(object component)
	{
		return false;
	}

	public override object GetValue(object component)
	{
		DataRowView dataRowView = (DataRowView)component;
		return dataRowView.CreateChildView(Relation);
	}

	public override void ResetValue(object component)
	{
	}

	public override void SetValue(object component, object value)
	{
	}

	public override bool ShouldSerializeValue(object component)
	{
		return false;
	}
}
