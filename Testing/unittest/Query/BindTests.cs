﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;

namespace VDS.RDF.Query
{
    [TestClass]
    public class BindTests
    {
        [TestMethod]
        public void SparqlBindExistsAsChildExpression1()
        {
            String query = @"SELECT * WHERE
{
  ?s a ?type .
  BIND(IF(EXISTS { ?s rdfs:range ?range }, true, false) AS ?hasRange)
}";

            SparqlParameterizedString queryStr = new SparqlParameterizedString(query);
            queryStr.Namespaces.AddNamespace("rdfs", UriFactory.Create(NamespaceMapper.RDFS));

            SparqlQuery q = new SparqlQueryParser().ParseFromString(queryStr);

            IGraph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            SparqlResultSet results = g.ExecuteQuery(q) as SparqlResultSet;
            Assert.IsNotNull(results);

            TestTools.ShowResults(results);

            Assert.IsTrue(results.All(r => r.HasBoundValue("hasRange")));
        }

        [TestMethod]
        public void SparqlBindExistsAsChildExpression2()
        {
            String query = @"SELECT * WHERE
{
  ?s a ?type .
  BIND(IF(EXISTS { ?s rdfs:range ?range . FILTER EXISTS { ?s rdfs:domain ?domain } }, true, false) AS ?hasRangeAndDomain)
}";

            SparqlParameterizedString queryStr = new SparqlParameterizedString(query);
            queryStr.Namespaces.AddNamespace("rdfs", UriFactory.Create(NamespaceMapper.RDFS));

            SparqlQuery q = new SparqlQueryParser().ParseFromString(queryStr);

            IGraph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            SparqlResultSet results = g.ExecuteQuery(q) as SparqlResultSet;
            Assert.IsNotNull(results);

            TestTools.ShowResults(results);

            Assert.IsTrue(results.All(r => r.HasBoundValue("hasRangeAndDomain")));
        }
    }
}
