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

namespace VDS.RDF.Parsing.Suites
{
    [TestClass]
    public abstract class BaseRdfParserSuite
    {
        protected IRdfReader _parser;
        private IRdfReader _resultsParser;
        private String _baseDir;
        private bool _check = true;
        private int _count, _pass, _fail, _indeterminate;

        public static Uri BaseUri = new Uri("http://www.w3.org/2001/sw/DataAccess/df1/tests/");

        public BaseRdfParserSuite(IRdfReader testParser, IRdfReader resultsParser, String baseDir)
        {
            if (testParser == null) throw new ArgumentNullException("testParser");
            if (resultsParser == null) throw new ArgumentNullException("resultsParser");
            if (baseDir == null) throw new ArgumentNullException("baseDir");

            this._parser = testParser;
            this._resultsParser = resultsParser;
            this._baseDir = baseDir;
        }

        /// <summary>
        /// Gets/Sets whether the suite needs to check result files for tests that should parse
        /// </summary>
        public bool CheckResults
        {
            get
            {
                return this._check;
            }
            set
            {
                this._check = value;
            }
        }

        public int Count
        {
            get
            {
                return this._count;
            }
        }

        public int Passed
        {
            get
            {
                return this._pass;
            }
        }

        public int Failed
        {
            get
            {
                return this._fail;
            }
        }

        public int Indeterminate
        {
            get
            {
                return this._indeterminate;
            }
        }

        [TestInitialize]
        public void Setup()
        {
            this._count = 0;
            this._pass = 0;
            this._fail = 0;
            this._indeterminate = 0;
        }

        /// <summary>
        /// Runs all tests found in the manifest
        /// </summary>
        /// <param name="file">Manifest file</param>
        /// <param name="shouldParse">Whether contained tests are expected to parse or not</param>
        protected void RunManifest(String file, bool shouldParse)
        {
            if (!File.Exists(file))
            {
                Assert.Fail("Manifest file " + file + " not found");
            }

            Graph manifest = new Graph();
            manifest.BaseUri = BaseUri;
            try
            {
               manifest.LoadFromFile(file);
            }
            catch (Exception ex)
            {
                TestTools.ReportError("Bad Manifest", ex);
                Assert.Fail("Failed to load Manifest " + file);
            }

            String findTests = @"prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> 
prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> 
prefix qt:     <http://www.w3.org/2001/sw/DataAccess/tests/test-query#> 
SELECT ?name ?input ?comment ?result
WHERE
{
  { ?test mf:action [ qt:data ?input ] . }
  UNION
  { ?test mf:action ?input . FILTER(!ISBLANK(?input)) }
  OPTIONAL { ?test mf:name ?name }
  OPTIONAL { ?test rdfs:comment ?comment }
  OPTIONAL { ?test mf:result ?result }
}";

            SparqlResultSet tests = manifest.ExecuteQuery(findTests) as SparqlResultSet;
            if (tests == null) Assert.Fail("Failed to find tests in the Manifest");

            foreach (SparqlResult test in tests)
            {
                INode nameNode, inputNode, commentNode, resultNode;
                String name = test.TryGetBoundValue("name", out nameNode) ? nameNode.ToString() : null;
                inputNode = test["input"];
                String input = this.GetFile(inputNode);
                String comment = test.TryGetBoundValue("comment", out commentNode) ? commentNode.ToString() : null;
                String results = test.TryGetBoundValue("result", out resultNode) ? this.GetFile(resultNode) : null;

                this.RunTest(name, comment, input, results, shouldParse);
            }
        }

