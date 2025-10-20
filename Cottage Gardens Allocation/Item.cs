using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;



namespace Cottage_Gardens_Allocation
{
    public class Item : IEquatable<Item>
    {
        public enum ConfidenceLevel { No, Low, Medium, High }
        public string Nbr { get; set; }
        public string Desc { get; set; }
        public string Size { get; set; }
        public GenusSize GenusSize { get; set; }
        public Group Group { get; set; }
        public string Tag { get; set; }
        public string ItemProgram { get; set; }
        public byte Zone { get; set; }
        public int Multiple { get; set; }
        private decimal _RetailPrice;
        public Dictionary<Store, Metrics>[] History { get; set; }
        public Dictionary<Store, Metrics> Benchmark { get; set; }
        public HashSet<Store> DoNotShip { get; set; }
        public Dictionary<Store, Allocation> Allocations { get; set; }
        public bool InsufficientHistory { get; set; }
        public bool[] ValidHistory { get; set; }
        private HashSet<Store> _TargetStoreSet;


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
            ValidHistory = new bool[Program.HistoryYears.Length];
            _RetailPrice = -1;
        }

        public void UpdateDoNotShip(HashSet<Store> doNotShip)
        {
            if (DoNotShip == null)
            {
                DoNotShip = doNotShip;
            }
        }

        public void AssessHistory()
        {
            for (int i = 0; i < Program.HistoryYears.Length; i++)
            {
                if (History[i] != null)
                {
                    var validData = from x in History[i] where TargetStoreSet.Contains(x.Key) && !x.Value.Ignore && x.Value.Valid && (DoNotShip == null || !DoNotShip.Contains(x.Key)) select x;
                    Debug.WriteLine(validData.Count());
                    ValidHistory[i] = validData.Count() >= 0.75 * Benchmark.Count;
                }
            }
        }

        public double ItemHistoryWeight
        {
            get
            {

                double weight = 0;
                for (int i = 0; i < Program.HistoryYears.Length; i++)
                {
                    if (ValidHistory[i])
                    {
                        weight += 0.11;
                    }
                }
                return weight;
            }
        }

        public void Allocate(AllocationIndex index)
        {
            Allocations = new Dictionary<Store, Allocation>();
            if (Nbr == "13HEM3SDO")
            {
                Debug.WriteLine("Stop");
                foreach (var kvp in Benchmark)
                {
                    Debug.WriteLine(kvp.Key.Nbr + "," + kvp.Value.Ignore + "," + (DoNotShip == null));
                }

            }
            AssessHistory();
            if (TargetStoreSet != null && TargetStoreSet.Count > 0)
            {
                double itemWeight = ItemHistoryWeight;
                Dictionary<Store, double> storeIndex = Program.Projection(index.Index, TargetStoreSet);

                if (itemWeight > 0)
                {
                    Dictionary<Store, double> itemIndex = ItemIndex;
                    storeIndex = Program.CombineIndex(itemIndex, storeIndex, itemWeight);
                }

                // Allocate 
                Dictionary<Store, int> allocations = new Dictionary<Store, int>();
                int totalQty = TotalQty;
                List<StoreResidual> residuals = new List<StoreResidual>();
                int totalAllocated = 0;
                foreach (KeyValuePair<Store, double> kvp in storeIndex)
                {
                    int floor = Multiple * (int)Math.Floor(totalQty * kvp.Value / Multiple);
                    double residual = totalQty * kvp.Value - floor;
                    allocations.Add(kvp.Key, floor);
                    totalAllocated += floor;
                    residuals.Add(new StoreResidual(kvp.Key, residual));
                }
                if (totalAllocated < TotalQty)
                {
                    residuals.Sort();

                    for (int i = 0; i < residuals.Count; i++)
                    {
                        allocations[residuals[i].Store] += Multiple;
                        totalAllocated += Multiple;
                        if (totalAllocated == totalQty)
                        {
                            break;
                        }
                    }
                }
                foreach (var kvp in allocations)
                {
                    Allocations.Add(kvp.Key, new Allocation(kvp.Value, Math.Round(RetailPrice * kvp.Value, 2)));
                }

            }
        }

