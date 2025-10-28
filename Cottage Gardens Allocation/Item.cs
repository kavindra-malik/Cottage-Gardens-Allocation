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

        public int TotalAvailableUnits { get; set; }
        public int ReplenishmentUnits { get; set; }
        public Dictionary<Store, Metrics>[] History { get; set; }
        public HashSet<Store> DoNotShip { get; set; }
        public Dictionary<Store, Allocation> InitialAllocations { get; set; }
        public Dictionary<Store, Allocation> Replenishments { get; set; }

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

        public int AllocatableUnits
        {
            get
            {
                return TotalAvailableUnits - ReplenishmentUnits;
            }
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
                    ValidHistory[i] = validData.Count() >= 0.75 * (Program.Stores.Count - DoNotShip.Count);
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
            InitialAllocations = new Dictionary<Store, Allocation>();
            Replenishments = new Dictionary<Store, Allocation>();

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

                // Allocate Initial (allocationType = 0) and Replenishment Units (allocationType = 1)
                for (int allocationType = 0; allocationType < 2; allocationType++)
                {
                    Dictionary<Store, int> allocations = new Dictionary<Store, int>();
                    int totalQty = allocationType == 0 ? AllocatableUnits : ReplenishmentUnits;
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
                    if (totalAllocated < totalQty)
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
                        if (allocationType == 0)
                        {
                            InitialAllocations.Add(kvp.Key, new Allocation(kvp.Value, Math.Round(RetailPrice * kvp.Value, 2)));
                        }
                        else
                        {
                            Replenishments.Add(kvp.Key, new Allocation(kvp.Value, Math.Round(RetailPrice * kvp.Value, 2)));

                        }
                    }
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
                    _TargetStoreSet = new HashSet<Store>(Program.Stores.Values.Except(DoNotShip));
                }

                return _TargetStoreSet;
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
