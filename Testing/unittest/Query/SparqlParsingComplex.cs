/*
dotNetRDF is free and open source software licensed under the MIT License

-----------------------------------------------------------------------------

Copyright (c) 2009-2012 dotNetRDF Project (dotnetrdf-developer@lists.sf.net)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is furnished
to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Update;

using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Query
{
    [TestClass]
    public class SparqlParsingComplex
    {
        [TestMethod]
        public void SparqlParsingNestedGraphPatternFirstItem()
        {
                SparqlQueryParser parser = new SparqlQueryParser();
                SparqlQuery q = parser.ParseFromFile("childgraphpattern.rq");

                Console.WriteLine(q.ToString());
                Console.WriteLine();
                Console.WriteLine(q.ToAlgebra().ToString());
        }

        [TestMethod]
        public void SparqlParsingNestedGraphPatternFirstItem2()
        {
                SparqlQueryParser parser = new SparqlQueryParser();
                SparqlQuery q = parser.ParseFromFile("childgraphpattern2.rq");

                Console.WriteLine(q.ToString());
                Console.WriteLine();
                Console.WriteLine(q.ToAlgebra().ToString());
         }

        [TestMethod]
        public void SparqlParsingSubQueryWithLimitAndOrderBy()
        {
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");

            String query = "SELECT * WHERE { { SELECT * WHERE {?s ?p ?o} ORDER BY ?p ?o LIMIT 2 } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Object results = g.ExecuteQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                TestTools.ShowResults(rset);

                Assert.IsTrue(rset.All(r => r.HasValue("s") && r.HasValue("p") && r.HasValue("o")), "All Results should have had ?s, ?p and ?o variables");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        //[TestMethod]
        //public void SparqlNonNormalizedUris()
        //{
        //    try
        //    {
        //        //Options.UriNormalization = false;

        //        SparqlQueryParser parser = new SparqlQueryParser();
        //        SparqlRdfParser rdfParser = new SparqlRdfParser(new TurtleParser());

        //        foreach (String file in Directory.GetFiles(Environment.CurrentDirectory))
        //        {
        //            if (Path.GetFileName(file).StartsWith("normalization") && Path.GetExtension(file).Equals(".ttl") && !Path.GetFileName(file).EndsWith("-results.ttl"))
        //            {
        //                QueryableGraph g = new QueryableGraph();
        //                FileLoader.Load(g, file);

        //                Console.WriteLine("Testing " + Path.GetFileName(file));

        //                SparqlQuery query = parser.ParseFromFile(Path.GetFileNameWithoutExtension(file) + ".rq");

        //                Object results = g.ExecuteQuery(query);
        //                if (results is SparqlResultSet)
        //                {
        //                    SparqlResultSet rset = (SparqlResultSet)results;

        //                    SparqlResultSet expected = new SparqlResultSet();
        //                    rdfParser.Load(expected, Path.GetFileNameWithoutExtension(file) + "-results.ttl");

        //                    if (!rset.Equals(expected))
        //                    {
        //                        Console.WriteLine("Expected Results");
        //                        Console.WriteLine();
        //                        foreach (SparqlResult r in expected)
        //                        {
        //                            Console.WriteLine(r.ToString());
        //                        }
        //                        Console.WriteLine();
        //                        Console.WriteLine("Actual Results");
        //                        Console.WriteLine();
        //                        foreach (SparqlResult r in rset)
        //                        {
        //                            Console.WriteLine(r.ToString());
        //                        }
        //                        Console.WriteLine();
        //                    }
        //                    Assert.AreEqual(rset, expected, "Result Sets should be equal");
        //                }
        //                else
        //                {
        //                    Assert.Fail("Didn't get a SPARQL Result Set as expected");
        //                }
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        //Options.UriNormalization = true;
        //    }
        //}

        [TestMethod]
        public void SparqlParsingDescribeHangingWhere()
        {
            List<String> valid = new List<string>()
            {
                "DESCRIBE ?s WHERE { ?s a ?type }",
                "DESCRIBE <http://example.org/>",
                "PREFIX ex: <http://example.org/> DESCRIBE ex:"
            };

            List<String> invalid = new List<string>()
            {
                "DESCRIBE ?s WHERE"
            };

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlFormatter formatter = new SparqlFormatter();
            foreach (String v in valid)
            {
                    SparqlQuery q = parser.ParseFromString(v);
                    Console.WriteLine(formatter.Format(q));
                    Console.WriteLine();
            }

            foreach (String iv in invalid)
            {
                try
                {
                    SparqlQuery q = parser.ParseFromString(iv);
                    Assert.Fail("Should have thrown a Parsing Error");
                }
                catch (RdfParseException parseEx)
                {
                    Console.WriteLine("Errored as expected");
                    TestTools.ReportError("Parsing Error", parseEx);
                }
            }
        }

        [TestMethod]
        public void SparqlParsingConstructShortForm()
        {
            List<String> valid = new List<string>()
            {
                "CONSTRUCT WHERE {?s ?p ?o }",
                "CONSTRUCT WHERE {?s a ?type }",
            };

            List<String> invalid = new List<string>()
            {
                "CONSTRUCT {?s ?p ?o}",
                "CONSTRUCT WHERE { ?s ?p ?o . FILTER(ISLITERAL(?o)) }",
                "CONSTRUCT WHERE { GRAPH ?g { ?s ?p ?o } }",
                "CONSTRUCT WHERE { ?s ?p ?o . OPTIONAL {?s a ?type}}",
                "CONSTRUCT WHERE { ?s a ?type . BIND (<http://example.org> AS ?thing) }",
                "CONSTRUCT WHERE { {SELECT * WHERE { ?s ?p ?o } } }"
            };

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlFormatter formatter = new SparqlFormatter();
            foreach (String v in valid)
            {
                Console.WriteLine("Valid Input: " + v);
                    SparqlQuery q = parser.ParseFromString(v);
                    Console.WriteLine(formatter.Format(q));
                Console.WriteLine();
            }

            foreach (String iv in invalid)
            {
                Console.WriteLine("Invalid Input: " + iv);
                try
                {
                    SparqlQuery q = parser.ParseFromString(iv);
                    Assert.Fail("Should have thrown a Parsing Error");
                }
                catch (RdfParseException parseEx)
                {
                    Console.WriteLine("Errored as expected");
                    TestTools.ReportError("Parsing Error", parseEx);
                }
                Console.WriteLine();
            }
        }

        [TestMethod]
        public void SparqlEvaluationMultipleOptionals()
        {
            TripleStore store = new TripleStore();
            store.LoadFromFile("multiple-options.trig");

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery query = parser.ParseFromFile("multiple-optionals.rq");

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(store);
            Object results = processor.ProcessQuery(query);
            if (results is SparqlResultSet)
            {
                TestTools.ShowResults(results);
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlEvaluationMultipleOptionals2()
        {
            TripleStore store = new TripleStore();
            store.LoadFromFile("multiple-options.trig");

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery query = parser.ParseFromFile("multiple-optionals-alternate.rq");

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(store);
            Object results = processor.ProcessQuery(query);
            if (results is SparqlResultSet)
            {
                TestTools.ShowResults(results);
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod,ExpectedException(typeof(RdfParseException))]
        public void SparqlParsingSubqueries1()
        {
            String query = "SELECT * WHERE { { SELECT * WHERE { ?s ?p ?o } } . }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);
            Console.WriteLine("Parsed original input OK");

            String query2 = q.ToString();
            SparqlQuery q2 = parser.ParseFromString(query2);
            Console.WriteLine("Parsed reserialized input OK");
        }

        [TestMethod]
        public void SparqlParsingSubqueries2()
        {
            String query = "SELECT * WHERE { { SELECT * WHERE { ?s ?p ?o } } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);
            Console.WriteLine("Parsed original input OK");

            String query2 = q.ToString();
            SparqlQuery q2 = parser.ParseFromString(query2);
            Console.WriteLine("Parsed reserialized input OK");
        }

        [TestMethod,ExpectedException(typeof(RdfParseException))]
        public void SparqlParsingSubqueries3()
        {
            String query = "SELECT * WHERE { { SELECT * WHERE { ?s ?p ?o } } . }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);
            Console.WriteLine("Parsed original input OK");

            SparqlFormatter formatter = new SparqlFormatter();
            String query2 = formatter.Format(q);
            SparqlQuery q2 = parser.ParseFromString(query2);
            Console.WriteLine("Parsed reserialized input OK");
        }

        [TestMethod]
        public void SparqlParsingSubqueries4()
        {
            String query = "SELECT * WHERE { { SELECT * WHERE { ?s ?p ?o } } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);
            Console.WriteLine("Parsed original input OK");

            SparqlFormatter formatter = new SparqlFormatter();
            String query2 = formatter.Format(q);
            SparqlQuery q2 = parser.ParseFromString(query2);
            Console.WriteLine("Parsed reserialized input OK");
        }
    }
}
