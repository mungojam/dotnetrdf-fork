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
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Parsing.Suites
{
   
    [TestClass]
    public class RdfXmlDomParser
        : BaseRdfParserSuite
    {
        public RdfXmlDomParser()
            : base(new RdfXmlParser(RdfXmlParserMode.DOM), new NTriplesParser(), "rdfxml\\")
        {
            this.CheckResults = false;
        }

        [TestMethod]
        public void ParsingSuiteRdfXmlDOM()
        {
            //Run manifests
            this.RunAllDirectories(f => Path.GetExtension(f).Equals(".rdf") && !f.Contains("error"), true);
            this.RunAllDirectories(f => Path.GetExtension(f).Equals(".rdf") && f.Contains("error"), false);

            if (this.Count == 0) Assert.Fail("No tests found");

            Console.WriteLine(this.Count + " Tests - " + this.Passed + " Passed - " + this.Failed + " Failed");
            Console.WriteLine((((double)this.Passed / (double)this.Count) * 100) + "% Passed");

            if (this.Failed > 0) Assert.Fail(this.Failed + " Tests failed");
            if (this.Indeterminate > 0) Assert.Inconclusive(this.Indeterminate + " Tests are indeterminate");
        }

        [TestMethod]
        public void ParsingRdfXmlIDsDOM()
        {
            IGraph g = new Graph();
            g.BaseUri = BaseRdfParserSuite.BaseUri;
            this._parser.Load(g, "rdfxml\\xmlbase\\test014.rdf");
        }
    }

    [TestClass]
    public class RdfXmlStreamingParser
        : BaseRdfParserSuite
    {
        public RdfXmlStreamingParser()
            : base(new RdfXmlParser(RdfXmlParserMode.Streaming), new NTriplesParser(), "rdfxml\\")
        {
            this.CheckResults = false;
        }

        [TestMethod]
        public void ParsingSuiteRdfXmlStreaming()
        {
            //Run manifests
            this.RunAllDirectories(f => Path.GetExtension(f).Equals(".rdf") && !f.Contains("error"), true);
            this.RunAllDirectories(f => Path.GetExtension(f).Equals(".rdf") && f.Contains("error"), false);

            if (this.Count == 0) Assert.Fail("No tests found");

            Console.WriteLine(this.Count + " Tests - " + this.Passed + " Passed - " + this.Failed + " Failed");
            Console.WriteLine((((double)this.Passed / (double)this.Count) * 100) + "% Passed");

            if (this.Failed > 0) Assert.Fail(this.Failed + " Tests failed");
            if (this.Indeterminate > 0) Assert.Inconclusive(this.Indeterminate + " Tests are indeterminate");
        }

        [TestMethod]
        public void ParsingRdfXmlIDsStreaming()
        {
            IGraph g = new Graph();
            g.BaseUri = BaseRdfParserSuite.BaseUri;
            this._parser.Load(g, "rdfxml\\xmlbase\\test014.rdf");
        }
    }
}
