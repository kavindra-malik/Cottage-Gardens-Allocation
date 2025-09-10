using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cottage_Gardens_Analysis
{
    public class Metrics
    {
        private double _DollarDelivered;
        private double _DollarSales; 
        private double _Weight;
        public double Index { get; set; }


        public Metrics(double delivered = 0, double sales = 0, double weight = 0) 
        {
            _DollarDelivered = delivered * weight;
            _DollarSales = sales * weight;
            _Weight = weight;
        }

        public void Add(double delivered, double sales, double weight)
        {
            _DollarDelivered += delivered * weight;
            _DollarSales += sales * weight;
            _Weight += weight;
        }

        public double DollarDelivered
        {
            get { return _Weight > 0 ? _DollarDelivered / _Weight : 0; }
        }

        public double DollarSales
        {
            get { return _Weight > 0 ? _DollarSales / _Weight : 0; }
        }

        public double CompositeMetric 
        { 
            get
            {
                return _Weight > 0 ? (_DollarDelivered + _DollarSales) / (2 * _Weight) : 0;
            }
        }
    }
}
