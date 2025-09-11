using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cottage_Gardens_Analysis.Program;

namespace Cottage_Gardens_Analysis
{
    public class DoNotShipSpec
    {
        public SpecLevel SpecificationLevel { get; set; }
        public string Id { get; set; }
        public HashSet<Store> Stores { get; set; }

        public DoNotShipSpec(SpecLevel specificationLevel, string id)
        {
            SpecificationLevel = specificationLevel;
            Id = id;
            Stores = new HashSet<Store>();
        }
        
        public void AddStore(Store store)
        {
            Stores.Add(store);
        }
    }
}
