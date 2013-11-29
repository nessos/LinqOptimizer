using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqOptimizer.Core;
using LinqOptimizer.CSharp;


using System.Xml.Linq;
using LinqOptimizer.Base;

namespace LinqOptimizer.Tests
{
    public class Program
    {

        public static void Main(string[] args)
        {
            //var r = new OptiReader();

            //r.SearchVideos("", "", "", "");
            var tests = new QueryTests();
            tests.ListSource();

        }

        static void Measure(Action action)
        {
            var watch = new Stopwatch();
            watch.Start();
            action();
            Console.WriteLine(watch.Elapsed);
        }
    }

    public class Entity
    {
        public string Property { get; set; }
        public string Property1 { get; set; }
        public IList<string> Property2 { get; set; }

    }

    public class OptiReader
    {
        private XDocument _document = XDocument.Parse(@"<xmlelement></xmlelement>");

        public OptiReader() { }

        public List<Entity> SearchVideos(string search1, string search2, string search3, string search4)
        {
            List<Entity> entities = (from elem in _document.Descendants("xmlelement").Descendants("xmlelement").AsParallelQueryExpr()
                                    from vdbt in elem.Descendants("xmlelement")
                                               where Search(search2, search3, search4, search1, elem, vdbt)
                                               select new Entity
                                               {
                                                   Property = elem.Descendants("xmlelement").Descendants("xmlelement").First().Value.Split('#').First()
                                               })
                                               .OrderBySearch(search2, search3, search1).Run() // Break during the Run() method
                                               .ToList();
            return entities;
        }

        private static List<string> Retrieve1(string search1, XElement vdbt)
        {
            return (from elem in vdbt.Descendants("xmlelement")
                    select elem.Descendants("xmlelement").Descendants("xmlelement").First().Value)
                    .OrderBy(a => a.Equals(search1 ?? String.Empty))
                    .ThenBy(a => a.IndexOf(search1 ?? String.Empty))
                    .ThenBy(a => a).ToList();
        }

        private static string Retrieve2(string search2, XElement search_elem)
        {
            return (from elem in search_elem.Descendants("xmlelement").Descendants("xmlement")
                    select elem.Value)
                    .OrderBy(a => a.Equals(search2 ?? String.Empty))
                    .ThenBy(a => a).FirstOrDefault();
        }

        private bool Search(string search1, string search2, string search3, string search4, XElement search_elem, XElement vdbt)
        {
            return (search1 == null || vdbt.Descendants("xmlelement").Descendants("xmlelement").First().Value.Contains(search1))
                    && (search2 == null || (from elem in vdbt.Descendants("xmlelement").Descendants("xmlelement").Descendants("xmlelement") where elem.Value.Contains(search2) select elem).Count() > 0)
                    && (search3 == null || (from elem in search_elem.Descendants("xmlelement").Descendants("xmlelement") where elem.Value.Equals(search3) select elem).Count() > 0);
        }
    }

    public static class ParallelEnumerableDDEXVideoExtension
    {
        public static IParallelQueryExpr<IEnumerable<Entity>> OrderBySearch(this IParallelQueryExpr<IEnumerable<Entity>> entities, string search1, string search2, string search3)
        {
            IParallelQueryExpr<IOrderedEnumerable<Entity>> ordered_entities = null;

            if (!String.IsNullOrWhiteSpace(search1))
            {
                ordered_entities = entities
                    .OrderByDescending(v => (v.Property1 ?? String.Empty).Equals(search1))
                    .ThenBy(v => (v.Property1 ?? String.Empty).StartsWith(search1))
                    .ThenBy(v => (v.Property1 ?? String.Empty).IndexOf(search1))
                    .ThenBy(v => (v.Property1 ?? String.Empty));
            }

            if (!String.IsNullOrWhiteSpace(search2))
            {
                if (ordered_entities == null)
                    ordered_entities = entities.OrderByDescending(v => (v.Property2.FirstOrDefault() ?? String.Empty).Equals(search2));
                else
                    ordered_entities = ordered_entities.ThenByDescending(v => (v.Property2.FirstOrDefault() ?? String.Empty).Equals(search2));

                ordered_entities = ordered_entities
                        .ThenBy(v => (v.Property2.FirstOrDefault() ?? String.Empty).IndexOf(search2))
                        .ThenBy(v => (v.Property2.FirstOrDefault() ?? String.Empty));
            }

            if (ordered_entities == null)
                ordered_entities = entities.OrderBy(v => v.Property);
            else
                return ordered_entities.ThenBy(v => v.Property).Select(m => m);
            return ordered_entities;
        }
    }
}
