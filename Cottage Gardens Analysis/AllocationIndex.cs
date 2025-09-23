using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

        public AllocationIndex(int yearIndex, Group group,HashSet<Store> allocationSet, HashSet<Item> excludeItems = null, Dictionary<Store, double> preallocatedIndex = null, double totalIndex = 1)
        {
            Index = new Dictionary<Store, double>();

            foreach (var item in group.Items.Values.Where(x => x.History[yearIndex] != null && (excludeItems == null || !excludeItems.Contains(x))))
            {
                foreach (var kvp in item.History[yearIndex].Where(x => (preallocatedIndex == null || !preallocatedIndex.ContainsKey(x.Key)) && allocationSet.Contains(x.Key)))
                {
                    if (!Index.TryGetValue(kvp.Key, out double value))
                    {
                        Index.Add(kvp.Key, kvp.Value.Performance);
                    }
                    else
                    {
                        Index[kvp.Key] += value;
                    }
                }
            }
            if (Index.Count > 0)
            {
                NormalizeIndex(totalIndex);
                if (preallocatedIndex != null)
                {
                    foreach (var kvp in preallocatedIndex)
                    {
                        Index.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        public AllocationIndex(AllocationIndex index1, AllocationIndex index2)
        {
            double sum = 0;
            Index = new Dictionary<Store, double>();
            foreach (KeyValuePair<Store, double> kvp in index1.Index)
            {
                if (index2.Index.ContainsKey(kvp.Key))
                {
                    sum += Index[kvp.Key] = kvp.Value + index2.Index[kvp.Key];
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
