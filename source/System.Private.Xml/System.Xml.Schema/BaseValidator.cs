using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace System.Xml.Schema;

internal class BaseValidator
{
	private readonly XmlSchemaCollection _schemaCollection;

	private readonly IValidationEventHandling _eventHandling;

	private readonly XmlNameTable _nameTable;

	private SchemaNames _schemaNames;

	private readonly PositionInfo _positionInfo;

	private XmlResolver _xmlResolver;

	private Uri _baseUri;

	protected SchemaInfo schemaInfo;

	protected XmlValidatingReaderImpl reader;

	protected XmlQualifiedName elementName;

	protected ValidationState context;

	protected StringBuilder textValue;

	protected string textString;

	protected bool hasSibling;

	protected bool checkDatatype;

	public XmlValidatingReaderImpl Reader => reader;

	public XmlSchemaCollection SchemaCollection => _schemaCollection;

	public XmlNameTable NameTable => _nameTable;

	public SchemaNames SchemaNames
	{
		get
		{
			if (_schemaNames != null)
			{
				return _schemaNames;
			}
			if (_schemaCollection != null)
			{
				_schemaNames = _schemaCollection.GetSchemaNames(_nameTable);
			}
			else
			{
				_schemaNames = new SchemaNames(_nameTable);
			}
			return _schemaNames;
		}
	}

	public PositionInfo PositionInfo => _positionInfo;

	public XmlResolver XmlResolver
	{
		get
		{
			return _xmlResolver;
		}
		set
		{
			_xmlResolver = value;
		}
	}

	public Uri BaseUri
	{
		get
		{
			return _baseUri;
		}
		set
		{
			_baseUri = value;
		}
	}

	public ValidationEventHandler EventHandler => (ValidationEventHandler)_eventHandling.EventHandler;

	public SchemaInfo SchemaInfo => schemaInfo;

	public IDtdInfo DtdInfo
	{
		[param: DisallowNull]
		set
		{
			if (!(value is SchemaInfo schemaInfo))
			{
				throw new XmlException(System.SR.Xml_InternalError, string.Empty);
			}
			this.schemaInfo = schemaInfo;
		}
	}

	public virtual bool PreserveWhitespace => false;

	public BaseValidator(BaseValidator other)
	{
		reader = other.reader;
		_schemaCollection = other._schemaCollection;
		_eventHandling = other._eventHandling;
		_nameTable = other._nameTable;
		_schemaNames = other._schemaNames;
		_positionInfo = other._positionInfo;
		_xmlResolver = other._xmlResolver;
		_baseUri = other._baseUri;
		elementName = other.elementName;
	}

	public BaseValidator(XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling)
	{
		this.reader = reader;
		_schemaCollection = schemaCollection;
		_eventHandling = eventHandling;
		_nameTable = reader.NameTable;
		_positionInfo = PositionInfo.GetPositionInfo(reader);
		elementName = new XmlQualifiedName();
	}

	public virtual void Validate()
	{
	}

	public virtual void CompleteValidation()
	{
	}

	public virtual object FindId(string name)
	{
		return null;
	}

