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
            Allocations = new Dictionary<Store, Allocation>();
            RankAllocations = new Dictionary<string, Allocation>();
        }

        public void AddBenchmark(Dictionary<Store, Metrics> benchmark)
        {
            if (Benchmark == null)
            {
                Benchmark = new Dictionary<Store, Metrics>();
            }
            if (RankBenchmark == null)
            { 
                RankBenchmark = new Dictionary<string, Metrics>();
            }
            if (benchmark != null)
            {
                foreach (var kvp in benchmark.Where(x => !x.Value.Ignore))
                {
                    if (!Benchmark.ContainsKey(kvp.Key))
                    {
                        Benchmark[kvp.Key] = new Metrics(kvp.Value);
                    }
                    else
                    {
                        Benchmark[kvp.Key].Add(kvp.Value);
                    }
                    if (!string.IsNullOrWhiteSpace(kvp.Key.Rank))
                    {
                        if (!RankBenchmark.ContainsKey(kvp.Key.Rank))
                        {
                            RankBenchmark[kvp.Key.Rank] = new Metrics(kvp.Value);
                        }
                        else
                        {
                            RankBenchmark[kvp.Key.Rank].Add(kvp.Value);
                        }
                    }
                }
            }
        }

        public void AddHistory(Dictionary<Store, Metrics>[] history)
        {
            if (History == null)
            {
                History = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
            }
            if (RankHistory == null)
            { 
                RankHistory = new Dictionary<string, Metrics>[Program.HistoryYears.Length];
            }
            if (history != null)
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
                            if (!string.IsNullOrWhiteSpace(kvp.Key.Rank))
                            {
                                if (RankHistory[i] == null)
                                {
                                    RankHistory[i] = new Dictionary<string, Metrics>();
                                }
                                if (!RankHistory[i].ContainsKey(kvp.Key.Rank))
                                {
                                    RankHistory[i][kvp.Key.Rank] = new Metrics(kvp.Value);
                                }
                                else
                                {
                                    RankHistory[i][kvp.Key.Rank].Add(kvp.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddAllocations(Dictionary<Store, Allocation> allocations)
        {
            if (Allocations == null || RankAllocations == null)
            {
                Allocations = new Dictionary<Store, Allocation>();
                RankAllocations = new Dictionary<string, Allocation>();
            }
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
                    if (!string.IsNullOrWhiteSpace(kvp.Key.Rank))
                    {
                        if (!RankAllocations.ContainsKey(kvp.Key.Rank))
                        {
                            RankAllocations[kvp.Key.Rank] = new Allocation(kvp.Value);
                        }
                        else
                        {
                            RankAllocations[kvp.Key.Rank].Add(kvp.Value);
                        }
                    }
                }
            }
        }



    }
}
