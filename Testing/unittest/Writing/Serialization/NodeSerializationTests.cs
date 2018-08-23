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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Handlers;

namespace VDS.RDF.Writing.Serialization
{
    [TestClass]
    public class NodeSerializationTests
    {
        #region Methods that perform the actual test logic

        private void TestSerializationXml(INode n, Type t, bool fullEquality)
        {
            Console.WriteLine("Input: " + n.ToString());

            System.IO.StringWriter writer = new System.IO.StringWriter();
            XmlSerializer serializer = new XmlSerializer(t);
            serializer.Serialize(writer, n);
            Console.WriteLine("Serialized Form:");
            Console.WriteLine(writer.ToString());

            INode m = serializer.Deserialize(new StringReader(writer.ToString())) as INode;
            Console.WriteLine("Deserialized Form: " + m.ToString());
            Console.WriteLine();

            if (fullEquality)
            {
                Assert.AreEqual(n, m, "Nodes should be equal");
            }
            else
            {
                Assert.AreEqual(n.ToString(), m.ToString(), "String forms should be equal");
            }
        }

        private void TestSerializationXml(IEnumerable<INode> nodes, Type t, bool fullEquality)
        {
            foreach (INode n in nodes)
            {
                this.TestSerializationXml(n, t, fullEquality);
            }
        }

        private void TestSerializationBinary(INode n, bool fullEquality)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter serializer = new BinaryFormatter(null, new StreamingContext());
            serializer.Serialize(stream, n);

            stream.Seek(0, SeekOrigin.Begin);
            Console.WriteLine("Serialized Form:");
            StreamReader reader = new StreamReader(stream);
            Console.WriteLine(reader.ReadToEnd());

            stream.Seek(0, SeekOrigin.Begin);
            INode m = serializer.Deserialize(stream) as INode;

            reader.Close();

            if (fullEquality)
            {
                Assert.AreEqual(n, m, "Nodes should be equal");
            }
            else
            {
                Assert.AreEqual(n.ToString(), m.ToString(), "String forms should be equal");
            }

            stream.Dispose();
        }

        private void TestSerializationBinary(IEnumerable<INode> nodes, bool fullEquality)
        {
            foreach (INode n in nodes)
            {
                this.TestSerializationBinary(n, fullEquality);
            }
        }

        private void TestSerializationDataContract(INode n, Type t, bool fullEquality)
        {
            Console.WriteLine("Input: " + n.ToString());

            System.IO.StringWriter writer = new System.IO.StringWriter();
            DataContractSerializer serializer = new DataContractSerializer(t);
            serializer.WriteObject(new XmlTextWriter(writer), n);
            Console.WriteLine("Serialized Form:");
            Console.WriteLine(writer.ToString());

            INode m = serializer.ReadObject(XmlTextReader.Create(new StringReader(writer.ToString()))) as INode;
            Console.WriteLine("Deserialized Form: " + m.ToString());
            Console.WriteLine();

            if (fullEquality)
            {
                Assert.AreEqual(n, m, "Nodes should be equal");
            }
            else
            {
                Assert.AreEqual(n.ToString(), m.ToString(), "String forms should be equal");
            }
        }

        private void TestSerializationDataContract(IEnumerable<INode> nodes, Type t, bool fullEquality)
        {
            foreach (INode n in nodes)
            {
                this.TestSerializationDataContract(n, t, fullEquality);
            }
        }

        #endregion

        #region Unit Tests for Blank Nodes

        [TestMethod]
        public void SerializationXmlBlankNodes()
        {
            Graph g = new Graph();
            INode b = g.CreateBlankNode();
            this.TestSerializationXml(b, typeof(BlankNode), false);
        }

        [TestMethod]
        public void SerializationBinaryBlankNodes()
        {
            Graph g = new Graph();
            INode b = g.CreateBlankNode();
            this.TestSerializationBinary(b, false);
        }

        [TestMethod]
        public void SerializationDataContractBlankNodes()
        {
            Graph g = new Graph();
            INode b = g.CreateBlankNode();
            this.TestSerializationDataContract(b, typeof(BlankNode), false);
        }

        #endregion

        #region Unit Tests for Literal Nodes

