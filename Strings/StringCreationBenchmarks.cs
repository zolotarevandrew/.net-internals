using System;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Strings
{
    [MemoryDiagnoser]
    public class StringCreationBenchmarks
    {
        [Benchmark]
        public string SimpleConcat()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";
            var str3 = 15;

            //boxing
            return string.Concat(str1, str2, str3);
        }

        [Benchmark]
        public string StringBuilder()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";
            var str3 = 15;

            var builder = new StringBuilder();
            builder.Append(str1);
            builder.Append(str2);
            builder.Append(str3);

            return builder.ToString();
        }

        [Benchmark]
        public string Interpolation()
        {
            var str1 = "MySrt1";
            var str2 = "MySrt2";
            var str3 = 15;
            
            return $"{str1} {str2} {str3}";
        }

        public static void Run()
        {
            BenchmarkRunner.Run(typeof(StringCreationBenchmarks).Assembly);
        }
    }
}