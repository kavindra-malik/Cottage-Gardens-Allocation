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
            double sum = 0;
            Index = new Dictionary<Store, double>();
            foreach (KeyValuePair<Store, double> kvp in index1.Index)
            {
                if (index2.Index.ContainsKey(kvp.Key))
                {
                    sum += Index[kvp.Key] = kvp.Value * index1Weight + index2.Index[kvp.Key] * (1 - index1Weight);
                }
                else
                {
                    sum += Index[kvp.Key] = 2* kvp.Value;
                }
            }
            var index2NotInIndex1 = from x in index2.Index.Keys where !index1.Index.ContainsKey(x) select x;
            foreach (var x in index2NotInIndex1)
            {
                sum += Index[x] = 2 * index2.Index[x];
            }
            foreach (var x in new List<Store>(Index.Keys))
            {
                Index[x] /= sum;
            }
        }

        public AllocationIndex(AllocationIndex index, HashSet<Store> allocationSet)
        {
            Index = new Dictionary<Store, double>();
            double sum = 0;
            foreach (var kvp in index.Index.Where(k => allocationSet.Contains(k.Key)))
            {
                Index.Add(kvp.Key, kvp.Value);
                sum += kvp.Value;
            }
            foreach (var x in new List<Store>(Index.Keys))
            {
                Index[x] /= sum;
            }
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
