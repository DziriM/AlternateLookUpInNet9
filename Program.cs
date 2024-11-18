using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AlternateLookUp
{
    public static partial class Program
    {
        static void Main(string[] args)
        {
            string text = File.ReadAllText(@"w:\PrideAndPrejudice.txt");

            Dictionary<string, int> frequency = [];
            var lookup = frequency.GetAlternateLookup<ReadOnlySpan<char>>();

            Stopwatch sw = Stopwatch.StartNew();

            while (true)
            {
                long mem = GC.GetTotalAllocatedBytes();
                sw.Restart();

                for (int trial = 0; trial < 10; trial++)
                {
                    // 1) Matches vs EnumerateMatches (amortized runtime) :
                    // 10x time better (1150ms vs 200ms) - Time-Wise
                    // 10x time better (350mb vs 43mb) - Space-Wise

                    // 2) We are allocating a string each and every time a word is found
                    // The word "the" is maybe found 400 times, but we only need to allocate a string for it once
                    // and look up if it's in the dictionnary for comparison !
                    // So we can use a read only Span of char and use that to index into the dictionnary !
                    // But I can't do that because :
                    // - I have a dictionnary of string
                    // - I can't look up in a dictionnary of string with a read only span of char
                    // I could not until .NET 9 !
                    // That's why we can now use GetAlternateLookup !
                    // A generic parameter can be a ref struct now
                    // Now instead of using the dictionnary, we use the lookup !

                    // Now when we run this, we have still about the same speed
                    // But now we use near 0mb allocated, we are just using less than 1mb at the moment we are allocating the string
                    // Fantastic !!!
                    foreach (var m in Helpers.Words().EnumerateMatches(text))
                    {
                        var word = text.AsSpan(m.Index, m.Length);

                        lookup[word] = lookup.TryGetValue(word, out int count) ? count + 1 : 1;
                    }
                }

                sw.Stop();
                mem = GC.GetTotalAllocatedBytes() - mem;

                Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms, Alloc: {mem / 1024.0 / 1024:N2}mb");
            }
        }

        static partial class Helpers
        {
            [GeneratedRegex(@"\b\w+\b")]
            public static partial Regex Words();

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Use<T>(T value) { }
        }
    }
}
