using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using System.Text.Json;
using System.Xml.Serialization;
using test3.Models;

namespace test3.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 2, warmupCount: 3, iterationCount: 30)]
    public class SerializationBenchmarks
    {
        private Root data = new();

        private byte[] jsonBytes = Array.Empty<byte>();
        private byte[] xmlBytes = Array.Empty<byte>();
        private byte[] protobufData = Array.Empty<byte>();

        private readonly XmlSerializer xmlSerializer = new(typeof(Root));
        private int _xmlCapacity;
        private int _protobufCapacity;

        [GlobalSetup]
        public void Setup()
        {
            var jsonString = File.ReadAllText("data/testdata3.json");
            var jsonRoot = JsonSerializer.Deserialize<JsonRoot>(
                jsonString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            data = new Root { RESULT = jsonRoot!.RESPONSE.RESULT };

            jsonBytes = JsonSerializer.SerializeToUtf8Bytes(data);

            using (var ms = new MemoryStream())
            {
                xmlSerializer.Serialize(ms, data);
                xmlBytes = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, data);
                protobufData = ms.ToArray();
            }

            _xmlCapacity = xmlBytes.Length;
            _protobufCapacity = protobufData.Length;

            var expectedCount = data.RESULT.SelectMany(r => r.AntalKörfält2).Count();
            var firstGid = data.RESULT.SelectMany(r => r.AntalKörfält2).First().GID;

            var jsonRt = JsonSerializer.Deserialize<Root>(jsonBytes)!;
            if (jsonRt.RESULT.SelectMany(r => r.AntalKörfält2).Count() != expectedCount)
                throw new InvalidOperationException("JSON round-trip failed: object count mismatch");
            if (jsonRt.RESULT.SelectMany(r => r.AntalKörfält2).First().GID != firstGid)
                throw new InvalidOperationException("JSON round-trip failed: GID field mismatch");

            using (var ms = new MemoryStream(xmlBytes))
            {
                var xmlRt = (Root)xmlSerializer.Deserialize(ms)!;
                if (xmlRt.RESULT.SelectMany(r => r.AntalKörfält2).Count() != expectedCount)
                    throw new InvalidOperationException("XML round-trip failed: object count mismatch");
                if (xmlRt.RESULT.SelectMany(r => r.AntalKörfält2).First().GID != firstGid)
                    throw new InvalidOperationException("XML round-trip failed: GID field mismatch");
            }

            using (var ms = new MemoryStream(protobufData))
            {
                var protoRt = Serializer.Deserialize<Root>(ms);
                if (protoRt.RESULT.SelectMany(r => r.AntalKörfält2).Count() != expectedCount)
                    throw new InvalidOperationException("Protobuf round-trip failed: object count mismatch");
                if (protoRt.RESULT.SelectMany(r => r.AntalKörfält2).First().GID != firstGid)
                    throw new InvalidOperationException("Protobuf round-trip failed: GID field mismatch");
            }

            Console.WriteLine();
            Console.WriteLine("=== ROUND-TRIP VALIDATION ===");
            Console.WriteLine("All three formats passed round-trip validation.");
            Console.WriteLine();
            Console.WriteLine($"Objects: {expectedCount:N0}  |  JSON: {jsonBytes.Length:N0} B  |  XML: {xmlBytes.Length:N0} B  |  Protobuf: {protobufData.Length:N0} B");

            var resultsDir = Environment.GetEnvironmentVariable("BENCH_RESULTS_DIR");
            if (resultsDir != null)
            {
                Directory.CreateDirectory(resultsDir);
                File.WriteAllText(
                    Path.Combine(resultsDir, "payload-info.json"),
                    JsonSerializer.Serialize(new Dictionary<string, int>
                    {
                        ["JsonBytes"]    = jsonBytes.Length,
                        ["XmlBytes"]     = xmlBytes.Length,
                        ["ProtobufBytes"] = protobufData.Length,
                        ["ObjectCount"]  = expectedCount
                    }));
            }
        }

        [Benchmark]
        public byte[] Json_Serialize() => JsonSerializer.SerializeToUtf8Bytes(data);

        [Benchmark]
        public Root Json_Deserialize() => JsonSerializer.Deserialize<Root>(jsonBytes)!;

        [Benchmark]
        public byte[] Xml_Serialize()
        {
            using var ms = new MemoryStream(_xmlCapacity);
            xmlSerializer.Serialize(ms, data);
            return ms.ToArray();
        }

        [Benchmark]
        public Root Xml_Deserialize()
        {
            using var ms = new MemoryStream(xmlBytes);
            return (Root)xmlSerializer.Deserialize(ms)!;
        }

        [Benchmark]
        public byte[] Protobuf_Serialize()
        {
            using var ms = new MemoryStream(_protobufCapacity);
            Serializer.Serialize(ms, data);
            return ms.ToArray();
        }

        [Benchmark]
        public Root Protobuf_Deserialize()
        {
            using var ms = new MemoryStream(protobufData);
            return Serializer.Deserialize<Root>(ms);
        }
    }
}
