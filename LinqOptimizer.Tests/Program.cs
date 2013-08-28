using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LinqOptimizer.Tests
{
    class Program
    {
        
        public static void Main(string[] args)
        {
            LinqTests tests = new LinqTests();
            tests.TestSelect();
        }
    }
}
