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
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Query.Patterns;

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Represents a BINDINGS modifier on a SPARQL Query
    /// </summary>
    public class Bindings
        : ITerminalOperator
    {
        private BindingsPattern _bindings;
        private BaseMultiset _mset;

        /// <summary>
        /// Creates a new BINDINGS modifier
        /// </summary>
        /// <param name="bindings">Bindings</param>
        public Bindings(BindingsPattern bindings)
        {
            if (bindings == null) throw new ArgumentNullException("Null Bindings");
            this._bindings = bindings;
        }

        /// <summary>
        /// Evaluates the BINDINGS modifier
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            if (this._mset == null)
            {
                this._mset = this._bindings.ToMultiset();
            }
            return this._mset;
        }

        /// <summary>
        /// Gets the Variables used in the Algebra
        /// </summary>
        public IEnumerable<String> Variables
        {
            get
            {
                return this._bindings.Variables;
            }
        }

        /// <summary>
        /// Gets the Bindings 
        /// </summary>
        public BindingsPattern BindingsPattern
        {
            get
            {
                return this._bindings;
            }
        }

        /// <summary>
        /// Gets the String representation of the Algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Bindings()";
        }

        /// <summary>
        /// Converts the Algebra back to a SPARQL Query
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            GraphPattern gp = this.ToGraphPattern();
            SparqlQuery q = new SparqlQuery();
            q.RootGraphPattern = gp;
            return q;
        }

        /// <summary>
        /// Convers the Algebra back to a Graph Pattern
        /// </summary>
        /// <returns></returns>
        public GraphPattern ToGraphPattern()
        {
            GraphPattern gp = new GraphPattern();
            gp.AddInlineData(this._bindings);
            return gp;
        }

        ///// <summary>
        ///// Transforms the Inner Algebra using the given Optimiser
        ///// </summary>
        ///// <param name="optimiser">Optimiser</param>
        ///// <returns></returns>
        //public ISparqlAlgebra Transform(IAlgebraOptimiser optimiser)
        //{
        //    return this;
        //}
    }
}
