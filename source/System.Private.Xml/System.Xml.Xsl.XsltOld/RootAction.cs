using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.XPath;
using System.Xml.Xsl.Runtime;
using MS.Internal.Xml.XPath;

namespace System.Xml.Xsl.XsltOld;

internal class RootAction : TemplateBaseAction
{
	private readonly Hashtable _attributeSetTable = new Hashtable();

	private readonly Hashtable _decimalFormatTable = new Hashtable();

	private List<Key> _keyList;

	private XsltOutput _output;

	public Stylesheet builtInSheet;

	internal XsltOutput Output
	{
		get
		{
			if (_output == null)
			{
				_output = new XsltOutput();
			}
			return _output;
		}
	}

	internal List<Key> KeyList => _keyList;

	internal override void Compile(Compiler compiler)
	{
		CompileDocument(compiler, inInclude: false);
	}

	internal void InsertKey(XmlQualifiedName name, int MatchKey, int UseKey)
	{
		if (_keyList == null)
		{
			_keyList = new List<Key>();
		}
		_keyList.Add(new Key(name, MatchKey, UseKey));
	}

	internal AttributeSetAction GetAttributeSet(XmlQualifiedName name)
	{
		AttributeSetAction attributeSetAction = (AttributeSetAction)_attributeSetTable[name];
		if (attributeSetAction == null)
		{
			throw XsltException.Create(System.SR.Xslt_NoAttributeSet, name.ToString());
		}
		return attributeSetAction;
	}

	public void PorcessAttributeSets(Stylesheet rootStylesheet)
	{
		MirgeAttributeSets(rootStylesheet);
		foreach (AttributeSetAction value in _attributeSetTable.Values)
		{
			if (value.containedActions != null)
			{
				value.containedActions.Reverse();
			}
		}
		CheckAttributeSets_RecurceInList(new Hashtable(), _attributeSetTable.Keys);
	}

	private void MirgeAttributeSets(Stylesheet stylesheet)
	{
		if (stylesheet.AttributeSetTable != null)
		{
			foreach (AttributeSetAction value in stylesheet.AttributeSetTable.Values)
			{
				ArrayList arrayList = value.containedActions;
				AttributeSetAction attributeSetAction2 = (AttributeSetAction)_attributeSetTable[value.Name];
				if (attributeSetAction2 == null)
				{
					attributeSetAction2 = new AttributeSetAction();
					attributeSetAction2.name = value.Name;
					attributeSetAction2.containedActions = new ArrayList();
					_attributeSetTable[value.Name] = attributeSetAction2;
				}
				ArrayList arrayList2 = attributeSetAction2.containedActions;
				if (arrayList != null)
				{
					int num = arrayList.Count - 1;
					while (0 <= num)
					{
						arrayList2.Add(arrayList[num]);
						num--;
					}
				}
			}
		}
		foreach (Stylesheet import in stylesheet.Imports)
		{
			MirgeAttributeSets(import);
		}
	}

	private void CheckAttributeSets_RecurceInList(Hashtable markTable, ICollection setQNames)
	{
		foreach (XmlQualifiedName setQName in setQNames)
		{
			object obj = markTable[setQName];
			if (obj == "P")
			{
				throw XsltException.Create(System.SR.Xslt_CircularAttributeSet, setQName.ToString());
			}
			if (obj != "D")
			{
				markTable[setQName] = "P";
				CheckAttributeSets_RecurceInContainer(markTable, GetAttributeSet(setQName));
				markTable[setQName] = "D";
			}
		}
	}

	private void CheckAttributeSets_RecurceInContainer(Hashtable markTable, ContainerAction container)
	{
		if (container.containedActions == null)
		{
			return;
		}
		foreach (Action containedAction in container.containedActions)
		{
			if (containedAction is UseAttributeSetsAction)
			{
				CheckAttributeSets_RecurceInList(markTable, ((UseAttributeSetsAction)containedAction).UsedSets);
			}
			else if (containedAction is ContainerAction)
			{
				CheckAttributeSets_RecurceInContainer(markTable, (ContainerAction)containedAction);
			}
		}
	}

	internal void AddDecimalFormat(XmlQualifiedName name, DecimalFormat formatinfo)
	{
		DecimalFormat decimalFormat = (DecimalFormat)_decimalFormatTable[name];
		if (decimalFormat != null)
		{
			NumberFormatInfo info = decimalFormat.info;
			NumberFormatInfo info2 = formatinfo.info;
			if (info.NumberDecimalSeparator != info2.NumberDecimalSeparator || info.NumberGroupSeparator != info2.NumberGroupSeparator || info.PositiveInfinitySymbol != info2.PositiveInfinitySymbol || info.NegativeSign != info2.NegativeSign || info.NaNSymbol != info2.NaNSymbol || info.PercentSymbol != info2.PercentSymbol || info.PerMilleSymbol != info2.PerMilleSymbol || decimalFormat.zeroDigit != formatinfo.zeroDigit || decimalFormat.digit != formatinfo.digit || decimalFormat.patternSeparator != formatinfo.patternSeparator)
			{
				throw XsltException.Create(System.SR.Xslt_DupDecimalFormat, name.ToString());
			}
		}
		_decimalFormatTable[name] = formatinfo;
	}

	internal DecimalFormat GetDecimalFormat(XmlQualifiedName name)
	{
		return _decimalFormatTable[name] as DecimalFormat;
	}

	internal override void Execute(Processor processor, ActionFrame frame)
	{
		switch (frame.State)
		{
		case 0:
		{
			frame.AllocateVariables(variableCount);
			XPathNavigator xPathNavigator = processor.Document.Clone();
			xPathNavigator.MoveToRoot();
			frame.InitNodeSet(new XPathSingletonIterator(xPathNavigator));
			if (containedActions != null && containedActions.Count > 0)
			{
				processor.PushActionFrame(frame);
			}
			frame.State = 2;
			break;
		}
		case 2:
			frame.NextNode(processor);
			if (processor.Debugger != null)
			{
				processor.PopDebuggerStack();
			}
			processor.PushTemplateLookup(frame.NodeSet, null, null);
			frame.State = 3;
			break;
		case 3:
			frame.Finished();
			break;
		case 1:
			break;
		}
	}
}
