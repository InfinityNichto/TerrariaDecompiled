using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;

[assembly: MetadataUpdateHandler(typeof(JsonSerializerOptionsUpdateHandler))]
[assembly: CLSCompliant(true)]
[assembly: AssemblyDefaultAlias("System.Text.Json")]
[assembly: NeutralResourcesLanguage("en-US")]
[assembly: AssemblyMetadata(".NETFrameworkAssembly", "")]
[assembly: AssemblyMetadata("Serviceable", "True")]
[assembly: AssemblyMetadata("PreferInbox", "True")]
[assembly: AssemblyMetadata("IsTrimmable", "True")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("© Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyDescription("Provides high-performance and low-allocating types that serialize objects to JavaScript Object Notation (JSON) text and deserialize JSON text to objects, with UTF-8 support built-in. Also provides types to read and write JSON text encoded as UTF-8, and to create an in-memory document object model (DOM), that is read-only, for random access of the JSON elements within a structured view of the data.\r\n\r\nCommonly Used Types:\r\nSystem.Text.Json.JsonSerializer\r\nSystem.Text.Json.JsonDocument\r\nSystem.Text.Json.JsonElement\r\nSystem.Text.Json.Utf8JsonWriter\r\nSystem.Text.Json.Utf8JsonReader")]
[assembly: AssemblyFileVersion("6.0.21.52210")]
[assembly: AssemblyInformationalVersion("6.0.0+4822e3c3aa77eb82b2fb33c9321f923cf11ddde6")]
[assembly: AssemblyProduct("Microsoft® .NET")]
[assembly: AssemblyTitle("System.Text.Json")]
[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/dotnet/runtime")]
[assembly: AssemblyVersion("6.0.0.0")]
[module: System.Runtime.CompilerServices.NullablePublicOnly(false)]
[module: SkipLocalsInit]
