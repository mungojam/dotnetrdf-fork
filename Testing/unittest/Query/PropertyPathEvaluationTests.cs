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
using VDS.RDF.Configuration;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Query.Paths;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query
{
    [TestClass]
    public class PropertyPathEvaluationTests
    {
        private NodeFactory _factory = new NodeFactory();
        private ISparqlDataset _data;

        private ISparqlAlgebra GetAlgebra(ISparqlPath path)
        {
            return GetAlgebra(path, null, null);
        }

        private ISparqlAlgebra GetAlgebra(ISparqlPath path, INode start, INode end)
        {
            PatternItem x, y;
            if (start == null)
            {
                x = new VariablePattern("?x");
            }
            else
            {
                x = new NodeMatchPattern(start);
            }
            if (end == null)
            {
                y = new VariablePattern("?y");
            }
            else
            {
                y = new NodeMatchPattern(end);
            }
            PathTransformContext context = new PathTransformContext(x, y);
            return path.ToAlgebra(context);
        }

        private ISparqlAlgebra GetAlgebraUntransformed(ISparqlPath path)
        {
            return this.GetAlgebraUntransformed(path, null, null);
        }

        private ISparqlAlgebra GetAlgebraUntransformed(ISparqlPath path, INode start, INode end)
        {
            PatternItem x, y;
            if (start == null)
            {
                x = new VariablePattern("?x");
            }
            else
            {
                x = new NodeMatchPattern(start);
            }
            if (end == null)
            {
                y = new VariablePattern("?y");
            }
            else
            {
                y = new NodeMatchPattern(end);
            }
            return new Bgp(new PropertyPathPattern(x, path, y));
        }

        private void EnsureTestData()
        {
            if (this._data == null)
            {
                TripleStore store = new TripleStore();
                Graph g = new Graph();
                g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
                store.Add(g);
                this._data = new InMemoryDataset(store, g.BaseUri);
            }
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroLength()
        {
            EnsureTestData();

            FixedCardinality path = new FixedCardinality(new Property(this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))), 0);
            ISparqlAlgebra algebra = this.GetAlgebra(path);
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroLengthWithTermEnd()
        {
            EnsureTestData();

            FixedCardinality path = new FixedCardinality(new Property(this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))), 0);
            INode rdfsClass = this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "Class"));
            ISparqlAlgebra algebra = this.GetAlgebra(path, null, rdfsClass);
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
            Assert.AreEqual(1, results.Count, "Expected 1 Result");
            Assert.AreEqual(rdfsClass, results[1]["x"], "Expected 1 Result set to rdfs:Class");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroLengthWithTermStart()
        {
            EnsureTestData();

            FixedCardinality path = new FixedCardinality(new Property(this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))), 0);
            ISparqlAlgebra algebra = this.GetAlgebra(path, new Graph().CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassHttpHandler)), null);
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroLengthWithBothTerms()
        {
            EnsureTestData();

            FixedCardinality path = new FixedCardinality(new Property(this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))), 0);
            ISparqlAlgebra algebra = this.GetAlgebra(path, new Graph().CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassHttpHandler)), this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "Class")));
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsTrue(results.IsEmpty, "Results should  be empty");
            Assert.IsTrue(results is NullMultiset, "Results should be Null");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationNegatedPropertySet()
        {
            EnsureTestData();

            NegatedSet path = new NegatedSet(new Property[] { new Property(this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))) }, Enumerable.Empty<Property>());
            ISparqlAlgebra algebra = this.GetAlgebra(path);
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationInverseNegatedPropertySet()
        {
            EnsureTestData();

            NegatedSet path = new NegatedSet(Enumerable.Empty<Property>(), new Property[] { new Property(this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType))) });
            ISparqlAlgebra algebra = this.GetAlgebra(path);
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationSequencedAlternatives()
        {
            EnsureTestData();

            INode a = this._factory.CreateUriNode(new Uri(RdfSpecsHelper.RdfType));
            INode b = this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "range"));
            SequencePath path = new SequencePath(new AlternativePath(new Property(a), new Property(b)), new AlternativePath(new Property(a), new Property(a)));
            ISparqlAlgebra algebra = this.GetAlgebraUntransformed(path);
            SparqlEvaluationContext context = new SparqlEvaluationContext(null, this._data);
            BaseMultiset results = algebra.Evaluate(context);

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");            
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationOneOrMorePath()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromFile("InferenceTest.ttl");
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store, g.BaseUri);

            OneOrMore path = new OneOrMore(new Property(this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "subClassOf"))));
            ISparqlAlgebra algebra = this.GetAlgebra(path);
            BaseMultiset results = algebra.Evaluate(new SparqlEvaluationContext(null, dataset));

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationOneOrMorePathForward()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromFile("InferenceTest.ttl");
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store, g.BaseUri);

            OneOrMore path = new OneOrMore(new Property(this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "subClassOf"))));
            INode sportsCar = this._factory.CreateUriNode(new Uri("http://example.org/vehicles/SportsCar"));
            ISparqlAlgebra algebra = this.GetAlgebra(path, sportsCar, null);
            BaseMultiset results = algebra.Evaluate(new SparqlEvaluationContext(null, dataset));

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationOneOrMorePathReverse()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromFile("InferenceTest.ttl");
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store, g.BaseUri);

            OneOrMore path = new OneOrMore(new Property(this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "subClassOf"))));
            INode airVehicle = this._factory.CreateUriNode(new Uri("http://example.org/vehicles/AirVehicle"));
            ISparqlAlgebra algebra = this.GetAlgebra(path, null, airVehicle);
            BaseMultiset results = algebra.Evaluate(new SparqlEvaluationContext(null, dataset));

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroOrMorePath()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromFile("InferenceTest.ttl");
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store, g.BaseUri);

            ZeroOrMore path = new ZeroOrMore(new Property(this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "subClassOf"))));
            ISparqlAlgebra algebra = this.GetAlgebra(path);
            BaseMultiset results = algebra.Evaluate(new SparqlEvaluationContext(null, dataset));

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroOrMorePathForward()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromFile("InferenceTest.ttl");
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store);

            ZeroOrMore path = new ZeroOrMore(new Property(this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "subClassOf"))));
            INode sportsCar = this._factory.CreateUriNode(new Uri("http://example.org/vehicles/SportsCar"));
            ISparqlAlgebra algebra = this.GetAlgebra(path, sportsCar, null);
            BaseMultiset results = algebra.Evaluate(new SparqlEvaluationContext(null, dataset));

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationZeroOrMorePathReverse()
        {
            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromFile("InferenceTest.ttl");
            store.Add(g);
            InMemoryDataset dataset = new InMemoryDataset(store);

            ZeroOrMore path = new ZeroOrMore(new Property(this._factory.CreateUriNode(new Uri(NamespaceMapper.RDFS + "subClassOf"))));
            INode airVehicle = this._factory.CreateUriNode(new Uri("http://example.org/vehicles/AirVehicle"));
            ISparqlAlgebra algebra = this.GetAlgebra(path, null, airVehicle);
            BaseMultiset results = algebra.Evaluate(new SparqlEvaluationContext(null, dataset));

            TestTools.ShowMultiset(results);

            Assert.IsFalse(results.IsEmpty, "Results should not be empty");
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationGraphInteraction()
        {
            String query = @"PREFIX ex: <http://www.example.org/schema#>
PREFIX in: <http://www.example.org/instance#>

SELECT ?x
FROM NAMED <http://example/1>
FROM NAMED <http://example/2>
WHERE
{
  GRAPH ?g { in:a ex:p1 / ex:p2 ?x . }
}";

            String data = @"<http://www.example.org/instance#a> <http://www.example.org/schema#p1> <http://www.example.org/instance#b> <http://example/1> .
<http://www.example.org/instance#b> <http://www.example.org/schema#p2> <http://www.example.org/instance#c> <http://example/2> .";

            TripleStore store = new TripleStore();
            store.LoadFromString(data, new NQuadsParser());

            SparqlResultSet results = store.ExecuteQuery(query) as SparqlResultSet;
            Assert.IsNotNull(results);
            Assert.AreEqual(SparqlResultsType.VariableBindings, results.ResultsType);
            Assert.AreEqual(0, results.Results.Count);
        }

        [TestMethod]
        public void SparqlPropertyPathEvaluationDuplicates()
        {
            IGraph g = new Graph();
            g.LoadFromFile("schema-org.ttl");

            SparqlQueryParser parser = new SparqlQueryParser();
            SparqlQuery q = parser.ParseFromFile("schema-org.rq");
            SparqlQuery qDistinct = parser.ParseFromFile("schema-org.rq");
            qDistinct.QueryType = SparqlQueryType.SelectDistinct;

            InMemoryDataset dataset = new InMemoryDataset(g);
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(dataset);

            SparqlResultSet results = processor.ProcessQuery(q) as SparqlResultSet;
            Assert.IsNotNull(results);
            Assert.IsFalse(results.IsEmpty);
            SparqlResultSet resultsDistinct = processor.ProcessQuery(qDistinct) as SparqlResultSet;
            Assert.IsNotNull(resultsDistinct);
            Assert.IsFalse(resultsDistinct.IsEmpty);

            Assert.AreEqual(resultsDistinct.Count, results.Count);
        }
    }
}
