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

        private int _jsonCapacity;
        private int _xmlCapacity;
        private int _protobufCapacity;

        [GlobalSetup]
        public void Setup()
        {
            jsonBytes    = File.ReadAllBytes("data/testdata3.json");
            xmlBytes     = File.ReadAllBytes("data/testdata3.xml");
            protobufData = File.ReadAllBytes("data/testdata3.pb");

            data = JsonSerializer.Deserialize<Root>(jsonBytes)!;

            _jsonCapacity    = jsonBytes.Length;
            _xmlCapacity     = xmlBytes.Length;
            _protobufCapacity = protobufData.Length;

            var originalItems = data.RESULT.SelectMany(r => r.AntalKörfält2).ToList();
            int expectedCount = originalItems.Count;

            Root jsonRt;
            using (var ms = new MemoryStream(jsonBytes))
                jsonRt = JsonSerializer.Deserialize<Root>(ms)!;
            AssertEquivalent(originalItems, jsonRt.RESULT.SelectMany(r => r.AntalKörfält2).ToList(), "JSON");

            Root xmlRt;
            using (var ms = new MemoryStream(xmlBytes))
                xmlRt = (Root)xmlSerializer.Deserialize(ms)!;
            AssertEquivalent(originalItems, xmlRt.RESULT.SelectMany(r => r.AntalKörfält2).ToList(), "XML");

            Root protoRt;
            using (var ms = new MemoryStream(protobufData))
                protoRt = Serializer.Deserialize<Root>(ms);
            AssertEquivalent(originalItems, protoRt.RESULT.SelectMany(r => r.AntalKörfält2).ToList(), "Protobuf");

            Console.WriteLine();
            Console.WriteLine("=== ROUND-TRIP VALIDATION ===");
            Console.WriteLine($"All three formats passed round-trip validation ({expectedCount:N0} objects, all fields).");
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
                        ["JsonBytes"]     = jsonBytes.Length,
                        ["XmlBytes"]      = xmlBytes.Length,
                        ["ProtobufBytes"] = protobufData.Length,
                        ["ObjectCount"]   = expectedCount
                    }));
            }
        }

        private static void AssertEquivalent(List<AntalKorfalt2> original, List<AntalKorfalt2> deserialized, string formatName)
        {
            if (original.Count != deserialized.Count)
                throw new InvalidOperationException($"{formatName} round-trip count mismatch: expected={original.Count}, actual={deserialized.Count}.");
            for (int i = 0; i < original.Count; i++)
                AssertEquivalent(original[i], deserialized[i], formatName, i);
        }

        private static void AssertEquivalent(AntalKorfalt2 expected, AntalKorfalt2 actual, string formatName, int index)
        {
            void Fail(string field, object? exp, object? act) =>
                throw new InvalidOperationException($"{formatName} round-trip mismatch at index {index}, field '{field}': expected='{exp}', actual='{act}'.");

            if (expected.GID != actual.GID) Fail(nameof(AntalKorfalt2.GID), expected.GID, actual.GID);
            if (expected.Valid_From != actual.Valid_From) Fail(nameof(AntalKorfalt2.Valid_From), expected.Valid_From, actual.Valid_From);
            if (expected.Valid_To != actual.Valid_To) Fail(nameof(AntalKorfalt2.Valid_To), expected.Valid_To, actual.Valid_To);
            if (expected.Element_Id != actual.Element_Id) Fail(nameof(AntalKorfalt2.Element_Id), expected.Element_Id, actual.Element_Id);
            if (expected.Feature_Oid != actual.Feature_Oid) Fail(nameof(AntalKorfalt2.Feature_Oid), expected.Feature_Oid, actual.Feature_Oid);
            if (expected.Start_Measure != actual.Start_Measure) Fail(nameof(AntalKorfalt2.Start_Measure), expected.Start_Measure, actual.Start_Measure);
            if (expected.End_Measure != actual.End_Measure) Fail(nameof(AntalKorfalt2.End_Measure), expected.End_Measure, actual.End_Measure);
            if (expected.Updated != actual.Updated) Fail(nameof(AntalKorfalt2.Updated), expected.Updated, actual.Updated);
            if (expected.Körfältsantal != actual.Körfältsantal) Fail(nameof(AntalKorfalt2.Körfältsantal), expected.Körfältsantal, actual.Körfältsantal);
            if (expected.Körfält_I_Vägens_Framriktning != actual.Körfält_I_Vägens_Framriktning) Fail(nameof(AntalKorfalt2.Körfält_I_Vägens_Framriktning), expected.Körfält_I_Vägens_Framriktning, actual.Körfält_I_Vägens_Framriktning);
            if (expected.Körfält_I_Vägens_Bakriktning != actual.Körfält_I_Vägens_Bakriktning) Fail(nameof(AntalKorfalt2.Körfält_I_Vägens_Bakriktning), expected.Körfält_I_Vägens_Bakriktning, actual.Körfält_I_Vägens_Bakriktning);
            if (expected.Deleted != actual.Deleted) Fail(nameof(AntalKorfalt2.Deleted), expected.Deleted, actual.Deleted);
            if (expected.ModifiedTime != actual.ModifiedTime) Fail(nameof(AntalKorfalt2.ModifiedTime), expected.ModifiedTime, actual.ModifiedTime);
            if (expected.Geometry.WKTSWEREF != actual.Geometry.WKTSWEREF) Fail("Geometry.WKTSWEREF", expected.Geometry.WKTSWEREF, actual.Geometry.WKTSWEREF);
            if (expected.Geometry.WKTWGS84 != actual.Geometry.WKTWGS84) Fail("Geometry.WKTWGS84", expected.Geometry.WKTWGS84, actual.Geometry.WKTWGS84);
        }

        [Benchmark]
        public byte[] Json_Serialize()
        {
            using var ms = new MemoryStream(_jsonCapacity);
            JsonSerializer.Serialize(ms, data);
            return ms.ToArray();
        }

        [Benchmark]
        public Root Json_Deserialize()
        {
            using var ms = new MemoryStream(jsonBytes);
            return JsonSerializer.Deserialize<Root>(ms)!;
        }

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
