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
                if (DollarDelivered + DollarSold > 50000)
                {
                    Debug.WriteLine("Look : DollarDelivered = " + DollarDelivered + "DollarSold = " + DollarSold);
                }
                return (DollarDelivered + DollarSold) / 2000;
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

        public static string HistoryHeader
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int year = 0; year < Program.HistoryYears.Length; year++)
                {
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Dollar Delivered");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Dollar Sold");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Dollar SellThru %");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Unit SellThru %");
                }
                for (int year = 0; year < Program.HistoryYears.Length; year++)
                {
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Dollar Delivered");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Dollar Sold");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Dollar SellThru %");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Unit SellThru %");
                }
                for (int year = 0; year < Program.HistoryYears.Length; year++)
                {
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Category Dollar Delivered");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Category Dollar Sold");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Category Dollar SellThru %");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Category Unit SellThru %");
                }
                return sb.ToString();
            }
        }

        public static string GroupHistoryHeader
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                for (int year = 0; year < Program.HistoryYears.Length; year++)
                {
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Dollar Delivered");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Dollar Sold");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Dollar SellThru %");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Unit SellThru %");
                }
                for (int year = 0; year < Program.HistoryYears.Length; year++)
                {
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Dollar Delivered");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Dollar Sold");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Dollar SellThru %");
                    sb.Append(',');
                    sb.Append(Program.HistoryYears[year] + " - Group Unit SellThru %");
                }
                return sb.ToString();
            }
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

        public static string BenchmarkHeader
        {
            get
            {
                return ", Qty Delivered, Dollar Delivered, Dollar Delivered Retail, Qty Sold, Dollar Sold, Dollar Sold Retail, Dollar SellThru %, Unit SellThru %,Record Ignored?";

            }
        }

        public static string GroupBenchmarkHeader
        {
            get
            {
                return ", Qty Delivered, Dollar Delivered, Dollar Delivered Retail, Qty Sold, Dollar Sold, Dollar Sold Retail, Dollar SellThru %, Unit SellThru %";

            }
        }

        public string BenchmarkDetail
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


        public static string NullBenchmarkDetail
        {
            get
            {
                return ",,,,,,,,";
            }
        }


        public string GroupBenchmarkDetail
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

        public static string GroupNullBenchmarkDetail
        {
            get
            {
                return ",,,,,,,";
            }
        }
    }
}
