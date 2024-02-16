using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

[assembly: CLSCompliant(true)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
[assembly: AssemblyDefaultAlias("System.Threading.Thread")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("System.Threading.Thread")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.Threading.Thread")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: AssemblyVersion("6.0.0.0")]
[assembly: TypeForwardedTo(typeof(LocalDataStoreSlot))]
[assembly: TypeForwardedTo(typeof(ApartmentState))]
[assembly: TypeForwardedTo(typeof(CompressedStack))]
[assembly: TypeForwardedTo(typeof(ParameterizedThreadStart))]
[assembly: TypeForwardedTo(typeof(Thread))]
[assembly: TypeForwardedTo(typeof(ThreadAbortException))]
[assembly: TypeForwardedTo(typeof(ThreadExceptionEventArgs))]
[assembly: TypeForwardedTo(typeof(ThreadExceptionEventHandler))]
[assembly: TypeForwardedTo(typeof(ThreadInterruptedException))]
[assembly: TypeForwardedTo(typeof(ThreadPriority))]
[assembly: TypeForwardedTo(typeof(ThreadStart))]
[assembly: TypeForwardedTo(typeof(ThreadStartException))]
[assembly: TypeForwardedTo(typeof(ThreadState))]
[assembly: TypeForwardedTo(typeof(ThreadStateException))]
[module: SkipLocalsInit]
