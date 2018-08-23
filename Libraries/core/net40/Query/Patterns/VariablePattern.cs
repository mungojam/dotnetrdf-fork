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
using System.Text;
using VDS.RDF.Query.Construct;

namespace VDS.RDF.Query.Patterns
{
    /// <summary>
    /// Pattern which matches Variables
    /// </summary>
    public class VariablePattern 
        : PatternItem
    {
        private String _varname;

        /// <summary>
        /// Creates a new Variable Pattern
        /// </summary>
        /// <param name="name">Variable name</param>
        public VariablePattern(String name)
        {
            this._varname = name;

            //Strip leading ?/$ if present
            if (this._varname.StartsWith("?") || this._varname.StartsWith("$"))
            {
                this._varname = this._varname.Substring(1);
            }
        }

        /// <summary>
        /// Checks whether the given Node is a valid value for the Variable in the current Binding Context
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="obj">Node to test</param>
        /// <returns></returns>
        protected internal override bool Accepts(SparqlEvaluationContext context, INode obj)
        {
            if (Options.RigorousEvaluation)
            {
                if (context.InputMultiset.ContainsVariable(this._varname))
                {
                    return context.InputMultiset.ContainsValue(this._varname, obj);
                }
                else if (this.Repeated)
                {
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Constructs a Node based on the given Set
        /// </summary>
        /// <param name="context">Construct Context</param>
        /// <returns>The Node which is bound to this Variable in this Solution</returns>
        protected internal override INode Construct(ConstructContext context)
        {
            INode value = context.Set[this._varname];

            if (value == null) throw new RdfQueryException("Unable to construct a Value for this Variable for this solution as it is bound to a null");
            switch (value.NodeType)
            {
                case NodeType.Blank:
                    if (!context.PreserveBlankNodes && value.GraphUri != null)
                    {
                        //Rename Blank Node based on the Graph Uri Hash Code
                        int hash = value.GraphUri.GetEnhancedHashCode();
                        if (hash >= 0)
                        {
                            return new BlankNode(context.Graph, ((IBlankNode)value).InternalID + "-" + value.GraphUri.GetEnhancedHashCode());
                        }
                        else
                        {
                            return new BlankNode(context.Graph, ((IBlankNode)value).InternalID + hash);
                        }
                    }
                    else
                    {
                        return new BlankNode(context.Graph, ((IBlankNode)value).InternalID);
                    }

                default:
                    return context.GetNode(value);
            }
        }

        /// <summary>
        /// Gets the String representation of this pattern
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "?" + this._varname;
        }

        /// <summary>
        /// Gets the Name of the Variable this Pattern matches
        /// </summary>
        public override string VariableName
        {
            get
            {
                return this._varname;
            }
        }
    }
}
