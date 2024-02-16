namespace System.IO.Compression;

internal enum BrotliEncoderOperation
{
	Process,
	Flush,
	Finish,
	EmitMetadata
}
