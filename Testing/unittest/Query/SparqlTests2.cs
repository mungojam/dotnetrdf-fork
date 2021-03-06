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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Functions;

namespace VDS.RDF
{
    [TestClass]
    public class SparqlTests2
    {
        private ISparqlDataset AsDataset(IInMemoryQueryableStore store)
        {
            if (store.Graphs.Count == 1)
            {
                return new InMemoryDataset(store, store.Graphs.First().BaseUri);
            }
            else
            {
                return new InMemoryDataset(store);
            }
        }

        [TestMethod]
        public void SparqlBind()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT ?triple WHERE { ?s ?p ?o . BIND(fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o)) AS ?triple) }";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count > 0, "Expected 1 or more results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlBindLazy()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT ?triple WHERE { ?s ?p ?o . BIND(fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o)) AS ?triple) } LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should not have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
                Assert.IsTrue(rset.All(r => r.HasValue("triple")), "All Results should have had a value for ?triple");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlBindLazy2()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT * WHERE { ?s ?p ?o . BIND(fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o)) AS ?triple) } LIMIT 10";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should not have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 10, "Expected exactly 10 results");
                Assert.IsTrue(rset.All(r => r.HasValue("s") && r.HasValue("p") && r.HasValue("o") && r.HasValue("triple")), "Expected ?s, ?p, ?o and ?triple values for every result");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlBindLazy3()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT * WHERE { ?s ?p ?o . BIND(fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o)) AS ?triple) } LIMIT 10 OFFSET 10";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should not have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 10, "Expected exactly 10 results");
                Assert.IsTrue(rset.All(r => r.HasValue("s") && r.HasValue("p") && r.HasValue("o") && r.HasValue("triple")), "Expected ?s, ?p, ?o and ?triple values for every result");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        //[TestMethod]
        //public void SparqlBindNested()
        //{
        //    String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT ?triple WHERE { ?s ?p ?o .{ BIND(fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o)) AS ?triple) } FILTER(BOUND(?triple))}";

        //    TripleStore store = new TripleStore();
        //    Graph g = new Graph();
        //    FileLoader.Load(g, "InferenceTest.ttl");
        //    store.Add(g);

        //    SparqlQueryParser parser = new SparqlQueryParser();
        //    SparqlQuery q = parser.ParseFromString(query);

        //    Object results = q.Evaluate(store);
        //    if (results is SparqlResultSet)
        //    {
        //        SparqlResultSet rset = (SparqlResultSet)results;
        //        foreach (SparqlResult r in rset)
        //        {
        //            Console.WriteLine(r.ToString());
        //        }
        //        Assert.IsTrue(rset.Count == 0, "Expected no results");
        //    }
        //    else
        //    {
        //        Assert.Fail("Expected a SPARQL Result Set");
        //    }
        //}

        [TestMethod]
        public void SparqlBindIn10Standard()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT ?triple WHERE { ?s ?p ?o . BIND(fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o)) AS ?triple) }";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser(SparqlQuerySyntax.Sparql_1_0);
            try
            {
                SparqlQuery q = parser.ParseFromString(query);
                Assert.Fail("Expected a RdfParseException to be thrown");
            }
            catch (RdfParseException)
            {
                Console.WriteLine("Error thrown as expected");
            }
            catch
            {
                Assert.Fail("Expected a RdfParseException");
            }
        }

        [TestMethod]
        public void SparqlBindToExistingVariable()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT * WHERE { ?s ?p ?o . BIND(?s AS ?p) }";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            try
            {
                SparqlQuery q = parser.ParseFromString(query);
                store.ExecuteQuery(q);
                Assert.Fail("Expected a RdfParseException/RdfQueryException to be thrown");
            }
            catch (RdfParseException parseEx)
            {
                Console.WriteLine("Parsing Error thrown as expected");
                TestTools.ReportError("Parser Error", parseEx);
            }
            catch (RdfQueryException queryEx)
            {
                Console.WriteLine("Query Error thrown as expected");
                TestTools.ReportError("Query Error", queryEx);
            }
            catch (Exception ex)
            {
                TestTools.ReportError("Unexpected Error", ex);
                Assert.Fail("Did not get a RdfParseException/RdfQueryException as expected");
            }
        }

        [TestMethod]
        public void SparqlBindToExistingVariableLazy()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT * WHERE { ?s ?p ?o . BIND(?s AS ?p) } LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            try
            {
                SparqlQuery q = parser.ParseFromString(query);

                Console.WriteLine(q.ToAlgebra().ToString());
                Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
                Console.WriteLine();

                store.ExecuteQuery(q);
                Assert.Fail("Expected a RdfParseException/RdfQueryException to be thrown");
            }
            catch (RdfParseException parseEx)
            {
                Console.WriteLine("Parsing Error thrown as expected");
                TestTools.ReportError("Parser Error", parseEx);
            }
            catch (RdfQueryException queryEx)
            {
                Console.WriteLine("Query Error thrown as expected");
                TestTools.ReportError("Query Error", queryEx);
            }
            catch (Exception ex)
            {
                TestTools.ReportError("Unexpected Error", ex);
                Assert.Fail("Did not get a RdfParseException/RdfQueryException as expected");
            }
        }

        [TestMethod,ExpectedException(typeof(RdfParseException))]
        public void SparqlBindScope1()
        {
            String query = @"PREFIX : <http://www.example.org>
 SELECT *
 WHERE {
    {
    :s :p ?o .
    :s :q ?o1 .
    }
    BIND((1+?o) AS ?o1)
 }";

            SparqlQueryParser parser = new SparqlQueryParser();
            parser.ParseFromString(query);
        }

        [TestMethod]
        public void SparqlBindScope2()
        {
            String query = @"PREFIX : <http://www.example.org>
 SELECT *
 WHERE {
    :s :p ?o .
    { BIND((1 + ?o) AS ?o1) } UNION { BIND((2 + ?o) AS ?o1) }
 }";

            SparqlQueryParser parser = new SparqlQueryParser();
            parser.ParseFromString(query);
        }

        [TestMethod]
        public void SparqlBindScope3()
        {
            String query = @" PREFIX : <http://www.example.org>
 SELECT *
 WHERE {
    :s :p ?o .
    :s :q ?o1
    { BIND((1+?o) AS ?o1) }
 }";

            SparqlQueryParser parser = new SparqlQueryParser();
            parser.ParseFromString(query);
        }

        [TestMethod]
        public void SparqlBindScope4()
        {
            String query = @" PREFIX : <http://www.example.org>
 SELECT *
 WHERE {
    { 
    :s :p ?o .
    :s :q ?o1
    }
    { BIND((1+?o) AS ?o1) }
 }";

            SparqlQueryParser parser = new SparqlQueryParser();
            parser.ParseFromString(query);
        }

        [TestMethod]
        public void SparqlBindScope5()
        {
            String query = @"PREFIX : <http://example.org>
SELECT *
WHERE
{
  GRAPH ?g { :s :p ?o }
  BIND (?g AS ?in)
}";

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToString());

            ISparqlAlgebra algebra = q.ToAlgebra();
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOfType(algebra, typeof(Select));

            algebra = ((IUnaryOperator)algebra).InnerAlgebra;
            Assert.IsInstanceOfType(algebra, typeof(Extend));
        }

        [TestMethod]
        public void SparqlBindScope6()
        {
            String query = @"PREFIX : <http://example.org>
SELECT *
WHERE
{
  {
    GRAPH ?g { :s :p ?o }
    BIND (?g AS ?in)
  }
  UNION
  {
    :s :p ?o .
    BIND('default' AS ?in)
  }
}";

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToString());

            ISparqlAlgebra algebra = q.ToAlgebra();
            Console.WriteLine(algebra.ToString());
            Assert.IsInstanceOfType(algebra, typeof(Select));

            algebra = ((IUnaryOperator)algebra).InnerAlgebra;
            Assert.IsInstanceOfType(algebra, typeof(Union));

            IUnion union = (Union)algebra;
            ISparqlAlgebra lhs = union.Lhs;
            Assert.IsInstanceOfType(lhs, typeof(Extend));

            ISparqlAlgebra rhs = union.Rhs;
            Assert.IsInstanceOfType(rhs, typeof(Join));
        }

        [TestMethod]
        public void SparqlLet()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT ?triple WHERE { ?s ?p ?o . LET (?triple := fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o))) }";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser(SparqlQuerySyntax.Extended);
            SparqlQuery q = parser.ParseFromString(query);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count > 0, "Expected 1 or more results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlLetIn11Standard()
        {
            String query = "PREFIX fn: <" + XPathFunctionFactory.XPathFunctionsNamespace + "> SELECT ?triple WHERE { ?s ?p ?o . LET (?triple := fn:concat(STR(?s), ' ', STR(?p), ' ', STR(?o))) }";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser(SparqlQuerySyntax.Sparql_1_1);
            try
            {
                SparqlQuery q = parser.ParseFromString(query);
                Assert.Fail("Expected a RdfParseException to be thrown");
            }
            catch (RdfParseException)
            {
                Console.WriteLine("Error thrown as expected");
            } 
            catch 
            {
                Assert.Fail("Expected a RdfParseException");
            }
        }

        //[TestMethod]
        //public void SparqlSubQueryLazy()
        //{
        //    String query = "SELECT * WHERE { {SELECT * WHERE { ?s ?p ?o}}} LIMIT 1";

        //    TripleStore store = new TripleStore();
        //    Graph g = new Graph();
        //    FileLoader.Load(g, "InferenceTest.ttl");
        //    store.Add(g);

        //    SparqlQueryParser parser = new SparqlQueryParser();
        //    SparqlQuery q = parser.ParseFromString(query);

        //    Console.WriteLine(q.ToAlgebra().ToString());
        //    Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
        //    Console.WriteLine();

        //    Object results = q.Evaluate(store);
        //    if (results is SparqlResultSet)
        //    {
        //        SparqlResultSet rset = (SparqlResultSet)results;
        //        foreach (SparqlResult r in rset)
        //        {
        //            Console.WriteLine(r.ToString());
        //        }
        //        Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
        //    }
        //    else
        //    {
        //        Assert.Fail("Expected a SPARQL Result Set");
        //    }
        //}

        //[TestMethod]
        //public void SparqlSubQueryLazy2()
        //{
        //    String query = "SELECT * WHERE { {SELECT * WHERE { ?s ?p ?o}}} LIMIT 10";

        //    TripleStore store = new TripleStore();
        //    Graph g = new Graph();
        //    FileLoader.Load(g, "InferenceTest.ttl");
        //    store.Add(g);

        //    SparqlQueryParser parser = new SparqlQueryParser();
        //    SparqlQuery q = parser.ParseFromString(query);

        //    Console.WriteLine(q.ToAlgebra().ToString());
        //    Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
        //    Console.WriteLine();

        //    Object results = q.Evaluate(store);
        //    if (results is SparqlResultSet)
        //    {
        //        SparqlResultSet rset = (SparqlResultSet)results;
        //        foreach (SparqlResult r in rset)
        //        {
        //            Console.WriteLine(r.ToString());
        //        }
        //        Assert.IsTrue(rset.Count == 10, "Expected exactly 10 results");
        //    }
        //    else
        //    {
        //        Assert.Fail("Expected a SPARQL Result Set");
        //    }
        //}

        //[TestMethod]
        //public void SparqlSubQueryLazy3()
        //{
        //    String query = "SELECT * WHERE { {SELECT * WHERE { ?s ?p ?o}}} LIMIT 10 OFFSET 10";

        //    TripleStore store = new TripleStore();
        //    Graph g = new Graph();
        //    FileLoader.Load(g, "InferenceTest.ttl");
        //    store.Add(g);

        //    SparqlQueryParser parser = new SparqlQueryParser();
        //    SparqlQuery q = parser.ParseFromString(query);

        //    Console.WriteLine(q.ToAlgebra().ToString());
        //    Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
        //    Console.WriteLine();

        //    Object results = q.Evaluate(store);
        //    if (results is SparqlResultSet)
        //    {
        //        SparqlResultSet rset = (SparqlResultSet)results;
        //        foreach (SparqlResult r in rset)
        //        {
        //            Console.WriteLine(r.ToString());
        //        }
        //        Assert.IsTrue(rset.Count == 10, "Expected exactly 10 results");
        //    }
        //    else
        //    {
        //        Assert.Fail("Expected a SPARQL Result Set");
        //    }
        //}

        //[TestMethod]
        //public void SparqlSubQueryLazyComplex()
        //{
        //    String query = "SELECT * WHERE { ?s a <http://example.org/vehicles/Car> . {SELECT * WHERE { ?s <http://example.org/vehicles/Speed> ?speed}}} LIMIT 1";

        //    TripleStore store = new TripleStore();
        //    Graph g = new Graph();
        //    FileLoader.Load(g, "InferenceTest.ttl");
        //    store.Add(g);

        //    SparqlQueryParser parser = new SparqlQueryParser();
        //    SparqlQuery q = parser.ParseFromString(query);

        //    Console.WriteLine(q.ToAlgebra().ToString());
        //    Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
        //    Console.WriteLine();

        //    Object results = q.Evaluate(store);
        //    if (results is SparqlResultSet)
        //    {
        //        SparqlResultSet rset = (SparqlResultSet)results;
        //        foreach (SparqlResult r in rset)
        //        {
        //            Console.WriteLine(r.ToString());
        //        }
        //        Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
        //    }
        //    else
        //    {
        //        Assert.Fail("Expected a SPARQL Result Set");
        //    }
        //}

        //[TestMethod]
        //public void SparqlSubQueryLazyComplex2()
        //{
        //    String query = "SELECT * WHERE { ?s a <http://example.org/vehicles/Car> . {SELECT * WHERE { ?s <http://example.org/vehicles/Speed> ?speed}}} LIMIT 5";

        //    TripleStore store = new TripleStore();
        //    Graph g = new Graph();
        //    FileLoader.Load(g, "InferenceTest.ttl");
        //    store.Add(g);

        //    SparqlQueryParser parser = new SparqlQueryParser();
        //    SparqlQuery q = parser.ParseFromString(query);

        //    Console.WriteLine(q.ToAlgebra().ToString());
        //    Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
        //    Console.WriteLine();

        //    Object results = q.Evaluate(store);
        //    if (results is SparqlResultSet)
        //    {
        //        SparqlResultSet rset = (SparqlResultSet)results;
        //        foreach (SparqlResult r in rset)
        //        {
        //            Console.WriteLine(r.ToString());
        //        }
        //        Assert.IsTrue(rset.Count <= 5, "Expected at most 5 results");
        //    }
        //    else
        //    {
        //        Assert.Fail("Expected a SPARQL Result Set");
        //    }
        //}

        [TestMethod]
        public void SparqlOrderBySubjectLazyAscending()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY ?s LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderBySubjectLazyAscendingExplicit()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY ASC(?s) LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderBySubjectLazyDescending()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY DESC(?s) LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderByPredicateLazyAscending()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY ?p LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderByPredicateLazyAscendingExplicit()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY ASC(?p) LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderByPredicateLazyDescending()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY DESC(?p) LIMIT 1";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 1, "Expected exactly 1 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderByComplexLazy()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY ?s DESC(?p) LIMIT 5";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 5, "Expected exactly 5 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlOrderByComplexLazyPerformance()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . } ORDER BY ?s DESC(?p) LIMIT 5";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "dataset_50.ttl.gz");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();

            //First do with Optimisation
            Stopwatch timer = new Stopwatch();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            timer.Start();
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            timer.Stop();
            Console.WriteLine("Took " + timer.Elapsed + " to execute when Optimised");
            timer.Reset();
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 5, "Expected exactly 5 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }

            //Then do without optimisation
            Options.AlgebraOptimisation = false;
            timer.Start();
            results = processor.ProcessQuery(q);
            timer.Stop();
            Console.WriteLine("Took " + timer.Elapsed + " to execute when Unoptimised");
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 5, "Expected exactly 5 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
            Options.AlgebraOptimisation = true;
        }

        [TestMethod]
        public void SparqlOrderByComplexLazy2()
        {
            String query = "SELECT * WHERE { ?s a ?vehicle . ?s <http://example.org/vehicles/Speed> ?speed } ORDER BY DESC(?speed) LIMIT 3";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.AreEqual(3, rset.Count, "Expected exactly 3 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlFilterLazy()
        {
            String query = "SELECT * WHERE { ?s a ?vehicle . FILTER (SAMETERM(?vehicle, <http://example.org/vehicles/Car>)) } LIMIT 3";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 3, "Expected exactly 3 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlFilterLazy2()
        {
            String query = "SELECT * WHERE { ?s a ?vehicle . FILTER (SAMETERM(?vehicle, <http://example.org/Vehicles/Car>)) } LIMIT 3";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine("NOTE: The URI for Car is purposefully wrong in this case so no results should be returned");
            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 0, "Expected no results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlFilterLazy3()
        {
            long currTimeout = Options.QueryExecutionTimeout;
            try
            {
                Options.QueryExecutionTimeout = 0;

                String query = "SELECT * WHERE { ?s a ?vehicle . FILTER (SAMETERM(?vehicle, <http://example.org/vehicles/Car>)) . ?s <http://example.org/vehicles/Speed> ?speed } LIMIT 3";

                TripleStore store = new TripleStore();
                Graph g = new Graph();
                FileLoader.Load(g, "InferenceTest.ttl");
                store.Add(g);

                SparqlQueryParser parser = new SparqlQueryParser();
                SparqlQuery q = parser.ParseFromString(query);
                q.Timeout = 0;

                Console.WriteLine(q.ToAlgebra().ToString());
                Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
                Console.WriteLine();

                LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
                Object results = processor.ProcessQuery(q);
                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    foreach (SparqlResult r in rset)
                    {
                        Console.WriteLine(r.ToString());
                    }
                    Assert.IsTrue(rset.Count == 3, "Expected exactly 3 results");
                }
                else
                {
                    Assert.Fail("Expected a SPARQL Result Set");
                }
            }
            finally
            {
                Options.QueryExecutionTimeout = currTimeout;
            }
        }

        [TestMethod]
        public void SparqlFilterLazy4()
        {
            String query = "SELECT * WHERE { ?s a <http://example.org/vehicles/Car> ; <http://example.org/vehicles/Speed> ?speed } LIMIT 3";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 3, "Expected exactly 3 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlFilterLazyDBPedia()
        {
            SparqlParameterizedString query = new SparqlParameterizedString();
            query.Namespaces.AddNamespace("rdfs", new Uri(NamespaceMapper.RDFS));
            query.CommandText = "SELECT * WHERE {?s ?p ?label . FILTER(ISLITERAL(?label) && LANGMATCHES(LANG(?label), \"en\")) } LIMIT 5";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            UriLoader.Load(g, new Uri("http://dbpedia.org/resource/Southampton"));
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 5, "Expected exactly 5 results");
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlLazyWithAndWithoutOffset()
        {
            String query = "SELECT * WHERE { ?s a ?vehicle . FILTER (SAMETERM(?vehicle, <http://example.org/vehicles/Car>)) } LIMIT 3";
            String query2 = "SELECT * WHERE { ?s a ?vehicle . FILTER (SAMETERM(?vehicle, <http://example.org/vehicles/Car>)) } LIMIT 3 OFFSET 3";

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");
            store.Add(g);

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);
            SparqlQuery q2 = parser.ParseFromString(query2);

            Console.WriteLine(q.ToAlgebra().ToString());
            Assert.IsTrue(q.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            Console.WriteLine(q2.ToAlgebra().ToString());
            Assert.IsTrue(q2.ToAlgebra().ToString().Contains("LazyBgp"), "Should have been optimised to use a Lazy BGP");
            Console.WriteLine();

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    Console.WriteLine(r.ToString());
                }
                Assert.IsTrue(rset.Count == 3, "Expected exactly 3 results");

                Object results2 = processor.ProcessQuery(q2);
                if (results2 is SparqlResultSet)
                {
                    SparqlResultSet rset2 = (SparqlResultSet)results2;
                    foreach (SparqlResult r in rset2)
                    {
                        Console.WriteLine(r.ToString());
                    }
                    Assert.IsTrue(rset2.Count == 1, "Expected exactly 1 results");
                }
                else
                {
                    Assert.Fail("Expected a SPARQL Result Set");
                }
            }
            else
            {
                Assert.Fail("Expected a SPARQL Result Set");
            }
        }

        [TestMethod]
        public void SparqlLazyLimitSimple1()
        {
            const string query = @"PREFIX eg:
<http://example.org/vehicles/> PREFIX rdf:
<http://www.w3.org/1999/02/22-rdf-syntax-ns#> SELECT ?car ?speed WHERE
{ ?car rdf:type eg:Car . ?car eg:Speed ?speed } LIMIT 1";

            var g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");

            var parser = new SparqlQueryParser();
            var q = parser.ParseFromString(query);
            var results = g.ExecuteQuery(q);
            Assert.IsTrue(results is SparqlResultSet, "Expected a SPARQL results set");
            var rset = results as SparqlResultSet;
            foreach (var r in rset)
            {
                Console.WriteLine(r);
                Assert.AreEqual(2, r.Count, "Expected 2 variable bindings per row.");
            }
        }

        [TestMethod]
        public void SparqlLazyLimitSimple2()
        {
            const string query = @"PREFIX eg:
<http://example.org/vehicles/> PREFIX rdf:
<http://www.w3.org/1999/02/22-rdf-syntax-ns#> SELECT ?car ?speed WHERE
{ ?car rdf:type eg:Car . ?car eg:Speed ?speed } LIMIT 20";

            var g = new Graph();
            FileLoader.Load(g, "InferenceTest.ttl");

            var parser = new SparqlQueryParser();
            var q = parser.ParseFromString(query);
            var results = g.ExecuteQuery(q);
            Assert.IsTrue(results is SparqlResultSet, "Expected a SPARQL results set");
            var rset = results as SparqlResultSet;
            foreach (var r in rset)
            {
                Console.WriteLine(r);
                Assert.AreEqual(2, r.Count, "Expected 2 variable bindings per row.");
            }
        }
    }
}
