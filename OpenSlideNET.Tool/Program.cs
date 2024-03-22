using System.Text.Json;
using OpenSlideNET;
Console.WriteLine($"{nameof(OpenSlideNET)}.Tools Start");
AppDomain.CurrentDomain.UnhandledException += async (o, e) =>
{
    var msg = e.ToString();
    Console.WriteLine(msg);
    await Console.Error.WriteAsync(msg);
};
Console.WriteLine(JsonSerializer.Serialize(args, new JsonSerializerOptions
{
    WriteIndented = true
}));
if (args.Length < 2) throw new ArgumentException($"command args should be at least 2");
var input  = args[0];
var output = args[1];
if (Directory.Exists(input))
{
    if (Path.HasExtension(output)) throw new ArgumentException("If specified as directory, output should be directory too");
    if (!Directory.Exists(output)) Directory.CreateDirectory(output);
    foreach (var file in Directory.EnumerateFiles(input))
    {
        TiffPageDeleter.NdpiDeleteMacro(file, Path.Combine(output, Path.GetFileName(file)));
    }
}
else if (File.Exists(input))
{
    TiffPageDeleter.NdpiDeleteMacro(input, output);
}
else
{
    throw new ArgumentException("input should be a file or directory");
}

Console.WriteLine("输出完成");