        public Dictionary<Store, double> ItemIndex
        {
            get
            {
                Dictionary<Store, double> combinedIndex = null;
                int count = 0;
                for (int i = 0; i < Program.HistoryYears.Length; i++) 
                {
                    if (ValidHistory[i])
                    {
                        double sum = 0;
                        Dictionary<Store, double> index = new Dictionary<Store, double>();
                        foreach (var kvp in History[i].Where(x => !x.Value.Ignore && TargetStoreSet.Contains(x.Key)))
                        {
                            index.Add(kvp.Key, kvp.Value.Performance);
                            sum += kvp.Value.Performance;
                        }
                        if (index.Count > 0)
                        {
                            foreach (var x in new List<Store>(index.Keys))
                            {
                                index[x] /= sum;
                            }
                            if (combinedIndex == null)
                            {
                                combinedIndex = index;
                                count++;
                            }
                            else
                            {
                                double weight = count == 1 ? 0.5 : 0.67;
                                combinedIndex = Program.CombineIndex(combinedIndex, index, weight);
                            }
                        }
                    }
                }
                return combinedIndex;
            }
        }

        public HashSet<Store> TargetStoreSet
        {
            get
            {
                if (_TargetStoreSet == null)
                {
                    _TargetStoreSet = new HashSet<Store>();
                    if (Benchmark != null)
                    {
                        foreach (var kvp in Benchmark)
                        {
                            if ((DoNotShip == null || !DoNotShip.Contains(kvp.Key)) && !kvp.Value.Ignore)
                            {
                                _TargetStoreSet.Add(kvp.Key);
                            }
                        }
                    }
                }

                return _TargetStoreSet;
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
                return ", Eligible To Ship, Suggested Allocation Units, Suggested Allocation Dollars";
            }
        }

        public string AllocationDetail(Store store, Allocation allocation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(",");
            sb.Append( DoNotShip == null || !DoNotShip.Contains(store));
            sb.Append(",");
            sb.Append(allocation.Qty);
            sb.Append(",");
            sb.Append(allocation.Dollars);
            return sb.ToString();
        }

        public string AllocationDetail()
        {
            return ",,,";
        }

        public void Output()
        {
            if (Benchmark != null)
            {
                foreach (var kvp in Benchmark)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(kvp.Key.Detail);
                    sb.Append(ItemDetail);
                    sb.Append(kvp.Value.BenchmarkDetailShort);
                    if (Allocations != null && Allocations.TryGetValue(kvp.Key, out var allocatedUnits))
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
                    for (int i = 0; i < Program.HistoryYears.Length; i++)
                    {
                        if (Group.History[i] == null)
                        {
                            sb.Append(Metrics.NullHistoryDetail);
                        }
                        else
                        {
                            if (Group.History[i].TryGetValue(kvp.Key, out Metrics metrics))
                            {
                                sb.Append(metrics.HistoryDetail);
                            }
                        }
                    }
                    for (int i = 0; i < Program.HistoryYears.Length; i++)
                    {
                        if (Group.Cat.History[i] == null)
                        {
                            sb.Append(Metrics.NullHistoryDetail);
                        }
                        else
                        {
                            if (Group.Cat.History[i].TryGetValue(kvp.Key, out Metrics metrics))
                            {
                                sb.Append(metrics.HistoryDetail);
                            }
                        }
                    }
                    Program.OutputLine(Program.OutputTypes.ItemStore, sb.ToString());
                }
            }
        }

        public decimal RetailPrice
        {
            get
            {
                if (_RetailPrice < 0)
                {
                    if (Benchmark != null)
                    {
                        foreach (var metric in Benchmark.Values)
                        {
                            if (metric.QtyDelivered > 0 && metric.DollarDelivered > 0)
                            {
                                _RetailPrice = (decimal)Math.Round(metric.DollarDelivered / metric.QtyDelivered, 2);
                                break;
                            }
                        }
                    }
                    else
                    {
                        _RetailPrice = 0;
                    }
                }
                return _RetailPrice;
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