        /// <summary>
        /// Runs all tests found in the manifest, determines whether a test should pass/fail based on the test information
        /// </summary>
        /// <param name="file">Manifest file</param>
        protected void RunManifest(String file, INode positiveSyntaxTest, INode negativeSyntaxTest)
        {
            if (!File.Exists(file))
            {
                Assert.Fail("Manifest file " + file + " not found");
            }

            Graph manifest = new Graph();
            manifest.BaseUri = BaseUri;
            try
            {
                manifest.LoadFromFile(file);
            }
            catch (Exception ex)
            {
                TestTools.ReportError("Bad Manifest", ex);
                Assert.Fail("Failed to load Manifest " + file);
            }
            manifest.NamespaceMap.AddNamespace("rdf", UriFactory.Create("http://www.w3.org/ns/rdftest#"));

            String findTests = @"prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> 
prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> 
prefix qt:     <http://www.w3.org/2001/sw/DataAccess/tests/test-query#> 
prefix rdft:   <http://www.w3.org/ns/rdftest#>
SELECT ?name ?input ?comment ?result ?type
WHERE
{
  { ?test mf:action [ qt:data ?input ] . }
  UNION
  { ?test mf:action ?input . FILTER(!ISBLANK(?input)) }
  OPTIONAL { ?test a ?type }
  OPTIONAL { ?test mf:name ?name }
  OPTIONAL { ?test rdfs:comment ?comment }
  OPTIONAL { ?test mf:result ?result }
}";

            SparqlResultSet tests = manifest.ExecuteQuery(findTests) as SparqlResultSet;
            if (tests == null) Assert.Fail("Failed to find tests in the Manifest");

            foreach (SparqlResult test in tests)
            {
                INode nameNode, inputNode, commentNode, resultNode;
                String name = test.TryGetBoundValue("name", out nameNode) ? nameNode.ToString() : null;
                inputNode = test["input"];
                String input = this.GetFile(inputNode);
                String comment = test.TryGetBoundValue("comment", out commentNode) ? commentNode.ToString() : null;
                String results = test.TryGetBoundValue("result", out resultNode) ? this.GetFile(resultNode) : null;

                //Determine expected outcome
                //Evaluation tests will have results and should always parse succesfully
                bool? shouldParse = results != null ? true : false;
                if (!shouldParse.Value)
                {
                    //No results declared so may be a positive/negative syntax test
                    //Inspect returned type to determine, if no type assume test should fail
                    INode type;
                    if (test.TryGetBoundValue("type", out type))
                    {
                        if (type.Equals(positiveSyntaxTest))
                        {
                            shouldParse = true;
                        }
                        else if (type.Equals(negativeSyntaxTest))
                        {
                            shouldParse = false;
                        }
                        else
                        {
                            //Unable to determine what the expected result is
                            shouldParse = null;
                        }
                    }
                }

                this.RunTest(name, comment, input, results, shouldParse);
            }
        }

        protected void RunDirectory(String dir, String pattern, bool shouldParse)
        {
            foreach (String file in Directory.GetFiles(dir, pattern))
            {
                this.RunTest(Path.GetFileName(file), null, file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".nt"), shouldParse);
            }
        }

        protected void RunDirectory(String pattern, bool shouldParse)
        {
            this.RunDirectory(this._baseDir, pattern, shouldParse);
        }

        protected void RunDirectory(Func<String, bool> isTest, bool shouldParse)
        {
            this.RunDirectory(this._baseDir, isTest, shouldParse);
        }