        private IEnumerable<INode> GetLiteralNodes()
        {
            Graph g = new Graph();
            List<INode> nodes = new List<INode>()
            {
                g.CreateLiteralNode(String.Empty),
                g.CreateLiteralNode("simple literal"),
                g.CreateLiteralNode("literal with language", "en"),
                g.CreateLiteralNode("literal with different language", "fr"),
                (12345).ToLiteral(g),
                DateTime.Now.ToLiteral(g),
                (123.45).ToLiteral(g),
                (123.45m).ToLiteral(g)
            };
            return nodes;
        }

        [TestMethod]
        public void SerializationXmlLiteralNodes()
        {
            this.TestSerializationXml(this.GetLiteralNodes(), typeof(LiteralNode), true);
        }

        [TestMethod]
        public void SerializationBinaryLiteralNodes()
        {
            this.TestSerializationBinary(this.GetLiteralNodes(), true);
        }

        [TestMethod]
        public void SerializationDataContractLiteralNodes()
        {
            this.TestSerializationDataContract(this.GetLiteralNodes(), typeof(LiteralNode), true);
        }

        #endregion

        #region Unit Tests for URI Nodes

        private IEnumerable<INode> GetUriNodes()
        {
            Graph g = new Graph();
            List<INode> nodes = new List<INode>()
            {
                g.CreateUriNode("rdf:type"),
                g.CreateUriNode("rdfs:label"),
                g.CreateUriNode("xsd:integer"),
                g.CreateUriNode(new Uri("http://example.org")),
                g.CreateUriNode(new Uri("mailto:example@example.org")),
                g.CreateUriNode(new Uri("ftp://ftp.example.org"))
            };
            return nodes;
        }

        [TestMethod]
        public void SerializationXmlUriNodes()
        {
            this.TestSerializationXml(this.GetUriNodes(), typeof(UriNode), true);
        }

        [TestMethod]
        public void SerializationBinaryUriNodes()
        {
            this.TestSerializationBinary(this.GetUriNodes(), true);
        }

        [TestMethod]
        public void SerializationDataContractUriNodes()
        {
            this.TestSerializationDataContract(this.GetUriNodes(), typeof(UriNode), true);
        }

        #endregion

        #region Unit Tests for Graph Literals

        private IEnumerable<INode> GetGraphLiteralNodes()
        {
            Graph g = new Graph();
            Graph h = new Graph();
            EmbeddedResourceLoader.Load(new PagingHandler(new GraphHandler(h), 10), "VDS.RDF.Configuration.configuration.ttl");
            Graph i = new Graph();
            EmbeddedResourceLoader.Load(new PagingHandler(new GraphHandler(i), 5, 25), "VDS.RDF.Configuration.configuration.ttl");

            List<INode> nodes = new List<INode>()
            {
                g.CreateGraphLiteralNode(),
                g.CreateGraphLiteralNode(h),
                g.CreateGraphLiteralNode(i)

            };
            return nodes;
        }

        [TestMethod]
        public void SerializationXmlGraphLiteralNodes()
        {
            this.TestSerializationXml(this.GetGraphLiteralNodes(), typeof(GraphLiteralNode), true);
        }

        [TestMethod]
        public void SerializationBinaryGraphLiteralNodes()
        {
            this.TestSerializationBinary(this.GetGraphLiteralNodes(), true);
        }

        [TestMethod]
        public void SerializationDataContractGraphLiteralNodes()
        {
            this.TestSerializationDataContract(this.GetGraphLiteralNodes(), typeof(GraphLiteralNode), true);
        }

        #endregion

        #region Unit Tests for Variables

        private IEnumerable<INode> GetVariableNodes()
        {
            Graph g = new Graph();
            List<INode> nodes = new List<INode>()
            {
                g.CreateVariableNode("a"),
                g.CreateVariableNode("b"),
                g.CreateVariableNode("c"),
                g.CreateVariableNode("variable"),
                g.CreateVariableNode("some123"),
                g.CreateVariableNode("this-that")
            };
            return nodes;
        }

        [TestMethod]
        public void SerializationXmlVariableNodes()
        {
            this.TestSerializationXml(this.GetVariableNodes(), typeof(VariableNode), true);
        }

        [TestMethod]
        public void SerializationBinaryVariableNodes()
        {
            this.TestSerializationBinary(this.GetVariableNodes(), true);
        }

        [TestMethod]
        public void SerializationDataContractVariableNodes()
        {
            this.TestSerializationDataContract(this.GetVariableNodes(), typeof(VariableNode), true);
        }

        #endregion
    }
}
