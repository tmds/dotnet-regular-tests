// Mono does not support runtime async.
// This test is ran against Mono builds to verify no assemblies require runtime async.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Find the root of the .NET installation.
// <dotnet-root>/shared/Microsoft.NETCore.App/<version>/System.Private.CoreLib.dll
string corelib = typeof(object).Assembly.Location;
string dotnetRoot = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(corelib)!, "..", "..", ".."));
if (!File.Exists(Path.Combine(dotnetRoot, "dotnet")))
    throw new Exception($"Expected to find 'dotnet' in {dotnetRoot}");
Console.WriteLine($"Searching for assemblies in {dotnetRoot}");

var assemblies = Directory.EnumerateFiles(dotnetRoot, "*.dll", SearchOption.AllDirectories);
int failureCount = 0;
foreach (var assembly in assemblies)
{
    using var file = File.Open(assembly, FileMode.Open, FileAccess.Read);
    using var reader = new PEReader(file);

    if (!reader.HasMetadata)
        continue;

    var metadataReader = reader.GetMetadataReader();

    foreach (var methodHandle in metadataReader.MethodDefinitions)
    {
        var method = metadataReader.GetMethodDefinition(methodHandle);

        // Detect the use of runtime async through MethodImplAttributes.Async.
        if ((method.ImplAttributes & MethodImplAttributes.Async) != 0)
        {
            Console.WriteLine($"FAIL: {assembly} has methods with MethodImplAttributes.Async");
            failureCount++;
            break;
        }
    }
}

if (failureCount > 0)
{
    Console.WriteLine($"FAIL: assemblies use runtime async. Mono does not support runtime async.");
    return 1;
}

Console.WriteLine("PASS: no assemblies use runtime async.");
return 0;
