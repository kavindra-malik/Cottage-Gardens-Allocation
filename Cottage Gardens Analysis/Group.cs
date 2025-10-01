using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class Group : IEquatable<Group>
    {
        public Category Cat { get; set; }
        public string Name { get; set; }
        public bool[] HasHistory { get; set; }
        public bool HasBenchmark { get; set; }
        public Dictionary<Store, Metrics>[] History { get; set; }
        public Dictionary<Store, Metrics> Benchmark { get; set; }
        public Dictionary<Store, Allocation> Allocations { get; set; }


        public Dictionary<string, Item> Items { get; set; }

        public Group(Category cat, string name)
        {
            Cat = cat;
            Name = name;
            HasHistory = new bool[Program.HistoryYears.Length];
            HasBenchmark = false;
            Items = new Dictionary<string, Item>();
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            foreach (var item in Items.Values)
            {
                item.UpdateDoNotShip(doNotShip);
            }
        }

        #region
        public void AllocateGroupItems()
        {
            HashSet<Store> allocationSet = new HashSet<Store>();
            foreach (var item in Items.Values.Where(x => x.TotalQty > 0))
            {
                allocationSet.UnionWith(item.TargetStoreSet);
            }
            if (allocationSet.Count > 0)
            {
                Dictionary<Store, DoNotShipItems> dnsItemsByStore = new Dictionary<Store, DoNotShipItems>();

                foreach (var item in Items.Values.Where(x => x.DoNotShip != null))
                {
                    IEnumerable<Store> storeDns = allocationSet.Intersect(item.DoNotShip);
                    foreach (Store store in storeDns)
                    {
                        if (!dnsItemsByStore.TryGetValue(store, out var doNotShip))
                        {
                            doNotShip = new DoNotShipItems();
                            dnsItemsByStore.Add(store, doNotShip);
                        }
                        doNotShip.AddItem(item);
                    }
                }
                InitHistoryAndBenchmark();
                AllocationIndex index = GetCompositeIndex(dnsItemsByStore, allocationSet);

                foreach (var item in Items.Values.Where(x => x.TotalQty > 0 && x.TargetStoreSet.Count > 0))
                {
                    if (index != null)
                    {
                        item.Allocate(index);
                    }
                }

            }
        }
        #endregion



        private AllocationIndex GetCompositeIndex(Dictionary<Store, DoNotShipItems> dnsItemsByStore, HashSet<Store> allocationSet)
        {
            double groupHistoryWeight = 0;
            AllocationIndex currentIndex = null;
            for (int i = Program.HistoryYears.Length - 1; i >= 0; i--)
            {
                if (HasHistory[i])
                {
                    AllocationIndex index = AllocateYear(i, dnsItemsByStore, allocationSet);
                    if (currentIndex == null)
                    {
                        currentIndex = index;
                    }
                    else
                    {
                        currentIndex = new AllocationIndex(currentIndex, index);
                    }
                }
                groupHistoryWeight += i > 0 ? 0.2 : 0.4;
            }
            AllocationIndex categoryIndex = Cat.Index;
            categoryIndex = new AllocationIndex(categoryIndex, allocationSet); 
            if (currentIndex == null)
            {
                return categoryIndex;
            }
            return new AllocationIndex(currentIndex, categoryIndex, groupHistoryWeight);
        }

        public AllocationIndex AllocateYear(int index, Dictionary<Store, DoNotShipItems> dnsItemsByStore, HashSet<Store> allocationSet)
        {
            if (dnsItemsByStore.Count > 0)
            {
                Dictionary<Store, double> preallocatedIndex = new Dictionary<Store, double>();
                double totalIndex = 1;
                List<Store> mostConstrainedStores = GetMostConstrainedStores(index, dnsItemsByStore, allocationSet, preallocatedIndex);
                while (mostConstrainedStores != null && mostConstrainedStores.Count > 0)
                {
                    AllocationIndex allocationIndex = new AllocationIndex(index, Items.Values, allocationSet, dnsItemsByStore[mostConstrainedStores.FirstOrDefault()].Items, preallocatedIndex, totalIndex);
                    int count = 0;
                    foreach (Store store in mostConstrainedStores)
                    {
                        if (allocationIndex.Index.TryGetValue(store, out double value))
                        {
                            preallocatedIndex.Add(store, value);
                            totalIndex -= value;
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        mostConstrainedStores = GetMostConstrainedStores(index, dnsItemsByStore, allocationSet, preallocatedIndex);
                    }
                    else
                    {
                        if (preallocatedIndex != null &&  preallocatedIndex.Count > 0)
                        {
                            allocationIndex.AddPreAallocated(preallocatedIndex);
                        }
                        return allocationIndex;
                    }
                }
                return new AllocationIndex(index, Items.Values, allocationSet, null, preallocatedIndex, totalIndex);
            }
            else
            {
                return new AllocationIndex(index, Items.Values, allocationSet);
            }
        }

        public List<Store> GetMostConstrainedStores(int index, Dictionary<Store, DoNotShipItems> dnsItemsByStore, HashSet<Store> allocationSet, Dictionary<Store,double> frozenStores = null)
        {
            if (dnsItemsByStore.Count > 0)
            {
                List<Store> mostConstrainedStores = new List<Store>();
                double maxWeight = 0;
                foreach (var kvp in dnsItemsByStore.Where(x => allocationSet.Contains(x.Key) && (frozenStores == null || !frozenStores.ContainsKey(x.Key))))
                {
                    if (kvp.Value.Weight[index] > 0)
                    {
                        if (kvp.Value.Weight[index] > maxWeight)
                        {
                            maxWeight = kvp.Value.Weight[index];
                            mostConstrainedStores = new List<Store>() { kvp.Key };
                        }
                        else if (kvp.Value.Weight[index] + 0.0000001 >= maxWeight && kvp.Value.Items.SetEquals(dnsItemsByStore[mostConstrainedStores.FirstOrDefault()].Items))
                        {
                            mostConstrainedStores.Add(kvp.Key);
                        }
                    }
                }
                return mostConstrainedStores;
            }
            return null;
        }

        public void InitHistoryAndBenchmark()
        {
            History  = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
            foreach (Item item in Items.Values)
            {
                if (item.Benchmark != null && item.TotalQty > 0)
                {
                    for (int i = 0; i < Program.HistoryYears.Length; i++)
                    {
                        if (item.History[i] != null)
                        {
                            foreach (var kvp in item.History[i].Where(x => !x.Value.Ignore))
                            {
                                if (History [i] == null)
                                {
                                    History [i] = new Dictionary<Store, Metrics>();
                                }
                                if (!History [i].ContainsKey(kvp.Key))
                                {
                                    History [i][kvp.Key] = new Metrics(kvp.Value);
                                }
                                else
                                {
                                    History[i][kvp.Key].Add(kvp.Value);
                                }
                            }
                        }
                    }

                    foreach (var kvp in item.Benchmark.Where(x => !x.Value.Ignore))
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
            }
        }

        public void InitAllocation()
        {
            Allocations = new Dictionary<Store, Allocation>();
            foreach (Item item in Items.Values)
            {
                if (item.Allocations != null)
                {
                    foreach (var kvp in item.Allocations)
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


        public static string GroupHeader
        {
            get
            {
                return ",Group, Category";
            }
        }

        public string GroupDetail
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(",");
                sb.Append(Name);
                sb.Append(",");
                sb.Append(Cat.Name); 
                return sb.ToString();
            }
        }

        public static string AllocationHeader
        {
            get
            {
                return ",Suggested Allocation Units, Suggested Allocation Dollars";
            }
        }

        public void Output()
        {
            InitAllocation();
            if (Benchmark != null)
            {
                foreach (var kvp in Benchmark)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(kvp.Key.StoreDetail);
                    sb.Append(GroupDetail);
                    sb.Append(kvp.Value.GroupBenchmarkDetail);
                    if (Allocations != null && Allocations.TryGetValue(kvp.Key, out var allocation))
                    {
                        sb.Append(",");
                        sb.Append(allocation.Qty);
                        sb.Append(",");
                        sb.Append(allocation.Dollars);
                    }
                    else
                    {
                        sb.Append(",,");
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
                    Program.OutputGroupAllocation(sb.ToString());
                }
            }
        }


        public bool Equals(Group other)
        {
            return this.Cat.Equals(other.Cat) && this.Name.Equals(other.Name);
        }
    }
}
