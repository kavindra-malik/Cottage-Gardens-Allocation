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
        public bool Valid
        {
            get
            {
                return (QtyDelivered >= 2 && QtySold > 2) && DollarSold > 20;
            }
        }

        public double Performance
        {
            get
            {
                return DollarDelivered + DollarSold > 0 ? DollarDelivered + DollarSold : 0;
            }
        }

        public double DollarSellThruPercent
        {
            get
            {
                return DollarDelivered > 0 ? Math.Round(100 * DollarSold / DollarDelivered, 2) : 0;
            }
        }

        public double UnitSellThruPercent
        {
            get
            {
                return QtyDelivered > 0 ? Math.Round(100 * (double)QtySold / QtyDelivered, 2) : 0;
            }
        }

        public static string HistoryHeader(string prefix = null)
        {
                StringBuilder sb = new StringBuilder();
                for (int year = 0; year < Program.HistoryYears.Length; year++)
                {
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + (prefix == null? " - Dollar Delivered" : " - " + prefix + ": Dollar Delivered"));
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + (prefix == null ? " - Dollar Sold" : " - " + prefix + ": Dollar Sold"));
                sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + (prefix == null ? " - Dollar SellThru %" : " - " + prefix + ": Dollar SellThru %")); 
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + (prefix == null ? " - Unit SellThru %" : " - " + prefix + ": Unit SellThru %"));
            }
                return sb.ToString();
        }

        public string HistoryDetail
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(',');
                sb.Append(DollarDelivered.ToString());
                sb.Append(',');
                sb.Append(DollarSold.ToString());
                sb.Append(',');
                sb.Append(DollarSellThruPercent);
                sb.Append("%");
                sb.Append(',');
                sb.Append(UnitSellThruPercent);
                sb.Append("%");
                return sb.ToString();
            }
        }

        public static string NullHistoryDetail
        {
            get
            {
                return ",,,,";
            }
        }

        public static string BenchmarkHeaderLong
        {
            get
            {
                return ", Qty Delivered, Dollar Delivered, Dollar Delivered Retail, Qty Sold, Dollar Sold, Dollar Sold Retail, Dollar SellThru %, Unit SellThru %,Record Ignored?";

            }
        }

        public static string BenchmarkHeaderShort
        {
            get
            {
                return ", Qty Delivered, Dollar Delivered, Dollar Delivered Retail, Qty Sold, Dollar Sold, Dollar Sold Retail, Dollar SellThru %, Unit SellThru %";

            }
        }

        public string BenchmarkDetailShort
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(',');
                sb.Append(QtyDelivered.ToString());
                sb.Append(',');
                sb.Append(DollarDelivered.ToString());
                sb.Append(',');
                sb.Append(DollarDeliveredRetail.ToString());

                sb.Append(',');
                sb.Append(QtySold.ToString());
                sb.Append(',');
                sb.Append(DollarSold.ToString());
                sb.Append(',');
                sb.Append(DollarSoldRetail.ToString());

                sb.Append(',');
                sb.Append(DollarSellThruPercent);
                sb.Append("%");

                sb.Append(',');
                sb.Append(UnitSellThruPercent);
                sb.Append("%");

                return sb.ToString();
            }
        }




        public string BenchmarkDetailLong
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(',');
                sb.Append(QtyDelivered.ToString());
                sb.Append(',');
                sb.Append(DollarDelivered.ToString());
                sb.Append(',');
                sb.Append(DollarDeliveredRetail.ToString());

                sb.Append(',');
                sb.Append(QtySold.ToString());
                sb.Append(',');
                sb.Append(DollarSold.ToString());
                sb.Append(',');
                sb.Append(DollarSoldRetail.ToString());

                sb.Append(',');
                sb.Append(DollarSellThruPercent);
                sb.Append("%");

                sb.Append(',');
                sb.Append(UnitSellThruPercent);
                sb.Append("%");

                sb.Append(',');
                sb.Append(Ignore);

                return sb.ToString();
            }
        }

        public static string NullBenchmarkDetailShort 
        {
            get
            {
                return ",,,,,,,";
            }
        }


        public static string NullBenchmarkDetailLong
        {
            get
            {
                return ",,,,,,,,";
            }
        }
    }
}
