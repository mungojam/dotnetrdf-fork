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

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Represents a fixed set of solutions
    /// </summary>
    public class Table
        : ITerminalOperator
    {
        private BaseMultiset _table;

        /// <summary>
        /// Creates a new fixed set of solutions
        /// </summary>
        /// <param name="table">Table</param>
        public Table(BaseMultiset table)
        {
            if (table == null) throw new ArgumentNullException("table");
            this._table = table;
        }

        /// <summary>
        /// Returns the fixed set of solutions
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            context.OutputMultiset = this._table;
            return context.OutputMultiset;
        }

        /// <summary>
        /// Gets the variables used in the algebra
        /// </summary>
        public IEnumerable<string> Variables
        {
            get
            {
                return this._table.Variables; 
            }
        }

        /// <summary>
        /// Throws an error as this cannot be converted back into a query
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            throw new NotSupportedException("Cannot convert Table to Query");
        }

        /// <summary>
        /// Throws an error as this cannot be converted back into a graph pattern
        /// </summary>
        /// <returns></returns>
        public Patterns.GraphPattern ToGraphPattern()
        {
            throw new NotSupportedException("Cannot convert Table to Graph Pattern");
        }

        /// <summary>
        /// Gets the string representation of the algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Table()";
        }
    }
}
