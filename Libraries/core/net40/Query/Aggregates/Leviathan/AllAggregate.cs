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
using VDS.RDF.Query.Expressions;
using VDS.RDF.Nodes;

namespace VDS.RDF.Query.Aggregates.Leviathan
{
    /// <summary>
    /// A Custom aggregate which requires the Expression to evaluate to true for all Sets in the Group
    /// </summary>
    public class AllAggregate
        : BaseAggregate
    {
        /// <summary>
        /// Creates a new All Aggregate
        /// </summary>
        /// <param name="expr">Expression</param>
        public AllAggregate(ISparqlExpression expr)
            : this(expr, false) { }

        /// <summary>
        /// Creates a new All Aggregate
        /// </summary>
        /// <param name="expr">Expression</param>
        /// <param name="distinct">Whether a DISTINCT modifier applies</param>
        public AllAggregate(ISparqlExpression expr, bool distinct)
            : base(expr, distinct) { }

        /// <summary>
        /// Applies the Aggregate to see if the expression evaluates true for every member of the Group
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="bindingIDs">Binding IDs</param>
        /// <returns></returns>
        /// <remarks>
        /// Does lazy evaluation - as soon as it encounters a false/error it will return false
        /// </remarks>
        public override IValuedNode Apply(SparqlEvaluationContext context, IEnumerable<int> bindingIDs)
        {
            foreach (int id in bindingIDs)
            {
                try
                {
                    if (!this._expr.Evaluate(context, id).AsSafeBoolean())
                    {
                        //As soon as we see a false we can return false
                        return new BooleanNode(null, false);
                    }
                }
                catch (RdfQueryException)
                {
                    //An error is a failure so we return false
                    return new BooleanNode(null, false);
                }
            }

            //If everything is true then we return true;
            return new BooleanNode(null, true);
        }

        /// <summary>
        /// Gets the String Representation of the Aggregate
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append('<');
            output.Append(LeviathanFunctionFactory.LeviathanFunctionsNamespace);
            output.Append(LeviathanFunctionFactory.All);
            output.Append(">(");
            if (this._distinct) output.Append("DISTINCT ");
            output.Append(this._expr.ToString());
            output.Append(')');
            return output.ToString();
        }

        /// <summary>
        /// Gets the Functor of the Aggregate
        /// </summary>
        public override string Functor
        {
            get
            {
                return LeviathanFunctionFactory.LeviathanFunctionsNamespace + LeviathanFunctionFactory.All;
            }
        }
    }
}
