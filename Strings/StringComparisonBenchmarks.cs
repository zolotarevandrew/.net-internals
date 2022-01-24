using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Strings
{
    [MemoryDiagnoser]
    public class StringComparisonBenchmarks
    {
        [Benchmark]
        public bool SimpleComparison()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";

            return str1 == str2;
        }

        [Benchmark]
        public bool Equals_OrdinalIgnoreCase()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";

            return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
        }

        [Benchmark]
        public bool CompareTo_CurrentCulture()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";

            return str1.CompareTo(str2) == 0;
        }

        [Benchmark]
        public bool StringCompare_OrdinalIgnoreCase()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";

            return string.Compare(str1, str2, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static void Run()
        {
            BenchmarkRunner.Run(typeof(StringComparisonBenchmarks).Assembly);
        }
    }
}