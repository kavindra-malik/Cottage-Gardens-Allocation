using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public abstract class Aggregate
    {
        public Dictionary<Store, Metrics>[] History { get; set; }
        public Dictionary<Store, Metrics> Benchmark { get; set; }
        public Dictionary<Store, Allocation> Allocations { get; set; }
        public Dictionary<string, Metrics>[] RankHistory { get; set; }
        public Dictionary<string, Metrics> RankBenchmark { get; set; }
        public Dictionary<string, Allocation> RankAllocations { get; set; }

        public Aggregate() 
        {
            History = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
            Benchmark = new Dictionary<Store, Metrics>();
            Allocations = new Dictionary<Store, Allocation>();
        }

        public void AddBenchmark(IEnumerable<KeyValuePair<Store, Metrics>> benchmark)
        {
            foreach (var kvp in benchmark.Where(x => !x.Value.Ignore))
            {
                if (Benchmark == null)
                {
                    Benchmark = new Dictionary<Store, Metrics>();
                }
                if (!Benchmark.ContainsKey(kvp.Key))
                {
                    Benchmark[kvp.Key] = new Metrics(kvp.Value);
                }
                else
                {
                    Benchmark[kvp.Key].Add(kvp.Value);
                }
            }
        }

        public void AddHistory(IEnumerable<KeyValuePair<Store, Metrics>>[] history)
        {
            for (int i = 0; i < Program.HistoryYears.Length; i++)
            {
                if (history[i] != null)
                {
                    foreach (var kvp in history[i].Where(x => !x.Value.Ignore))
                    {
                        if (History[i] == null)
                        {
                            History[i] = new Dictionary<Store, Metrics>();
                        }
                        if (!History[i].ContainsKey(kvp.Key))
                        {
                            History[i][kvp.Key] = new Metrics(kvp.Value);
                        }
                        else
                        {
                            History[i][kvp.Key].Add(kvp.Value);
                        }
                    }
                }
            }
        }

        public void AddAllocations(IEnumerable<KeyValuePair<Store, Allocation>> allocations)
        {
            if (allocations != null)
            {
                foreach (var kvp in allocations)
                {
                    if (!Allocations.ContainsKey(kvp.Key))
                    {
                        Allocations[kvp.Key] = new Allocation(kvp.Value);
                    }
                    else
                    {
                        Allocations[kvp.Key].Add(kvp.Value);
                    }
                }
            }
        }



    }
}
