using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class StoreResidual : IComparable<StoreResidual>
    {
        public Store Store { get; set; }
        public double Residual { get; set; }
        public StoreResidual(Store store, double residual) 
        { 
            this.Store = store;
            this.Residual = residual;
        }

        int IComparable<StoreResidual>.CompareTo(StoreResidual other)
        {
            if (other == null) return 1;

            int comparison = other.Residual.CompareTo(this.Residual); // Want to sort descending 
            if (comparison != 0)
            {
                return comparison;
            }
            return this.Store.Nbr.CompareTo(other.Store.Nbr);
        }
    }
}
