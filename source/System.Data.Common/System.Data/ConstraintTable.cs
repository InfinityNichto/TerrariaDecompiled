using System.Xml.Schema;

namespace System.Data;

internal sealed class ConstraintTable
{
	public DataTable table;

	public XmlSchemaIdentityConstraint constraint;

	public ConstraintTable(DataTable t, XmlSchemaIdentityConstraint c)
	{
		table = t;
		constraint = c;
	}
}
