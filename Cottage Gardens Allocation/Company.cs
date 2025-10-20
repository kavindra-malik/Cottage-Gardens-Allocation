using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Cottage_Gardens_Allocation.Program;

namespace Cottage_Gardens_Allocation
{
    public class Company : Aggregate
    {
        public string Name { get; set; }

        public Company(string name) 
        {
            Name = name;
        }

        public void InitHistoryAndBenchmark()
        {
            foreach (var category in Program.Categories.Values)
            {
                foreach (Group group in category.Groups.Values.Where(g => g.HasBenchmark))
                {
                    group.InitHistoryAndBenchmark();
                }
                category.InitHistoryAndBenchmark();
                AddHistory(category.History);
                AddBenchmark(category.Benchmark);
            }
        }

        public void InitAllocations()
        {
            foreach (var category in Program.Categories.Values)
            {
                foreach (Group group in category.Groups.Values.Where(g => g.HasBenchmark))
                {
                    group.InitAllocation();
                }
                category.InitAllocation();
                AddAllocations(category.Allocations);
            }
        }
        public static string Header
        {
            get
            {
                return ",Company";
            }
        }

        public string Detail
        {
            get
            {
                return "," + Name;
            }
        }

        public void Output(OutputTypes outputType)
        {
            switch (outputType)
            {
                case OutputTypes.CompanyStore:
                    if (Benchmark != null)
                    {
                        foreach (var kvp in Benchmark.OrderBy(x => x.Key.Nbr))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(kvp.Key.Detail);
                            sb.Append(Detail);
                            sb.Append(kvp.Value.BenchmarkDetailShort);
                            if (Allocations != null && Allocations.TryGetValue(kvp.Key, out var allocation))
                            {
                                sb.Append(allocation.Detail);
                            }
                            else
                            {
                                sb.Append(Allocation.NullDetail);
                            }
                            for (int i = 0; i < Program.HistoryYears.Length; i++)
                            {
                                if (History[i] == null)
                                {
                                    sb.Append(Metrics.NullHistoryDetail);
                                }
                                else
                                {
                                    if (History[i].TryGetValue(kvp.Key, out Metrics metrics))
                                    {
                                        sb.Append(metrics.HistoryDetail);
                                    }
                                }
                            }
                            Program.OutputLine(outputType, sb.ToString());
                        }
                    }
                    break;
                case OutputTypes.CompanyRank:
                    if (RankBenchmark != null)
                    {
                        foreach (var kvp in RankBenchmark.OrderBy(x => x.Key))
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(kvp.Key);
                            sb.Append(Detail);
                            sb.Append(kvp.Value.BenchmarkDetailShort);
                            if (RankAllocations != null && RankAllocations.TryGetValue(kvp.Key, out var allocation))
                            {
                                sb.Append(allocation.Detail);
                            }
                            else
                            {
                                sb.Append(Allocation.NullDetail);
                            }
                            for (int i = 0; i < Program.HistoryYears.Length; i++)
                            {
                                if (RankHistory[i] == null)
                                {
                                    sb.Append(Metrics.NullHistoryDetail);
                                }
                                else
                                {
                                    if (RankHistory[i].TryGetValue(kvp.Key, out Metrics metrics))
                                    {
                                        sb.Append(metrics.HistoryDetail);
                                    }
                                }
                            }
                            Program.OutputLine(outputType, sb.ToString());
                        }
                    }
                    break;
            }
        }

        public void ProcessOutput(Program.OutputTypes outputType)
        {
            OutputHeader(outputType);
            switch (outputType)
            {
                case Program.OutputTypes.ItemStore:
                    foreach (var category in Program.Categories.Values)
                    {
                        foreach (Group group in category.Groups.Values.Where(g => g.HasBenchmark))
                        {
                            foreach (Item item in group.Items.Values.Where(i => i.Benchmark != null))
                            {
                                item.Output();
                            }
                        }
                    }
                    break;
                case OutputTypes.GroupStore:
                    foreach (var category in Program.Categories.Values)
                    {
                        foreach (Group group in category.Groups.Values.Where(g => g.HasBenchmark))
                        {
                            group.Output();
                        }
                    }
                    break;
                case OutputTypes.CompanyStore:
                    Output(outputType);
                    break;
                case OutputTypes.CompanyRank:
                    Output(outputType);
                    break;
            }
        }
    }


}
