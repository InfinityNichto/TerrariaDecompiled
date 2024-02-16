namespace System.Runtime.Serialization;

[DataContract(Name = "MarshalByRefObject", Namespace = "http://schemas.datacontract.org/2004/07/System")]
internal abstract class MarshalByRefObjectAdapter
{
	[DataMember(Name = "__identity", Order = 0)]
	public object Identity
	{
		get
		{
			return null;
		}
		set
		{
		}
	}
}
