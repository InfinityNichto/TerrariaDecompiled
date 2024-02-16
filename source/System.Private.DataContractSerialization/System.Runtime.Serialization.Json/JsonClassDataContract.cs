using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;

namespace System.Runtime.Serialization.Json;

internal sealed class JsonClassDataContract : JsonDataContract
{
	private sealed class JsonClassDataContractCriticalHelper : JsonDataContractCriticalHelper
	{
		private JsonFormatClassReaderDelegate _jsonFormatReaderDelegate;

		private JsonFormatClassWriterDelegate _jsonFormatWriterDelegate;

		private XmlDictionaryString[] _memberNames;

		private readonly ClassDataContract _traditionalClassDataContract;

		private readonly string _typeName;

		internal JsonFormatClassReaderDelegate JsonFormatReaderDelegate
		{
			get
			{
				return _jsonFormatReaderDelegate;
			}
			set
			{
				_jsonFormatReaderDelegate = value;
			}
		}

		internal JsonFormatClassWriterDelegate JsonFormatWriterDelegate
		{
			get
			{
				return _jsonFormatWriterDelegate;
			}
			set
			{
				_jsonFormatWriterDelegate = value;
			}
		}

		internal XmlDictionaryString[] MemberNames => _memberNames;

		internal ClassDataContract TraditionalClassDataContract => _traditionalClassDataContract;

		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		public JsonClassDataContractCriticalHelper(ClassDataContract traditionalDataContract)
			: base(traditionalDataContract)
		{
			_typeName = (string.IsNullOrEmpty(traditionalDataContract.Namespace.Value) ? traditionalDataContract.Name.Value : (traditionalDataContract.Name.Value + ":" + XmlObjectSerializerWriteContextComplexJson.TruncateDefaultDataContractNamespace(traditionalDataContract.Namespace.Value)));
			_traditionalClassDataContract = traditionalDataContract;
			CopyMembersAndCheckDuplicateNames();
		}

		private void CopyMembersAndCheckDuplicateNames()
		{
			if (_traditionalClassDataContract.MemberNames == null)
			{
				return;
			}
			int num = _traditionalClassDataContract.MemberNames.Length;
			Dictionary<string, object> dictionary = new Dictionary<string, object>(num);
			XmlDictionaryString[] array = new XmlDictionaryString[num];
			for (int i = 0; i < num; i++)
			{
				if (dictionary.ContainsKey(_traditionalClassDataContract.MemberNames[i].Value))
				{
					throw new SerializationException(System.SR.Format(System.SR.JsonDuplicateMemberNames, DataContract.GetClrTypeFullName(_traditionalClassDataContract.UnderlyingType), _traditionalClassDataContract.MemberNames[i].Value));
				}
				dictionary.Add(_traditionalClassDataContract.MemberNames[i].Value, null);
				array[i] = DataContractJsonSerializer.ConvertXmlNameToJsonName(_traditionalClassDataContract.MemberNames[i]);
			}
			_memberNames = array;
		}
	}

	private readonly JsonClassDataContractCriticalHelper _helper;

	internal JsonFormatClassReaderDelegate JsonFormatReaderDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.JsonFormatReaderDelegate == null)
			{
				lock (this)
				{
					if (_helper.JsonFormatReaderDelegate == null)
					{
						JsonFormatClassReaderDelegate jsonFormatReaderDelegate = ((DataContractSerializer.Option != SerializationOption.ReflectionOnly) ? new JsonFormatReaderGenerator().GenerateClassReader(TraditionalClassDataContract) : new JsonFormatClassReaderDelegate(new ReflectionJsonClassReader(TraditionalClassDataContract).ReflectionReadClass));
						Interlocked.MemoryBarrier();
						_helper.JsonFormatReaderDelegate = jsonFormatReaderDelegate;
					}
				}
			}
			return _helper.JsonFormatReaderDelegate;
		}
	}

	internal JsonFormatClassWriterDelegate JsonFormatWriterDelegate
	{
		[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
		get
		{
			if (_helper.JsonFormatWriterDelegate == null)
			{
				lock (this)
				{
					if (_helper.JsonFormatWriterDelegate == null)
					{
						JsonFormatClassWriterDelegate jsonFormatWriterDelegate = ((DataContractSerializer.Option != SerializationOption.ReflectionOnly) ? new JsonFormatWriterGenerator().GenerateClassWriter(TraditionalClassDataContract) : new JsonFormatClassWriterDelegate(new ReflectionJsonFormatWriter().ReflectionWriteClass));
						Interlocked.MemoryBarrier();
						_helper.JsonFormatWriterDelegate = jsonFormatWriterDelegate;
					}
				}
			}
			return _helper.JsonFormatWriterDelegate;
		}
	}

	internal XmlDictionaryString[] MemberNames => _helper.MemberNames;

	internal override string TypeName => _helper.TypeName;

	private ClassDataContract TraditionalClassDataContract => _helper.TraditionalClassDataContract;

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public JsonClassDataContract(ClassDataContract traditionalDataContract)
		: base(new JsonClassDataContractCriticalHelper(traditionalDataContract))
	{
		_helper = base.Helper as JsonClassDataContractCriticalHelper;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
	{
		jsonReader.Read();
		object result = JsonFormatReaderDelegate(jsonReader, context, XmlDictionaryString.Empty, MemberNames);
		jsonReader.ReadEndElement();
		return result;
	}

	[RequiresUnreferencedCode("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
	public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
	{
		jsonWriter.WriteAttributeString(null, "type", null, "object");
		JsonFormatWriterDelegate(jsonWriter, obj, context, TraditionalClassDataContract, MemberNames);
	}
}
