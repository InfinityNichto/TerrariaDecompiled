namespace System.Xml.Xsl.XsltOld;

internal interface IRecordOutput
{
	Processor.OutputResult RecordDone(RecordBuilder record);

	void TheEnd();
}