	public void ValidateText()
	{
		if (!context.NeedValidateChildren)
		{
			return;
		}
		if (context.IsNill)
		{
			SendValidationEvent(System.SR.Sch_ContentInNill, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
			return;
		}
		ContentValidator contentValidator = context.ElementDecl.ContentValidator;
		switch (contentValidator.ContentType)
		{
		case XmlSchemaContentType.ElementOnly:
		{
			ArrayList arrayList = contentValidator.ExpectedElements(context, isRequiredOnly: false);
			if (arrayList == null)
			{
				SendValidationEvent(System.SR.Sch_InvalidTextInElement, XmlSchemaValidator.BuildElementName(context.LocalName, context.Namespace));
				break;
			}
			SendValidationEvent(System.SR.Sch_InvalidTextInElementExpecting, new string[2]
			{
				XmlSchemaValidator.BuildElementName(context.LocalName, context.Namespace),
				XmlSchemaValidator.PrintExpectedElements(arrayList, getParticles: false)
			});
			break;
		}
		case XmlSchemaContentType.Empty:
			SendValidationEvent(System.SR.Sch_InvalidTextInEmpty, string.Empty);
			break;
		}
		if (checkDatatype)
		{
			SaveTextValue(reader.Value);
		}
	}

	public void ValidateWhitespace()
	{
		if (context.NeedValidateChildren)
		{
			XmlSchemaContentType contentType = context.ElementDecl.ContentValidator.ContentType;
			if (context.IsNill)
			{
				SendValidationEvent(System.SR.Sch_ContentInNill, XmlSchemaValidator.QNameString(context.LocalName, context.Namespace));
			}
			if (contentType == XmlSchemaContentType.Empty)
			{
				SendValidationEvent(System.SR.Sch_InvalidWhitespaceInEmpty, string.Empty);
			}
			if (checkDatatype)
			{
				SaveTextValue(reader.Value);
			}
		}
	}

	private void SaveTextValue(string value)
	{
		if (textString.Length == 0)
		{
			textString = value;
			return;
		}
		if (!hasSibling)
		{
			textValue.Append(textString);
			hasSibling = true;
		}
		textValue.Append(value);
	}

	protected void SendValidationEvent(string code)
	{
		SendValidationEvent(code, string.Empty);
	}

	protected void SendValidationEvent(string code, string[] args)
	{
		SendValidationEvent(new XmlSchemaException(code, args, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	protected void SendValidationEvent(string code, string arg)
	{
		SendValidationEvent(new XmlSchemaException(code, arg, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	protected void SendValidationEvent(XmlSchemaException e)
	{
		SendValidationEvent(e, XmlSeverityType.Error);
	}

	protected void SendValidationEvent(string code, string msg, XmlSeverityType severity)
	{
		SendValidationEvent(new XmlSchemaException(code, msg, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
	}

	protected void SendValidationEvent(string code, string[] args, XmlSeverityType severity)
	{
		SendValidationEvent(new XmlSchemaException(code, args, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
	}

	protected void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
	{
		if (_eventHandling != null)
		{
			_eventHandling.SendEvent(e, severity);
		}
		else if (severity == XmlSeverityType.Error)
		{
			throw e;
		}
	}

	protected static void ProcessEntity(SchemaInfo sinfo, string name, object sender, ValidationEventHandler eventhandler, string baseUri, int lineNumber, int linePosition)
	{
		XmlSchemaException ex = null;
		if (!sinfo.GeneralEntities.TryGetValue(new XmlQualifiedName(name), out var value))
		{
			ex = new XmlSchemaException(System.SR.Sch_UndeclaredEntity, name, baseUri, lineNumber, linePosition);
		}
		else if (value.NData.IsEmpty)
		{
			ex = new XmlSchemaException(System.SR.Sch_UnparsedEntityRef, name, baseUri, lineNumber, linePosition);
		}
		if (ex != null)
		{
			if (eventhandler == null)
			{
				throw ex;
			}
			eventhandler(sender, new ValidationEventArgs(ex));
		}
	}

	protected static void ProcessEntity(SchemaInfo sinfo, string name, IValidationEventHandling eventHandling, string baseUriStr, int lineNumber, int linePosition)
	{
		string text = null;
		if (!sinfo.GeneralEntities.TryGetValue(new XmlQualifiedName(name), out var value))
		{
			text = System.SR.Sch_UndeclaredEntity;
		}
		else if (value.NData.IsEmpty)
		{
			text = System.SR.Sch_UnparsedEntityRef;
		}
		if (text != null)
		{
			XmlSchemaException ex = new XmlSchemaException(text, name, baseUriStr, lineNumber, linePosition);
			if (eventHandling == null)
			{
				throw ex;
			}
			eventHandling.SendEvent(ex, XmlSeverityType.Error);
		}
	}

	public static BaseValidator CreateInstance(ValidationType valType, XmlValidatingReaderImpl reader, XmlSchemaCollection schemaCollection, IValidationEventHandling eventHandling, bool processIdentityConstraints)
	{
		return valType switch
		{
			ValidationType.XDR => new XdrValidator(reader, schemaCollection, eventHandling), 
			ValidationType.Schema => new XsdValidator(reader, schemaCollection, eventHandling), 
			ValidationType.DTD => new DtdValidator(reader, eventHandling, processIdentityConstraints), 
			ValidationType.Auto => new AutoValidator(reader, schemaCollection, eventHandling), 
			ValidationType.None => new BaseValidator(reader, schemaCollection, eventHandling), 
			_ => null, 
		};
	}
}
