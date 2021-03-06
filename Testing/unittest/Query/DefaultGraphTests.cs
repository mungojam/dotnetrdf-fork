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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Storage;
using VDS.RDF.Update;
using VDS.RDF.Writing;

namespace VDS.RDF.Query
{
    /// <summary>
    /// Summary description for DefaultGraphTests
    /// </summary>
    [TestClass]
    public class DefaultGraphTests
    {
        [TestMethod]
        public void SparqlDefaultGraphExists()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.Assert(g.CreateUriNode(new Uri("http://example.org/subject")), g.CreateUriNode(new Uri("http://example.org/predicate")), g.CreateUriNode(new Uri("http://example.org/object")));
            store.Add(g);

            Object results = store.ExecuteQuery("ASK WHERE { GRAPH ?g { ?s ?p ?o }}");
            if (results is SparqlResultSet)
            {
                Assert.IsFalse(((SparqlResultSet)results).Result);
            }
            else
            {
                Assert.Fail("ASK Query did not return a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlDefaultGraphExists2()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.Assert(g.CreateUriNode(new Uri("http://example.org/subject")), g.CreateUriNode(new Uri("http://example.org/predicate")), g.CreateUriNode(new Uri("http://example.org/object")));
            store.Add(g);

            Object results = store.ExecuteQuery("ASK WHERE { GRAPH <dotnetrdf:default-graph> { ?s ?p ?o }}");
            if (results is SparqlResultSet)
            {
                Assert.IsFalse(((SparqlResultSet)results).Result);
            }
            else
            {
                Assert.Fail("ASK Query did not return a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagement()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.Assert(g.CreateUriNode(new Uri("http://example.org/subject")), g.CreateUriNode(new Uri("http://example.org/predicate")), g.CreateUriNode(new Uri("http://example.org/object")));
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store);
            dataset.SetDefaultGraph(h.BaseUri);
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString("SELECT * WHERE { ?s ?p ?o }");

            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                TestTools.ShowResults(results);
                Assert.IsTrue(((SparqlResultSet)results).IsEmpty, "Results should be empty as an empty Graph was set as the Default Graph");
            }
            else
            {
                Assert.Fail("ASK Query did not return a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagement2()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.Assert(g.CreateUriNode(new Uri("http://example.org/subject")), g.CreateUriNode(new Uri("http://example.org/predicate")), g.CreateUriNode(new Uri("http://example.org/object")));
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store);
            dataset.SetDefaultGraph(g.BaseUri);
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString("SELECT * WHERE { ?s ?p ?o }");

            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                TestTools.ShowResults(results);
                Assert.IsFalse(((SparqlResultSet)results).IsEmpty, "Results should be false as a non-empty Graph was set as the Default Graph");
            }
            else
            {
                Assert.Fail("ASK Query did not return a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagementWithUpdate()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store, h.BaseUri);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString("LOAD <http://www.dotnetrdf.org/configuration#>");

            processor.ProcessCommandSet(cmds);

            Assert.IsTrue(g.IsEmpty, "Graph with null URI (normally the default Graph) should be empty as the Default Graph for the Dataset should have been a named Graph so this Graph should not have been filled by the LOAD Command");
            Assert.IsFalse(h.IsEmpty, "Graph with name should be non-empty as it should have been the Default Graph for the Dataset and so filled by the LOAD Command");
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagementWithUpdate2()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.BaseUri = new Uri("http://example.org/graph");
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store, h.BaseUri);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString("LOAD <http://www.dotnetrdf.org/configuration#> INTO GRAPH <http://example.org/graph>");

            processor.ProcessCommandSet(cmds);

            Assert.IsFalse(g.IsEmpty, "First Graph should not be empty as should have been filled by the LOAD command");
            Assert.IsTrue(h.IsEmpty, "Second Graph should be empty as should not have been filled by the LOAD command");
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagementWithUpdate3()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.BaseUri = new Uri("http://example.org/graph");
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store, h.BaseUri);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString("LOAD <http://www.dotnetrdf.org/configuration#> INTO GRAPH <http://example.org/graph>; LOAD <http://www.dotnetrdf.org/configuration#> INTO GRAPH <http://example.org/someOtherGraph>");

            processor.ProcessCommandSet(cmds);

            Assert.IsFalse(g.IsEmpty, "First Graph should not be empty as should have been filled by the first LOAD command");
            Assert.IsFalse(h.IsEmpty, "Second Graph should not be empty as should not have been filled by the second LOAD command");
            Assert.AreEqual(g, h, "Graphs should be equal");
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagementWithUpdate4()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.BaseUri = new Uri("http://example.org/graph");
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store, h.BaseUri);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString("LOAD <http://www.dotnetrdf.org/configuration#>; WITH <http://example.org/graph> INSERT { ?s a ?type } USING <http://example.org/someOtherGraph> WHERE { ?s a ?type }");

            processor.ProcessCommandSet(cmds);

            Assert.IsFalse(g.IsEmpty, "First Graph should not be empty as should have been filled by the INSERT command");
            Assert.IsFalse(h.IsEmpty, "Second Graph should not be empty as should not have been filled by the LOAD command");
            Assert.IsTrue(h.HasSubGraph(g), "First Graph should be a subgraph of the Second Graph");
        }

