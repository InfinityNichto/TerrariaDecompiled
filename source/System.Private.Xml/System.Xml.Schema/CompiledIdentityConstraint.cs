namespace System.Xml.Schema;

internal sealed class CompiledIdentityConstraint
{
	public enum ConstraintRole
	{
		Unique,
		Key,
		Keyref
	}

	internal XmlQualifiedName name = XmlQualifiedName.Empty;

	private readonly ConstraintRole _role;

	private readonly Asttree _selector;

	private readonly Asttree[] _fields;

	internal XmlQualifiedName refer = XmlQualifiedName.Empty;

	public static readonly CompiledIdentityConstraint Empty = new CompiledIdentityConstraint();

	public ConstraintRole Role => _role;

	public Asttree Selector => _selector;

	public Asttree[] Fields => _fields;

	private CompiledIdentityConstraint()
	{
	}

	public CompiledIdentityConstraint(XmlSchemaIdentityConstraint constraint, XmlNamespaceManager nsmgr)
	{
		name = constraint.QualifiedName;
		try
		{
			_selector = new Asttree(constraint.Selector.XPath, isField: false, nsmgr);
		}
		catch (XmlSchemaException ex)
		{
			ex.SetSource(constraint.Selector);
			throw;
		}
		XmlSchemaObjectCollection fields = constraint.Fields;
		_fields = new Asttree[fields.Count];
		for (int i = 0; i < fields.Count; i++)
		{
			try
			{
				_fields[i] = new Asttree(((XmlSchemaXPath)fields[i]).XPath, isField: true, nsmgr);
			}
			catch (XmlSchemaException ex2)
			{
				ex2.SetSource(constraint.Fields[i]);
				throw;
			}
		}
		if (constraint is XmlSchemaUnique)
		{
			_role = ConstraintRole.Unique;
			return;
		}
		if (constraint is XmlSchemaKey)
		{
			_role = ConstraintRole.Key;
			return;
		}
		_role = ConstraintRole.Keyref;
		refer = ((XmlSchemaKeyref)constraint).Refer;
	}
}
