using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using static Cottage_Gardens_Allocation.Program;

namespace Cottage_Gardens_Allocation
{
    public class Dashboard
    {
        public enum Sections { Rank, Region, Buyer };
        public static string[] Cols = new string[]
        {
            "Actual " + LY + " Allocations %",
            "Suggested " + TY + " Allocations %",
            "Difference",
            "% Change in Product",
            "Actual " + LY + " Dollars",
            "Suggested " + TY + " Dollars",
            "Difference",
            "% Change in Dollars",
            "Average $ Sell Through \'"+ (LLLY % 100).ToString() + "-" + (LY % 100).ToString(),
            "Average $ Sell Through Percentile \'"+ (LLLY % 100).ToString() + "-" + (LY % 100).ToString()
        };


        public static object[,] Header(Sections section)
        {
            object[,] data = new object[1, Cols.Length + 1];
            data[0, 0] = section.ToString();
            for (int i = 0; i < Cols.Length; i++) 
            {
                data[0, i + 1] = Cols[i];
            }
            return data;
        }

        public static int SectionStart(Sections section)
        {
            switch (section)
            {
                case Sections.Rank: return 0;
                case Sections.Region: return SectionEnd(Sections.Rank) + 2; // Blank Row + 1
                case Sections.Buyer: return SectionEnd(Sections.Region) + 2;  // Blank Row + 1
                default: throw new Exception("Unanticipated Section: " + section.ToString());
            }
        }

        public static int SectionEnd(Sections section)
        {
            switch (section)
            {
                case Sections.Rank: return Ranks.Length + 2; // Header + #(Ranks) + Totals
                case Sections.Region: return SectionStart(Sections.Region) + Regions.Length + 2; // Header + #(Regions) + Totals
                case Sections.Buyer: return SectionStart(Sections.Buyer) + Buyers.Length + 2; // Header + #(Buyers) + Totals
                default: throw new Exception("Unanticipated Section: " + section.ToString());
            }
        }


    }
}
