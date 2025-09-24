using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;



namespace Cottage_Gardens_Analysis
{
    public class Item : IEquatable<Item>
    {
        public string Nbr { get; set; }
        public string Desc { get; set; }
        public string Size { get; set; }
        public GenusSize GenusSize { get; set; }
        public Group Group { get; set; }
        public string Tag { get; set; }
        public string ItemProgram { get; set; }
        public byte Zone { get; set; }
        public int Multiple { get; set; }
        public Dictionary<Store, Metrics>[] History { get; set; }
        public Dictionary<Store, Metrics> Benchmark { get; set; }
        public HashSet<Store> DoNotShip { get; set; }
        public Dictionary<Store, int> Allocation { get; set; }
        public bool InsufficientHistory { get; set; }

        private int? _totalQty;
        private double?[] _totalSold = new double?[Program.HistoryYears.Length];

        // Item, Item Description, Size,    Inactive, Category,    Tag Code,    Program, Zone,    GROUP, GENUS SIZE, GENUS
        public Item(string nbr, string desc, string size, GenusSize genusSize, Group group, string tag, string program, byte zone, int multiple)
        {
            Nbr = nbr;
            Desc = desc;
            GenusSize = genusSize;
            Group = group;
            Tag = tag;
            ItemProgram = program;
            Zone = zone;
            Multiple = multiple;
            History = new Dictionary<Store, Metrics>[Program.HistoryYears.Length];
            DoNotShip = null;
            InsufficientHistory = false;
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            if (DoNotShip == null)
            {
                DoNotShip = doNotShip;
            }
        }

        public void Allocate(AllocationIndex index)
        {
            // Store-specific allocation index
            HashSet<Store> targetSet = TargetStoreSet;
            Dictionary<Store, double> storeIndex = index.Index.Where(x => targetSet.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            if (storeIndex.Count < index.Index.Count)
            {
                double sum = 0;
                foreach (KeyValuePair<Store, double> kvp in storeIndex)
                {
                    sum += kvp.Value;
                }
                foreach (Store store in new List<Store>(storeIndex.Keys))
                {
                    storeIndex[store] /= sum;
                }
            }
            // Allocate 
            Allocation = new Dictionary<Store, int>();
            int totalQty = TotalQty;
            List<StoreResidual> residuals = new List<StoreResidual>();
            int totalAllocated = 0;
            foreach (KeyValuePair <Store, double> kvp in storeIndex)
            {
                int floor = Multiple * (int)Math.Floor(totalQty * kvp.Value / Multiple);
                double residual = totalQty * kvp.Value - floor;
                Allocation.Add(kvp.Key, floor);
                totalAllocated += floor;
                residuals.Add(new StoreResidual(kvp.Key, residual));
            }
            if (totalAllocated < TotalQty) 
            {
                residuals.Sort();
                
                for (int i = 0; i < residuals.Count; i++)
                {
                    Allocation[residuals[i].Store] += Multiple;
                    totalAllocated += Multiple;
                    if (totalAllocated == totalQty)
                    {
                        break;
                    }
                }
            }
        }

        public HashSet<Store> TargetStoreSet
        {
            get
            {
                HashSet<Store> set = new HashSet<Store>();
                if (Benchmark != null)
                {
                    foreach (var kvp in Benchmark)
                    {
                        if ((DoNotShip == null || !DoNotShip.Contains(kvp.Key)) && !kvp.Value.Ignore)
                        {
                            set.Add(kvp.Key);
                        }
                    }
                }

                return set;
            }
        }

        public int TotalQty
        {
            get
            {
                if (_totalQty == null)
                {
                    _totalQty = 0;
                    if (Benchmark != null)
                    {
                        foreach (KeyValuePair<Store, Metrics> kvp in Benchmark.Where(x => !x.Value.Ignore))
                        {
                            if ((DoNotShip == null || !DoNotShip.Contains(kvp.Key)) && !kvp.Value.Ignore)
                            {
                                _totalQty += kvp.Value.QtyDelivered;
                            }
                        }
                        int remainder = _totalQty.Value % Multiple;
                        if (Math.Abs(remainder) > Multiple / 2.0) // Use 2.0 for float division
                        {
                            if (remainder > 0)
                            {
                                _totalQty = _totalQty.Value + (Multiple - remainder);
                            }
                            else
                            {
                                _totalQty = _totalQty.Value - (Multiple + remainder);
                            }
                        }
                        // Otherwise, subtract the remainder to get the lower multiple
                        else
                        {
                            _totalQty = _totalQty.Value - remainder;
                        }
                    }
                }
                return _totalQty.Value;
            }
        }

        public double TotalSold(int index)
        {

            if (_totalSold[index] == null)
            {
                if (History[index] != null)
                {
                    _totalSold[index] = 0;
                    foreach (Metrics metrics in History[index].Values)
                    {
                        _totalSold[index] += metrics.DollarSold;
                    }
                }
            }
            return _totalSold[index] == null ? 0 : _totalSold[index].Value;
        }

        public static string ItemHeader
        {
            get
            {
                return ",Item Code,Item Description, Size, Genus, Group,Category,  Zone";
            }
        }

        public string ItemDetail
        {
            get
            {
                return "," + Nbr + "," + Desc + "," + Size + "," + GenusSize.Genus.Name +"," + Group.Name + "," + Group.Cat.Name +  "," + Zone;
            }
        }



        
        public static string AllocationHeader
        {
            get
            {
                return ", Eligible To Ship, Suggested Allocation Units";
            }
        }

        public string AllocationDetail(Store store, int allocationUnits)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(",");
            sb.Append( DoNotShip == null || !DoNotShip.Contains(store));
            sb.Append(",");
            sb.Append(allocationUnits);
            return sb.ToString();
        }

        public string AllocationDetail()
        {
            return ",,";
        }

        public void Output()
        {
            if (Benchmark != null)
            {
                foreach (var kvp in Benchmark)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(kvp.Key.StoreDetail);
                    sb.Append(ItemDetail);
                    sb.Append(kvp.Value.BenchmarkDetail);
                    if (Allocation != null && Allocation.TryGetValue(kvp.Key, out var allocatedUnits))
                    {
                        sb.Append(AllocationDetail(kvp.Key, allocatedUnits));
                    }
                    else
                    {
                        sb.Append(AllocationDetail());
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
                    Program.OutputItemAllocation(sb.ToString());
                }
            }
        }

        public override int GetHashCode()
        {
            return Nbr.GetHashCode();
        }

        public bool Equals(Item other)
        {
            return this.Nbr == other.Nbr;
        }
    }
}
