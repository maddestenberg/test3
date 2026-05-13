using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using test3.Benchmarks;

var resultsDir = Path.GetFullPath("Results");
var csvPath = Path.Combine(resultsDir, "raw-iterations.csv");
Environment.SetEnvironmentVariable("BENCH_RESULTS_DIR", resultsDir);
var config = DefaultConfig.Instance.AddExporter(new RawCsvExporter(csvPath));
BenchmarkRunner.Run<SerializationBenchmarks>(config);
