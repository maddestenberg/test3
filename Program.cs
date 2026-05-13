using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using ProtoBuf;
using System.Text.Json;
using System.Xml.Serialization;
using test3.Benchmarks;
using test3.Models;

if (args.Contains("generate"))
{
    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var xmlSerializer = new XmlSerializer(typeof(Root));

    string sourceJson = File.ReadAllText("data/testdata3-source.json");
    var jsonRoot = JsonSerializer.Deserialize<JsonRoot>(sourceJson, jsonOptions);
    var data = new Root { RESULT = jsonRoot!.RESPONSE.RESULT };

    var benchJson = JsonSerializer.SerializeToUtf8Bytes(data);
    File.WriteAllBytes("data/testdata3.json", benchJson);
    Console.WriteLine($"Sparade data/testdata3.json       ({benchJson.Length,10:N0} bytes)");

    using (var ms = new MemoryStream())
    {
        xmlSerializer.Serialize(ms, data);
        File.WriteAllBytes("data/testdata3.xml", ms.ToArray());
        Console.WriteLine($"Sparade data/testdata3.xml        ({ms.Length,10:N0} bytes)");
    }

    using (var ms = new MemoryStream())
    {
        Serializer.Serialize(ms, data);
        File.WriteAllBytes("data/testdata3.pb", ms.ToArray());
        Console.WriteLine($"Sparade data/testdata3.pb         ({ms.Length,10:N0} bytes)");
    }

    int count = data.RESULT.SelectMany(r => r.AntalKörfält2).Count();
    Console.WriteLine($"{count:N0} objekt totalt.");
    return;
}

var resultsDir = Path.GetFullPath("Results");
var csvPath = Path.Combine(resultsDir, "raw-iterations.csv");
Environment.SetEnvironmentVariable("BENCH_RESULTS_DIR", resultsDir);
var config = DefaultConfig.Instance.AddExporter(new RawCsvExporter(csvPath));
BenchmarkRunner.Run<SerializationBenchmarks>(config);
