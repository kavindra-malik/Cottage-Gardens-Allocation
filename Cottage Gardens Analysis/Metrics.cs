using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class Metrics
    {

        public int QtyDelivered { get; set; }
        public int QtySold { get; set; }

        public double DollarDelivered { get; set; }
        public double DollarSold { get; set; }

        public double DollarDeliveredRetail { get; set; }
        public double DollarSoldRetail { get; set; }

        public Metrics(int qtyDelivered, int qtySold, double dollarsDelivered, double dollarSold, double dollarsDeliveredRetail, double dollarsSoldRetail)
        {
            QtyDelivered = qtyDelivered;
            QtySold = qtySold;
            DollarDelivered = dollarsDelivered;
            DollarSold = dollarSold;
            DollarDeliveredRetail = dollarsDeliveredRetail;
            DollarSoldRetail = dollarsSoldRetail;
        }

        public Metrics(Metrics original)
        {
            this.QtyDelivered = original.QtyDelivered;
            this.QtySold = original.QtySold;
            this.DollarDelivered = original.DollarDelivered;
            this.DollarSold = original.DollarSold;
            this.DollarDeliveredRetail = original.DollarDeliveredRetail;
            this.DollarSoldRetail = original.DollarSoldRetail;
        }



        public void Add(Metrics other)
        {
            this.QtyDelivered += other.QtyDelivered;
            this.QtySold += other.QtySold;
            this.DollarDelivered += other.DollarDelivered;
            this.DollarSold += other.DollarSold;
            this.DollarDeliveredRetail += other.DollarDeliveredRetail;
            this.DollarSoldRetail += other.DollarSoldRetail;
        }

        public bool Ignore
        {
            get
            {
                return (QtyDelivered >= 20 && QtySold == 0) || QtyDelivered == 0;
            }
        }

        public double Performance
        {
            get
            {
                if (DollarDelivered + DollarSold > 50000)
                {
                    Debug.WriteLine("Look : DollarDelivered = " + DollarDelivered + "DollarSold = "+ DollarSold);
                }
                return (DollarDelivered + DollarSold) / 2000;
            }
        }
    }
}
