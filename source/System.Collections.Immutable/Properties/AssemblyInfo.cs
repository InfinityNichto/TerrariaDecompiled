using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

[assembly: InternalsVisibleTo("System.Collections.Immutable.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001004b86c4cb78549b34bab61a3b1800e23bfeb5b3ec390074041536a7e3cbd97f5f04cf0f857155a8928eaa29ebfd11cfbbad3ba70efea7bda3226c6a8d370a4cd303f714486b6ebc225985a638471e6ef571cc92a4613c00b8fa65d61ccee0cbe5f36330c9a01f4183559f1bef24cc2917c6d913e3a541333a1d05d9bed22b38cb")]
[assembly: CLSCompliant(true)]
[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32 | DllImportSearchPath.AssemblyDirectory)]
[assembly: AssemblyDefaultAlias("System.Collections.Immutable")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("This package provides collections that are thread safe and guaranteed to never change their contents, also known as immutable collections. Like strings, any methods that perform modifications will not change the existing instance but instead return a new instance. For efficiency reasons, the implementation uses a sharing mechanism to ensure that newly created instances share as much data as possible with the previous instance while ensuring that operations have a predictable time complexity.\r\n\r\nCommonly Used Types:\r\nSystem.Collections.Immutable.ImmutableArray\r\nSystem.Collections.Immutable.ImmutableArray<T>\r\nSystem.Collections.Immutable.ImmutableDictionary\r\nSystem.Collections.Immutable.ImmutableDictionary<TKey,TValue>\r\nSystem.Collections.Immutable.ImmutableHashSet\r\nSystem.Collections.Immutable.ImmutableHashSet<T>\r\nSystem.Collections.Immutable.ImmutableList\r\nSystem.Collections.Immutable.ImmutableList<T>\r\nSystem.Collections.Immutable.ImmutableQueue\r\nSystem.Collections.Immutable.ImmutableQueue<T>\r\nSystem.Collections.Immutable.ImmutableSortedDictionary\r\nSystem.Collections.Immutable.ImmutableSortedDictionary<TKey,TValue>\r\nSystem.Collections.Immutable.ImmutableSortedSet\r\nSystem.Collections.Immutable.ImmutableSortedSet<T>\r\nSystem.Collections.Immutable.ImmutableStack\r\nSystem.Collections.Immutable.ImmutableStack<T>")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.Collections.Immutable")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: AssemblyVersion("6.0.0.0")]
[module: System.Runtime.CompilerServices.NullablePublicOnly(true)]
[module: SkipLocalsInit]
