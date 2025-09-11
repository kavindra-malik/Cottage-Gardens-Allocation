using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class Metrics
    {

        public int QtyDelivered { get; set; }
        public int QtySold{ get; set; }

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
    }
}
