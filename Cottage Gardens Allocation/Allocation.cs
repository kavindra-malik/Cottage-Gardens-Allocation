using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Allocation
{
    public class Allocation
    {
        public int Qty { get; set; }
        public decimal Dollars { get; set; }

        public Allocation(int qty, decimal dollars)
        {
            this.Qty = qty;
            this.Dollars = dollars;
        }

        public Allocation(Allocation other)
        {
            this.Qty = other.Qty;
            this.Dollars = other.Dollars;
        }


        public void Add(Allocation other)
        {
            this.Qty += other.Qty;
            this.Dollars += other.Dollars;
        }

        public static string Header
        {
            get
            {
                return ",Suggested Allocation Units, Suggested Allocation Dollars";
            }
        }

        public string Detail
        {
            get
            {
                return "," + Qty + "," + Dollars;
            }
        }

        public static string NullDetail
        {
            get
            {
                return ",,";
            }
        }
    }
}
