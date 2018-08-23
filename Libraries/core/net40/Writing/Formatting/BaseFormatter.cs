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
using VDS.RDF.Query;

namespace VDS.RDF.Writing.Formatting
{
    /// <summary>
    /// Abstract Base Class for Formatters
    /// </summary>
    public abstract class BaseFormatter
        : INodeFormatter, ITripleFormatter, IUriFormatter, ICharFormatter, IResultFormatter
    {
        private String _format;

        /// <summary>
        /// Creates a new Formatter
        /// </summary>
        /// <param name="formatName">Format Name</param>
        public BaseFormatter(String formatName)
        {
            this._format = formatName;
        }

        /// <summary>
        /// Gets the Format Name
        /// </summary>
        public String FormatName
        {
            get
            {
                return this._format;
            }
        }

        /// <summary>
        /// Formats a Node as a String
        /// </summary>
        /// <param name="n">Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns></returns>
        public virtual String Format(INode n, TripleSegment? segment)
        {
            switch (n.NodeType)
            {
                case NodeType.Blank:
                    return this.FormatBlankNode((IBlankNode)n, segment);
                case NodeType.GraphLiteral:
                    return this.FormatGraphLiteralNode((IGraphLiteralNode)n, segment);
                case NodeType.Literal:
                    return this.FormatLiteralNode((ILiteralNode)n, segment);
                case NodeType.Uri:
                    return this.FormatUriNode((IUriNode)n, segment);
                case NodeType.Variable:
                    return this.FormatVariableNode((IVariableNode)n, segment);
                default:
                    throw new RdfOutputException(WriterErrorMessages.UnknownNodeTypeUnserializable(this._format));
            }
        }

        /// <summary>
        /// Formats a Node as a String
        /// </summary>
        /// <param name="n">Node</param>
        /// <returns></returns>
        public virtual String Format(INode n)
        {
            return this.Format(n, null);
        }

        /// <summary>
        /// Formats a Triple as a String
        /// </summary>
        /// <param name="t">Triple</param>
        /// <returns></returns>
        public virtual String Format(Triple t)
        {
            return this.Format(t.Subject, TripleSegment.Subject) + " " + this.Format(t.Predicate, TripleSegment.Predicate) + " " + this.Format(t.Object, TripleSegment.Object) + " .";
        }

        /// <summary>
        /// Formats a URI Node as a String for the given Format
        /// </summary>
        /// <param name="u">URI Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns></returns>
        protected abstract String FormatUriNode(IUriNode u, TripleSegment? segment);

        /// <summary>
        /// Formats a URI as a String for full Output
        /// </summary>
        /// <param name="u">URI</param>
        /// <returns></returns>
        public virtual String FormatUri(String u)
        {
            //String uri = Uri.EscapeUriString(u);
            u = u.Replace(">", "\\>");
            return u;
        }

        /// <summary>
        /// Formats a URI as a String for full Output
        /// </summary>
        /// <param name="u">URI</param>
        /// <returns></returns>
        public virtual String FormatUri(Uri u)
        {
            return this.FormatUri(u.AbsoluteUri);
        }

        /// <summary>
        /// Formats a Literal Node as a String for the given Format
        /// </summary>
        /// <param name="l">Literal Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns></returns>
        protected abstract String FormatLiteralNode(ILiteralNode l, TripleSegment? segment);

        /// <summary>
        /// Formats a Blank Node as a String for the given Format
        /// </summary>
        /// <param name="b">Blank Node</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns></returns>
        protected virtual String FormatBlankNode(IBlankNode b, TripleSegment? segment)
        {
            if (segment == TripleSegment.Predicate) throw new RdfOutputException(WriterErrorMessages.BlankPredicatesUnserializable(this._format));
            return b.ToString();
        }

        /// <summary>
        /// Formats a Variable Node as a String for the given Format
        /// </summary>
        /// <param name="v">Variable Name</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns></returns>
        protected virtual String FormatVariableNode(IVariableNode v, TripleSegment? segment)
        {
            throw new RdfOutputException(WriterErrorMessages.VariableNodesUnserializable(this._format));
        }

        /// <summary>
        /// Formats a Graph Literal Node as a String for the given Format
        /// </summary>
        /// <param name="glit">Graph Literal</param>
        /// <param name="segment">Triple Segment</param>
        /// <returns></returns>
        protected virtual String FormatGraphLiteralNode(IGraphLiteralNode glit, TripleSegment? segment)
        {
            throw new RdfOutputException(WriterErrorMessages.GraphLiteralsUnserializable(this._format));
        }

        /// <summary>
        /// Formats a Character for the given Format
        /// </summary>
        /// <param name="c">Character</param>
        /// <returns></returns>
        public virtual String FormatChar(char c)
        {
            return c.ToString();
        }

        /// <summary>
        /// Formats a SPARQL Result for the given format
        /// </summary>
        /// <param name="result">SPARQL Result</param>
        /// <returns></returns>
        public virtual String Format(SparqlResult result)
        {
            return result.ToString(this);
        }

        /// <summary>
        /// Formats a SPARQL Boolean Result for the given format
        /// </summary>
        /// <param name="result">Boolean Result</param>
        /// <returns></returns>
        public virtual String FormatBooleanResult(bool result)
        {
            return result.ToString().ToLower();
        }

        /// <summary>
        /// Gets the Name of the Format this Format uses
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.FormatName;
        }
    }
}
