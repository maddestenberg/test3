using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProtoBuf;
using System.Text.Json;
using System.Xml.Serialization;
using test3.Models;

namespace test3
{
    [MemoryDiagnoser]
    public class SerializationBenchmarks
    {
        private Root data;

        private string jsonString;
        private string xmlString;
        private byte[] protobufData;

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
                RESULT = jsonRoot.RESPONSE.RESULT
            };

            xmlString = File.ReadAllText("data/testdata3.xml");

            using var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            protobufData = ms.ToArray();
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
                RESULT = jsonRoot.RESPONSE.RESULT
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
            return (Root)xmlSerializer.Deserialize(reader);
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

    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<SerializationBenchmarks>();
        }
    }
}