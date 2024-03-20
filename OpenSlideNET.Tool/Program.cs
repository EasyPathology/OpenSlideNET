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
TiffPageDeleter.NdpiDeleteMacro(input, output);
Console.WriteLine("输出完成");