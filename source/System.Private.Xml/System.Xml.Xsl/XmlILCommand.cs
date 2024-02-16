using System.Xml.Xsl.Runtime;

namespace System.Xml.Xsl;

internal sealed class XmlILCommand
{
	private readonly ExecuteDelegate _delExec;

	private readonly XmlQueryStaticData _staticData;

	public XmlQueryStaticData StaticData => _staticData;

	public XmlILCommand(ExecuteDelegate delExec, XmlQueryStaticData staticData)
	{
		_delExec = delExec;
		_staticData = staticData;
	}

	public void Execute(object defaultDocument, XmlResolver dataSources, XsltArgumentList argumentList, XmlWriter writer)
	{
		try
		{
			if (writer is XmlAsyncCheckWriter)
			{
				writer = ((XmlAsyncCheckWriter)writer).CoreWriter;
			}
			if (writer is XmlWellFormedWriter { RawWriter: not null, WriteState: WriteState.Start } xmlWellFormedWriter && xmlWellFormedWriter.Settings.ConformanceLevel != ConformanceLevel.Document)
			{
				Execute(defaultDocument, dataSources, argumentList, new XmlMergeSequenceWriter(xmlWellFormedWriter.RawWriter));
			}
			else
			{
				Execute(defaultDocument, dataSources, argumentList, new XmlMergeSequenceWriter(new XmlRawWriterWrapper(writer)));
			}
		}
		finally
		{
			writer.Flush();
		}
	}

	private void Execute(object defaultDocument, XmlResolver dataSources, XsltArgumentList argumentList, XmlSequenceWriter results)
	{
		if (dataSources == null)
		{
			dataSources = XmlNullResolver.Singleton;
		}
		_delExec(new XmlQueryRuntime(_staticData, defaultDocument, dataSources, argumentList, results));
	}
}