        [TestMethod]
        public void SparqlDatasetDefaultGraphManagementWithUpdate5()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.BaseUri = new Uri("http://example.org/graph");
            store.Add(g);
            Graph h = new Graph();
            h.BaseUri = new Uri("http://example.org/someOtherGraph");
            store.Add(h);

            InMemoryDataset dataset = new InMemoryDataset(store, h.BaseUri);
            LeviathanUpdateProcessor processor = new LeviathanUpdateProcessor(dataset);
            SparqlUpdateParser parser = new SparqlUpdateParser();
            SparqlUpdateCommandSet cmds = parser.ParseFromString("LOAD <http://www.dotnetrdf.org/configuration#>; WITH <http://example.org/graph> INSERT { ?s a ?type } USING <http://example.org/someOtherGraph> WHERE { ?s a ?type }; DELETE WHERE { ?s a ?type }");

            processor.ProcessCommandSet(cmds);

            Assert.IsFalse(g.IsEmpty, "First Graph should not be empty as should have been filled by the INSERT command");
            Assert.IsFalse(h.IsEmpty, "Second Graph should not be empty as should not have been filled by the  LOAD command");
            Assert.IsFalse(h.HasSubGraph(g), "First Graph should not be a subgraph of the Second Graph as the DELETE should have eliminated the subgraph relationship");
        }

        [TestMethod]
        public void SparqlGraphClause()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            InMemoryDataset dataset = new InMemoryDataset();
            IGraph ex = new Graph();
            FileLoader.Load(ex, "InferenceTest.ttl");
            ex.BaseUri = new Uri("http://example.org/graph");
            dataset.AddGraph(ex);

            IGraph def = new Graph();
            dataset.AddGraph(def);

            dataset.SetDefaultGraph(def.BaseUri);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                TestTools.ShowResults(rset);
                Assert.AreEqual(ex.Triples.Count, rset.Count, "Number of Results should have been equal to number of Triples");
            }
            else
            {
                Assert.Fail("Did not get a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlGraphClause2()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            InMemoryDataset dataset = new InMemoryDataset();
            IGraph ex = new Graph();
            FileLoader.Load(ex, "InferenceTest.ttl");
            ex.BaseUri = new Uri("http://example.org/graph");
            dataset.AddGraph(ex);

            IGraph def = new Graph();
            dataset.AddGraph(def);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                TestTools.ShowResults(rset);
                Assert.AreEqual(ex.Triples.Count, rset.Count, "Number of Results should have been equal to number of Triples");
            }
            else
            {
                Assert.Fail("Did not get a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlGraphClause3()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            InMemoryDataset dataset = new InMemoryDataset(false);
            IGraph ex = new Graph();
            FileLoader.Load(ex, "InferenceTest.ttl");
            ex.BaseUri = new Uri("http://example.org/graph");
            dataset.AddGraph(ex);

            IGraph def = new Graph();
            dataset.AddGraph(def);

            dataset.SetDefaultGraph(def.BaseUri);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                TestTools.ShowResults(rset);
                Assert.AreEqual(ex.Triples.Count, rset.Count, "Number of Results should have been equal to number of Triples");
            }
            else
            {
                Assert.Fail("Did not get a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlGraphClause4()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            InMemoryDataset dataset = new InMemoryDataset(false);
            IGraph ex = new Graph();
            FileLoader.Load(ex, "InferenceTest.ttl");
            ex.BaseUri = new Uri("http://example.org/graph");
            dataset.AddGraph(ex);

            IGraph def = new Graph();
            dataset.AddGraph(def);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                TestTools.ShowResults(rset);
                Assert.AreEqual(ex.Triples.Count, rset.Count, "Number of Results should have been equal to number of Triples");
            }
            else
            {
                Assert.Fail("Did not get a SPARQL Result Set as expected");
            }
        }

        [TestMethod]
        public void SparqlGraphClause5()
        {
            String query = "SELECT * FROM NAMED <http://example.org/named> WHERE { GRAPH <http://example.org/other> { ?s ?p ?o } }";
            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromString(query);

            TripleStore store = new TripleStore();
            IGraph ex = new Graph();
            FileLoader.Load(ex, "InferenceTest.ttl");
            ex.BaseUri = new Uri("http://example.org/named");
            store.Add(ex);
            IGraph ex2 = new Graph();
            ex2.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            ex2.BaseUri = new Uri("http://example.org/other");
            store.Add(ex2);

            InMemoryDataset dataset = new InMemoryDataset(store);

            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);
            Object results = processor.ProcessQuery(q);
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                TestTools.ShowResults(rset);
                Assert.AreEqual(0, rset.Count, "Should be no results because GRAPH <> specified a Graph not in the dataset");
            }
            else
            {
                Assert.Fail("Did not get a SPARQL Result Set as expected");
            }
        }
    }
}
