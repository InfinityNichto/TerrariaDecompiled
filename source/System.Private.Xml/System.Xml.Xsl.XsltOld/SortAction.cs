using System.Globalization;
using System.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class SortAction : CompiledAction
{
	private int _selectKey = -1;

	private Avt _langAvt;

	private Avt _dataTypeAvt;

	private Avt _orderAvt;

	private Avt _caseOrderAvt;

	private string _lang;

	private XmlDataType _dataType = XmlDataType.Text;

	private XmlSortOrder _order = XmlSortOrder.Ascending;

	private XmlCaseOrder _caseOrder;

	private Sort _sort;

	private bool _forwardCompatibility;

	private InputScopeManager _manager;

	private string ParseLang(string value)
	{
		if (value == null)
		{
			return null;
		}
		CultureInfo cultureInfo = new CultureInfo(value);
		if (!XmlComplianceUtil.IsValidLanguageID(value.ToCharArray(), 0, value.Length) && (value.Length == 0 || cultureInfo == null))
		{
			if (_forwardCompatibility)
			{
				return null;
			}
			throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "lang", value);
		}
		return value;
	}

	private XmlDataType ParseDataType(string value, InputScopeManager manager)
	{
		if (value == null)
		{
			return XmlDataType.Text;
		}
		if (value == "text")
		{
			return XmlDataType.Text;
		}
		if (value == "number")
		{
			return XmlDataType.Number;
		}
		PrefixQName.ParseQualifiedName(value, out var prefix, out var _);
		manager.ResolveXmlNamespace(prefix);
		if (prefix.Length == 0 && !_forwardCompatibility)
		{
			throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "data-type", value);
		}
		return XmlDataType.Text;
	}

	private XmlSortOrder ParseOrder(string value)
	{
		if (value == null)
		{
			return XmlSortOrder.Ascending;
		}
		if (value == "ascending")
		{
			return XmlSortOrder.Ascending;
		}
		if (value == "descending")
		{
			return XmlSortOrder.Descending;
		}
		if (_forwardCompatibility)
		{
			return XmlSortOrder.Ascending;
		}
		throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "order", value);
	}

	private XmlCaseOrder ParseCaseOrder(string value)
	{
		if (value == null)
		{
			return XmlCaseOrder.None;
		}
		if (value == "upper-first")
		{
			return XmlCaseOrder.UpperFirst;
		}
		if (value == "lower-first")
		{
			return XmlCaseOrder.LowerFirst;
		}
		if (_forwardCompatibility)
		{
			return XmlCaseOrder.None;
		}
		throw XsltException.Create(System.SR.Xslt_InvalidAttrValue, "case-order", value);
	}

	internal override void Compile(Compiler compiler)
	{
		CompileAttributes(compiler);
		CheckEmpty(compiler);
		if (_selectKey == -1)
		{
			_selectKey = compiler.AddQuery(".");
		}
		_forwardCompatibility = compiler.ForwardCompatibility;
		_manager = compiler.CloneScopeManager();
		_lang = ParseLang(CompiledAction.PrecalculateAvt(ref _langAvt));
		_dataType = ParseDataType(CompiledAction.PrecalculateAvt(ref _dataTypeAvt), _manager);
		_order = ParseOrder(CompiledAction.PrecalculateAvt(ref _orderAvt));
		_caseOrder = ParseCaseOrder(CompiledAction.PrecalculateAvt(ref _caseOrderAvt));
		if (_langAvt == null && _dataTypeAvt == null && _orderAvt == null && _caseOrderAvt == null)
		{
			_sort = new Sort(_selectKey, _lang, _dataType, _order, _caseOrder);
		}
	}

	internal override bool CompileAttribute(Compiler compiler)
	{
		string localName = compiler.Input.LocalName;
		string value = compiler.Input.Value;
		if (Ref.Equal(localName, compiler.Atoms.Select))
		{
			_selectKey = compiler.AddQuery(value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Lang))
		{
			_langAvt = Avt.CompileAvt(compiler, value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.DataType))
		{
			_dataTypeAvt = Avt.CompileAvt(compiler, value);
		}
		else if (Ref.Equal(localName, compiler.Atoms.Order))
		{
			_orderAvt = Avt.CompileAvt(compiler, value);
		}
		else
		{
			if (!Ref.Equal(localName, compiler.Atoms.CaseOrder))
			{
				return false;
			}
			_caseOrderAvt = Avt.CompileAvt(compiler, value);
		}
		return true;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		processor.AddSort((_sort != null) ? _sort : new Sort(_selectKey, (_langAvt == null) ? _lang : ParseLang(_langAvt.Evaluate(processor, frame)), (_dataTypeAvt == null) ? _dataType : ParseDataType(_dataTypeAvt.Evaluate(processor, frame), _manager), (_orderAvt == null) ? _order : ParseOrder(_orderAvt.Evaluate(processor, frame)), (_caseOrderAvt == null) ? _caseOrder : ParseCaseOrder(_caseOrderAvt.Evaluate(processor, frame))));
		frame.Finished();
	}
}