        protected void RunDirectory(String dir, Func<String, bool> isTest, bool shouldParse)
        {
            foreach (String file in Directory.GetFiles(dir).Where(f => isTest(f)))
            {
                this.RunTest(Path.GetFileName(file), null, file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".nt"), shouldParse);
            }
        }

        protected void RunAllDirectories(Func<String, bool> isTest, bool shouldParse)
        {
            this.RunAllDirectories(this._baseDir, isTest, shouldParse);
        }

        protected void RunAllDirectories(String dir, Func<String, bool> isTest, bool shouldParse)
        {
            foreach (String subdir in Directory.GetDirectories(dir))
            {
                this.RunDirectory(subdir, isTest, shouldParse);
                this.RunAllDirectories(subdir, isTest, shouldParse);
            }
        }

        private String GetFile(INode n)
        {
            switch (n.NodeType)
            {
                case NodeType.Uri:
                    Uri u = ((IUriNode)n).Uri;
                    if (u.IsFile)
                    {
                        return u.AbsolutePath;
                    }
                    else
                    {
                        String lastSegment = u.Segments[u.Segments.Length - 1];
                        return Path.Combine(this._baseDir, lastSegment);
                    }
                default:
                    Assert.Fail("Malformed manifest file, input file must be a  URI");
                    break;
            }
            //Keep compiler happy
            return null;
        }

        private void RunTest(String name, String comment, String file, String resultFile, bool? shouldParse)
        {
            Console.WriteLine("### Running Test #" + this._count);
            if (name != null) Console.WriteLine("Test Name " + name);
            if (comment != null) Console.WriteLine(comment);
            Console.WriteLine();
            Console.WriteLine("Input File is " + file);
            if (resultFile != null) Console.WriteLine("Expected Output File is " + resultFile);

            //Check File Exists
            if (!File.Exists(file))
            {
                Console.WriteLine("Input File not found");
                this._fail++;
                return;
            }

            try
            {
                Graph actual = new Graph();
                actual.BaseUri = new Uri(BaseUri, Path.GetFileName(file));
                this._parser.Load(actual, file);

                if (!shouldParse.HasValue)
                {
                    Console.WriteLine("Unable to determine whether the test should pass/fail based on manifest information (Test Indeterminate)");
                    this._indeterminate++;
                }
                else if (shouldParse.Value)
                {
                    Console.WriteLine("Parsed input in OK");

                    //Validate if necessary
                    if (this.CheckResults && resultFile != null)
                    {
                        if (!File.Exists(resultFile))
                        {
                            Console.WriteLine("Expected Output File not found");
                            this._fail++;
                        }
                        else
                        {
                            Graph expected = new Graph();
                            try
                            {
                                this._resultsParser.Load(expected, resultFile);

                                GraphDiffReport diff = expected.Difference(actual);
                                if (diff.AreEqual)
                                {
                                    Console.WriteLine("Parsed Graph matches Expected Graph (Test Passed)");
                                    this._pass++;
                                }
                                else
                                {
                                    Console.WriteLine("Parsed Graph did not match Expected Graph (Test Failed)");
                                    this._fail++;
                                    TestTools.ShowDifferences(diff);
                                }
                            }
                            catch (RdfParseException)
                            {
                                Console.WriteLine("Expected Output File could not be parsed (Test Indeterminate)");
                                this._indeterminate++;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Validation Required (Test Passed)");
                        this._pass++;
                    }
                }
                else
                {
                    Console.WriteLine("Parsed when failure was expected (Test Failed)");
                    this._fail++;
                }
            }
            catch (RdfParseException parseEx)
            {
                if (shouldParse.HasValue && shouldParse.Value)
                {
                    Console.WriteLine("Failed when was expected to parse (Test Failed)");

                    //Repeat parsing with tracing enabled if appropriate
                    //This gives us more useful debugging output for failed tests
                    if (this._parser is ITraceableTokeniser)
                    {
                        try
                        {
                            ((ITraceableTokeniser)this._parser).TraceTokeniser = true;
                            this._parser.Load(new Graph(), Path.GetFileName(file));
                        }
                        catch
                        {
                            //Ignore errors the 2nd time around, we've already got a copy of the error to report
                        }
                        finally
                        {
                            ((ITraceableTokeniser)this._parser).TraceTokeniser = false;
                        }
                    }

                    TestTools.ReportError("Parse Error", parseEx);
                    this._fail++;
                }
                else
                {
                    Console.WriteLine("Failed to parse as expected (Test Passed)");
                    this._pass++;
                }
            }
            Console.WriteLine("### End Test #" + this._count);
            Console.WriteLine();
            this._count++;
        }
    }

    [TestClass]
    public abstract class BaseDatasetParserSuite
    {
        protected IStoreReader _parser;
        private IStoreReader _resultsParser;
        private String _baseDir;
        private bool _check = true;
        private int _count, _pass, _fail, _indeterminate;

        public static Uri BaseUri = new Uri("http://www.w3.org/2001/sw/DataAccess/df1/tests/");

        public BaseDatasetParserSuite(IStoreReader testParser, IStoreReader resultsParser, String baseDir)
        {
            if (testParser == null) throw new ArgumentNullException("testParser");
            if (resultsParser == null) throw new ArgumentNullException("resultsParser");
            if (baseDir == null) throw new ArgumentNullException("baseDir");

            this._parser = testParser;
            this._resultsParser = resultsParser;
            this._baseDir = baseDir;
        }

        /// <summary>
        /// Gets/Sets whether the suite needs to check result files for tests that should parse
        /// </summary>
        public bool CheckResults
        {
            get
            {
                return this._check;
            }
            set
            {
                this._check = value;
            }
        }

        public int Count
        {
            get
            {
                return this._count;
            }
        }

        public int Passed
        {
            get
            {
                return this._pass;
            }
        }

        public int Failed
        {
            get
            {
                return this._fail;
            }
        }

        public int Indeterminate
        {
            get
            {
                return this._indeterminate;
            }
        }

        [TestInitialize]
        public void Setup()
        {
            this._count = 0;
            this._pass = 0;
            this._fail = 0;
            this._indeterminate = 0;
        }

        protected void RunManifest(String file, bool shouldParse)
        {
            if (!File.Exists(file))
            {
                Assert.Fail("Manifest file " + file + " not found");
            }

            Graph manifest = new Graph();
            manifest.BaseUri = BaseUri;
            try
            {
                manifest.LoadFromFile(file);
            }
            catch (Exception ex)
            {
                TestTools.ReportError("Bad Manifest", ex);
                Assert.Fail("Failed to load Manifest " + file);
            }

            String findTests = @"prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> 
prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> 
prefix qt:     <http://www.w3.org/2001/sw/DataAccess/tests/test-query#> 
SELECT ?name ?input ?comment ?result
WHERE
{
  ?test mf:action [ qt:data ?input ] .
  OPTIONAL { ?test mf:name ?name }
  OPTIONAL { ?test rdfs:comment ?comment }
  OPTIONAL { ?test mf:result ?result }
}";

            SparqlResultSet tests = manifest.ExecuteQuery(findTests) as SparqlResultSet;
            if (tests == null) Assert.Fail("Failed to find tests in the Manifest");

            foreach (SparqlResult test in tests)
            {
                INode nameNode, inputNode, commentNode, resultNode;
                String name = test.TryGetBoundValue("name", out nameNode) ? nameNode.ToString() : null;
                inputNode = test["input"];
                String input = this.GetFile(inputNode);
                String comment = test.TryGetBoundValue("comment", out commentNode) ? commentNode.ToString() : null;
                String results = test.TryGetBoundValue("result", out resultNode) ? this.GetFile(resultNode) : null;

                this.RunTest(name, comment, input, results, shouldParse);
            }
        }

        protected void RunDirectory(String dir, String pattern, bool shouldParse)
        {
            foreach (String file in Directory.GetFiles(dir, pattern))
            {
                this.RunTest(Path.GetFileName(file), null, file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".nt"), shouldParse);
            }
        }

        protected void RunDirectory(String pattern, bool shouldParse)
        {
            this.RunDirectory(this._baseDir, pattern, shouldParse);
        }

        protected void RunDirectory(Func<String, bool> isTest, bool shouldParse)
        {
            this.RunDirectory(this._baseDir, isTest, shouldParse);
        }

        protected void RunDirectory(String dir, Func<String, bool> isTest, bool shouldParse)
        {
            foreach (String file in Directory.GetFiles(dir).Where(f => isTest(f)))
            {
                this.RunTest(Path.GetFileName(file), null, file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".nt"), shouldParse);
            }
        }

        protected void RunAllDirectories(Func<String, bool> isTest, bool shouldParse)
        {
            this.RunAllDirectories(this._baseDir, isTest, shouldParse);
        }

        protected void RunAllDirectories(String dir, Func<String, bool> isTest, bool shouldParse)
        {
            foreach (String subdir in Directory.GetDirectories(dir))
            {
                this.RunDirectory(subdir, isTest, shouldParse);
                this.RunAllDirectories(subdir, isTest, shouldParse);
            }
        }

        private String GetFile(INode n)
        {
            switch (n.NodeType)
            {
                case NodeType.Uri:
                    Uri u = ((IUriNode)n).Uri;
                    if (u.IsFile)
                    {
                        return u.AbsolutePath;
                    }
                    else
                    {
                        String lastSegment = u.Segments[u.Segments.Length - 1];
                        return Path.Combine(this._baseDir, lastSegment);
                    }
                default:
                    Assert.Fail("Malformed manifest file, input file must be a  URI");
                    break;
            }
            //Keep compiler happy
            return null;
        }

        private void RunTest(String name, String comment, String file, String resultFile, bool shouldParse)
        {
            Console.WriteLine("### Running Test #" + this._count);
            if (name != null) Console.WriteLine("Test Name " + name);
            if (comment != null) Console.WriteLine(comment);
            Console.WriteLine();
            Console.WriteLine("Input File is " + file);
            if (resultFile != null) Console.WriteLine("Expected Output File is " + resultFile);

            //Check File Exists
            if (!File.Exists(file))
            {
                Console.WriteLine("Input File not found");
                this._fail++;
                return;
            }

            try
            {
                TripleStore actual = new TripleStore();
                this._parser.Load(actual, file);

                if (shouldParse)
                {
                    Console.WriteLine("Parsed input in OK");

                    //Validate if necessary
                    if (this.CheckResults)
                    {
                        if (!File.Exists(resultFile))
                        {
                            Console.WriteLine("Expected Output File not found");
                            this._fail++;
                        }
                        else
                        {
                            TripleStore expected = new TripleStore();
                            try
                            {
                                this._resultsParser.Load(expected, resultFile);

                                if (this.AreEqual(expected, actual))
                                {
                                    Console.WriteLine("Parsed Dataset matches Expected Dataset (Test Passed)");
                                    this._pass++;
                                }
                                else
                                {
                                    Console.WriteLine("Parsed Dataset did not match Expected Dataset (Test Failed)");
                                    this._fail++;
                                }
                            }
                            catch (RdfParseException)
                            {
                                Console.WriteLine("Expected Output File could not be parsed (Test Indeterminate)");
                                this._indeterminate++;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Validation Required (Test Passed)");
                        this._pass++;
                    }
                }
                else
                {
                    Console.WriteLine("Parsed when failure was expected (Test Failed)");
                    this._fail++;
                }
            }
            catch (RdfParseException parseEx)
            {
                if (shouldParse)
                {
                    Console.WriteLine("Failed when was expected to parse (Test Failed)");
                    TestTools.ReportError("Parse Error", parseEx);
                    this._fail++;
                }
                else
                {
                    Console.WriteLine("Failed to parse as expected (Test Passed)");
                    this._pass++;
                }
            }
            Console.WriteLine("### End Test #" + this._count);
            Console.WriteLine();
            this._count++;
        }

        private bool AreEqual(TripleStore expected, TripleStore actual)
        {
            if (expected.Graphs.Count != actual.Graphs.Count)
            {
                Console.WriteLine("Expected " + expected.Graphs.Count + " graphs but got " + actual.Graphs.Count);
                return false;
            }

            foreach (Uri u in expected.Graphs.GraphUris)
            {
                if (!actual.HasGraph(u))
                {
                    Console.WriteLine("Expected Graph " + VDS.RDF.Extensions.ToSafeString(u) + " missing");
                    return false;
                }
                GraphDiffReport diff = expected[u].Difference(actual[u]);
                if (!diff.AreEqual)
                {
                    Console.WriteLine("Expected Graph " + VDS.RDF.Extensions.ToSafeString(u) + " not as expected");
                    TestTools.ShowDifferences(diff);
                    return false;
                }
            }

            return true;
        }
    }

    [TestClass]
    public abstract class BaseResultsParserSuite
    {
        protected ISparqlResultsReader _parser;
        private ISparqlResultsReader _resultsParser;
        private String _baseDir;
        private bool _check = true;
        private int _count, _pass, _fail, _indeterminate;

        public static Uri BaseUri = new Uri("http://www.w3.org/2001/sw/DataAccess/df1/tests/");

        public BaseResultsParserSuite(ISparqlResultsReader testParser, ISparqlResultsReader resultsParser, String baseDir)
        {
            if (testParser == null) throw new ArgumentNullException("testParser");
            if (resultsParser == null) throw new ArgumentNullException("resultsParser");
            if (baseDir == null) throw new ArgumentNullException("baseDir");

            this._parser = testParser;
            this._resultsParser = resultsParser;
            this._baseDir = baseDir;
        }

        /// <summary>
        /// Gets/Sets whether the suite needs to check result files for tests that should parse
        /// </summary>
        public bool CheckResults
        {
            get
            {
                return this._check;
            }
            set
            {
                this._check = value;
            }
        }

        public int Count
        {
            get
            {
                return this._count;
            }
        }

        public int Passed
        {
            get
            {
                return this._pass;
            }
        }

        public int Failed
        {
            get
            {
                return this._fail;
            }
        }

        public int Indeterminate
        {
            get
            {
                return this._indeterminate;
            }
        }

        [TestInitialize]
        public void Setup()
        {
            this._count = 0;
            this._pass = 0;
            this._fail = 0;
            this._indeterminate = 0;
        }

        protected void RunManifest(String file, bool shouldParse)
        {
            if (!File.Exists(file))
            {
                Assert.Fail("Manifest file " + file + " not found");
            }

            Graph manifest = new Graph();
            manifest.BaseUri = BaseUri;
            try
            {
                manifest.LoadFromFile(file);
            }
            catch (Exception ex)
            {
                TestTools.ReportError("Bad Manifest", ex);
                Assert.Fail("Failed to load Manifest " + file);
            }

            String findTests = @"prefix rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> 
prefix rdfs:	<http://www.w3.org/2000/01/rdf-schema#> 
prefix mf:     <http://www.w3.org/2001/sw/DataAccess/tests/test-manifest#> 
prefix qt:     <http://www.w3.org/2001/sw/DataAccess/tests/test-query#> 
SELECT ?name ?input ?comment ?result
WHERE
{
  ?test mf:action [ qt:data ?input ] .
  OPTIONAL { ?test mf:name ?name }
  OPTIONAL { ?test rdfs:comment ?comment }
  OPTIONAL { ?test mf:result ?result }
}";

            SparqlResultSet tests = manifest.ExecuteQuery(findTests) as SparqlResultSet;
            if (tests == null) Assert.Fail("Failed to find tests in the Manifest");

            foreach (SparqlResult test in tests)
            {
                INode nameNode, inputNode, commentNode, resultNode;
                String name = test.TryGetBoundValue("name", out nameNode) ? nameNode.ToString() : null;
                inputNode = test["input"];
                String input = this.GetFile(inputNode);
                String comment = test.TryGetBoundValue("comment", out commentNode) ? commentNode.ToString() : null;
                String results = test.TryGetBoundValue("result", out resultNode) ? this.GetFile(resultNode) : null;

                this.RunTest(name, comment, input, results, shouldParse);
            }
        }

        protected void RunDirectory(String dir, String pattern, bool shouldParse)
        {
            foreach (String file in Directory.GetFiles(dir, pattern))
            {
                this.RunTest(Path.GetFileName(file), null, file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".srx"), shouldParse);
            }
        }

        protected void RunDirectory(String pattern, bool shouldParse)
        {
            this.RunDirectory(this._baseDir, pattern, shouldParse);
        }

        protected void RunDirectory(Func<String, bool> isTest, bool shouldParse)
        {
            this.RunDirectory(this._baseDir, isTest, shouldParse);
        }

        protected void RunDirectory(String dir, Func<String, bool> isTest, bool shouldParse)
        {
            foreach (String file in Directory.GetFiles(dir).Where(f => isTest(f)))
            {
                this.RunTest(Path.GetFileName(file), null, file, Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".srx"), shouldParse);
            }
        }

        protected void RunAllDirectories(Func<String, bool> isTest, bool shouldParse)
        {
            this.RunAllDirectories(this._baseDir, isTest, shouldParse);
        }

        protected void RunAllDirectories(String dir, Func<String, bool> isTest, bool shouldParse)
        {
            foreach (String subdir in Directory.GetDirectories(dir))
            {
                this.RunDirectory(subdir, isTest, shouldParse);
                this.RunAllDirectories(subdir, isTest, shouldParse);
            }
        }

        private String GetFile(INode n)
        {
            switch (n.NodeType)
            {
                case NodeType.Uri:
                    Uri u = ((IUriNode)n).Uri;
                    if (u.IsFile)
                    {
                        return u.AbsolutePath;
                    }
                    else
                    {
                        String lastSegment = u.Segments[u.Segments.Length - 1];
                        return Path.Combine(this._baseDir, lastSegment);
                    }
                default:
                    Assert.Fail("Malformed manifest file, input file must be a  URI");
                    break;
            }
            //Keep compiler happy
            return null;
        }

        private void RunTest(String name, String comment, String file, String resultFile, bool shouldParse)
        {
            Console.WriteLine("### Running Test #" + this._count);
            if (name != null) Console.WriteLine("Test Name " + name);
            if (comment != null) Console.WriteLine(comment);
            Console.WriteLine();
            Console.WriteLine("Input File is " + file);
            if (resultFile != null) Console.WriteLine("Expected Output File is " + resultFile);

            //Check File Exists
            if (!File.Exists(file))
            {
                Console.WriteLine("Input File not found");
                this._fail++;
                return;
            }

            try
            {
                SparqlResultSet actual = new SparqlResultSet();
                this._parser.Load(actual, file);

                if (shouldParse)
                {
                    Console.WriteLine("Parsed input in OK");

                    //Validate if necessary
                    if (this.CheckResults)
                    {
                        if (!File.Exists(resultFile))
                        {
                            Console.WriteLine("Expected Output File not found");
                            this._fail++;
                        }
                        else
                        {
                            SparqlResultSet expected = new SparqlResultSet();
                            try
                            {
                                this._resultsParser.Load(expected, resultFile);

                                if (expected.Equals(actual))
                                {
                                    Console.WriteLine("Parsed Results matches Expected Results (Test Passed)");
                                    this._pass++;
                                }
                                else
                                {
                                    Console.WriteLine("Parsed Results did not match Expected Graph (Test Failed)");
                                    this._fail++;
                                    Console.WriteLine("Expected:");
                                    TestTools.ShowResults(expected);
                                    Console.WriteLine();
                                    Console.WriteLine("Actual:");
                                    TestTools.ShowResults(actual);
                                }
                            }
                            catch (RdfParseException)
                            {
                                Console.WriteLine("Expected Output File could not be parsed (Test Indeterminate)");
                                this._indeterminate++;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Validation Required (Test Passed)");
                        this._pass++;
                    }
                }
                else
                {
                    Console.WriteLine("Parsed when failure was expected (Test Failed)");
                    this._fail++;
                }
            }
            catch (RdfParseException parseEx)
            {
                if (shouldParse)
                {
                    Console.WriteLine("Failed when was expected to parse (Test Failed)");
                    TestTools.ReportError("Parse Error", parseEx);
                    this._fail++;
                }
                else
                {
                    Console.WriteLine("Failed to parse as expected (Test Passed)");
                    this._pass++;
                }
            }
            Console.WriteLine("### End Test #" + this._count);
            Console.WriteLine();
            this._count++;
        }
    }

}
