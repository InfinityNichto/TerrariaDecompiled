using System;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: MetadataUpdateHandler(typeof(ReflectionCachesUpdateHandler))]
[assembly: CLSCompliant(true)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
[assembly: AssemblyDefaultAlias("System.ComponentModel.TypeConverter")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("System.ComponentModel.TypeConverter")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.ComponentModel.TypeConverter")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: AssemblyVersion("6.0.0.0")]
[assembly: TypeForwardedTo(typeof(Component))]
[assembly: TypeForwardedTo(typeof(DesignerSerializerAttribute))]
[assembly: TypeForwardedTo(typeof(DesignerAttribute))]
[assembly: TypeForwardedTo(typeof(EditorAttribute))]
[assembly: TypeForwardedTo(typeof(InvalidAsynchronousStateException))]
[assembly: TypeForwardedTo(typeof(InvalidEnumArgumentException))]
[assembly: TypeForwardedTo(typeof(ISupportInitialize))]
[assembly: TypeForwardedTo(typeof(TypeConverterAttribute))]
[assembly: TypeForwardedTo(typeof(TypeDescriptionProviderAttribute))]
[module: System.Runtime.CompilerServices.NullablePublicOnly(false)]
[module: SkipLocalsInit]
