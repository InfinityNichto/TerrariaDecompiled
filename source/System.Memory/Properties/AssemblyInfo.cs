using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

[assembly: CLSCompliant(true)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
[assembly: AssemblyDefaultAlias("System.Memory")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("System.Memory")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.Memory")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: AssemblyVersion("6.0.0.0")]
[assembly: TypeForwardedTo(typeof(BinaryPrimitives))]
[assembly: TypeForwardedTo(typeof(IMemoryOwner<>))]
[assembly: TypeForwardedTo(typeof(IPinnable))]
[assembly: TypeForwardedTo(typeof(MemoryHandle))]
[assembly: TypeForwardedTo(typeof(MemoryManager<>))]
[assembly: TypeForwardedTo(typeof(OperationStatus))]
[assembly: TypeForwardedTo(typeof(StandardFormat))]
[assembly: TypeForwardedTo(typeof(Utf8Formatter))]
[assembly: TypeForwardedTo(typeof(Utf8Parser))]
[assembly: TypeForwardedTo(typeof(Memory<>))]
[assembly: TypeForwardedTo(typeof(MemoryExtensions))]
[assembly: TypeForwardedTo(typeof(ReadOnlyMemory<>))]
[assembly: TypeForwardedTo(typeof(ReadOnlySpan<>))]
[assembly: TypeForwardedTo(typeof(MemoryMarshal))]
[assembly: TypeForwardedTo(typeof(Span<>))]
[assembly: TypeForwardedTo(typeof(SpanLineEnumerator))]
[assembly: TypeForwardedTo(typeof(SpanRuneEnumerator))]
[module: System.Runtime.CompilerServices.NullablePublicOnly(false)]
[module: SkipLocalsInit]
