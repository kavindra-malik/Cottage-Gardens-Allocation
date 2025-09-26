using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class AllocationIndex
    {
        public Dictionary<Store, double> Index {get; set;}
        public AllocationIndex()
        {
            Index = new Dictionary<Store, double>();
        }

        public AllocationIndex(int yearIndex, IEnumerable<Item> items,HashSet<Store> allocationSet = null, HashSet<Item> excludeItems = null, Dictionary<Store, double> preallocatedIndex = null, double totalIndex = 1)
        {
            Index = new Dictionary<Store, double>();
            bool debug = allocationSet == null;
            double sum = 0;
            foreach (var item in items.Where(x => x.History[yearIndex] != null && (excludeItems == null || !excludeItems.Contains(x))))
            {
                foreach (var kvp in item.History[yearIndex].Where(x => !x.Value.Ignore && (preallocatedIndex == null || !preallocatedIndex.ContainsKey(x.Key)) && (allocationSet == null || allocationSet.Contains(x.Key))))
                {
                    if (!Index.TryGetValue(kvp.Key, out double value))
                    {
                        Index.Add(kvp.Key, kvp.Value.Performance);
                        sum += kvp.Value.Performance;
                    }
                    else
                    {
                        Index[kvp.Key] += kvp.Value.Performance;
                        sum += kvp.Value.Performance;
                    }
                }
            }

            if (Index.Count > 0)
            {
                foreach (var x in new List<Store>(Index.Keys))
                {
                    Index[x] *= totalIndex / sum;
                }
                if (preallocatedIndex != null)
                {
                    foreach (var kvp in preallocatedIndex)
                    {
                        Index.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        public AllocationIndex(AllocationIndex index1, AllocationIndex index2, double index1Weight = 0.5)
        {
            Index = Program.CombineIndex(index1.Index, index2.Index, index1Weight);
        }

        public AllocationIndex(AllocationIndex index, HashSet<Store> allocationSet)
        {
            Index = Program.Projection(index.Index, allocationSet);
        }

        public void AddPreAallocated(Dictionary<Store, double> keyValuePairs)
        {
            foreach (var kvp in keyValuePairs)
            {
                if (!Index.ContainsKey(kvp.Key))
                {
                    Index.Add(kvp.Key, kvp.Value);
                }
            }
            NormalizeIndex();
        }

        public void NormalizeIndex(double totalIndex = 1)
        {
            double sum = 0;
            foreach (var x in Index.Keys)
            {
                sum += Index[x];
            }
            foreach (var x in new List<Store>(Index.Keys))
            {
                Index[x] *= totalIndex / sum;
            }
        }
    }
}
