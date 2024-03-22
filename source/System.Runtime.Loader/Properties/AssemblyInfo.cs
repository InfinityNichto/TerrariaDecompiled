using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

[assembly: CLSCompliant(true)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
[assembly: AssemblyDefaultAlias("System.Runtime.Loader")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("System.Runtime.Loader")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.Runtime.Loader")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: AssemblyVersion("6.0.0.0")]
[assembly: TypeForwardedTo(typeof(AssemblyExtensions))]
[assembly: TypeForwardedTo(typeof(MetadataUpdateHandlerAttribute))]
[assembly: TypeForwardedTo(typeof(MetadataUpdater))]
[assembly: TypeForwardedTo(typeof(CreateNewOnMetadataUpdateAttribute))]
[assembly: TypeForwardedTo(typeof(AssemblyDependencyResolver))]
[assembly: TypeForwardedTo(typeof(AssemblyLoadContext))]
[module: SkipLocalsInit]