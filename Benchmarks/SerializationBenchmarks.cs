using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using test3.Models;

namespace test3.Benchmarks
{
    [MemoryDiagnoser]
    [SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 30)]
    public class SerializationBenchmarks
    {
        private Root data = new();

        private string jsonString = "";
        private string xmlString = "";
        private byte[] protobufData = Array.Empty<byte>();

        private readonly XmlSerializer xmlSerializer = new(typeof(Root));

        [GlobalSetup]
        public void Setup()
        {
            jsonString = File.ReadAllText("data/testdata3.json");

            var jsonRoot = JsonSerializer.Deserialize<JsonRoot>(
                jsonString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            data = new Root
            {
                RESULT = jsonRoot!.RESPONSE.RESULT
            };

            xmlString = File.ReadAllText("data/testdata3.xml");

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            protobufData = ms.ToArray();

            var objectCount = data.RESULT
                .SelectMany(r => r.AntalKörfält2)
                .Count();

            var jsonPayload = Encoding.UTF8.GetByteCount(jsonString);
            var xmlPayload = Encoding.UTF8.GetByteCount(xmlString);
            var protobufPayload = protobufData.Length;

            Console.WriteLine();
            Console.WriteLine("=== OBJECT COUNT ===");
            Console.WriteLine($"Loaded objects: {objectCount:N0}");

            Console.WriteLine();
            Console.WriteLine("=== PAYLOAD SIZE ===");
            Console.WriteLine($"JSON: {jsonPayload:N0} bytes");
            Console.WriteLine($"XML: {xmlPayload:N0} bytes");
            Console.WriteLine($"PROTOBUF: {protobufPayload:N0} bytes");

            var csvLines = new List<string>
            {
                "Method,Iteration,ElapsedTimeMs,CpuTimeMs,PayloadBytes,ObjectCount"
            };

            Console.WriteLine();
            Console.WriteLine("=== RAW 30 ITERATIONS ===");

            MeasureRaw30("Json_Serialize", Json_Serialize, jsonPayload, objectCount, csvLines);
            MeasureRaw30("Xml_Serialize", Xml_Serialize, xmlPayload, objectCount, csvLines);
            MeasureRaw30("Protobuf_Serialize", Protobuf_Serialize, protobufPayload, objectCount, csvLines);

            MeasureRaw30("Json_Deserialize", Json_Deserialize, jsonPayload, objectCount, csvLines);
            MeasureRaw30("Xml_Deserialize", Xml_Deserialize, xmlPayload, objectCount, csvLines);
            MeasureRaw30("Protobuf_Deserialize", Protobuf_Deserialize, protobufPayload, objectCount, csvLines);

            var resultPath = "/Users/mads/Desktop/SYSTEMVET/T6 SYSTEMVET/examensarbete/test3/Results/raw-iterations.csv";

            Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
            File.WriteAllLines(resultPath, csvLines);

            Console.WriteLine($"Saved CSV to: {resultPath}");
        }

        private static void MeasureRaw30(
            string methodName,
            Func<object?> action,
            int payloadBytes,
            int objectCount,
            List<string> csvLines)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {methodName} ---");

            for (int i = 1; i <= 30; i++)
            {
                var process = Process.GetCurrentProcess();

                var cpuBefore = process.TotalProcessorTime;
                var stopwatch = Stopwatch.StartNew();

                var result = action();

                stopwatch.Stop();
                var cpuAfter = process.TotalProcessorTime;

                GC.KeepAlive(result);

                double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
                double cpuMs = (cpuAfter - cpuBefore).TotalMilliseconds;

                Console.WriteLine(
                    $"Iteration {i}: elapsed={elapsedMs.ToString("F6", CultureInfo.InvariantCulture)} ms, cpu={cpuMs.ToString("F6", CultureInfo.InvariantCulture)} ms"
                );

                csvLines.Add(
                    $"{methodName},{i},{elapsedMs.ToString("F6", CultureInfo.InvariantCulture)},{cpuMs.ToString("F6", CultureInfo.InvariantCulture)},{payloadBytes},{objectCount}"
                );
            }
        }

        [Benchmark]
        public string Json_Serialize()
        {
            var jsonRoot = new JsonRoot
            {
                RESPONSE = new JsonResponse
                {
                    RESULT = data.RESULT
                }
            };

            return JsonSerializer.Serialize(jsonRoot);
        }

        [Benchmark]
        public Root Json_Deserialize()
        {
            var jsonRoot = JsonSerializer.Deserialize<JsonRoot>(
                jsonString,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return new Root
            {
                RESULT = jsonRoot!.RESPONSE.RESULT
            };
        }

        [Benchmark]
        public string Xml_Serialize()
        {
            using var writer = new StringWriter();
            xmlSerializer.Serialize(writer, data);
            return writer.ToString();
        }

        [Benchmark]
        public Root Xml_Deserialize()
        {
            using var reader = new StringReader(xmlString);
            return (Root)xmlSerializer.Deserialize(reader)!;
        }

        [Benchmark]
        public byte[] Protobuf_Serialize()
        {
            using var ms = new MemoryStream();
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