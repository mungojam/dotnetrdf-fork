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
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Query.PropertyFunctions;

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Algebra that represents the application of a Property Function
    /// </summary>
    public class PropertyFunction
        : IUnaryOperator
    {
        private ISparqlPropertyFunction _function;
        private ISparqlAlgebra _algebra;

        /// <summary>
        /// Creates a new Property function algebra
        /// </summary>
        /// <param name="algebra">Inner algebra</param>
        /// <param name="function">Property Function</param>
        public PropertyFunction(ISparqlAlgebra algebra, ISparqlPropertyFunction function)
        {
            this._function = function;
            this._algebra = algebra;
        }

        /// <summary>
        /// Gets the Inner Algebra
        /// </summary>
        public ISparqlAlgebra InnerAlgebra
        {
            get 
            {
                return this._algebra;
            }
        }

        /// <summary>
        /// Transforms this algebra with the given optimiser
        /// </summary>
        /// <param name="optimiser">Optimiser</param>
        /// <returns></returns>
        public ISparqlAlgebra Transform(IAlgebraOptimiser optimiser)
        {
            return new PropertyFunction(optimiser.Optimise(this._algebra), this._function);
        }

        /// <summary>
        /// Evaluates the algebra in the given context
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            context.InputMultiset = context.Evaluate(this._algebra);
            return this._function.Evaluate(context);
        }

        /// <summary>
        /// Gets the variables used in the algebra
        /// </summary>
        public IEnumerable<string> Variables
        {
            get 
            {
                return this._algebra.Variables.Concat(this._function.Variables).Distinct();
            }
        }

        /// <summary>
        /// Throws an error because property functions cannot be converted back to queries
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            throw new NotSupportedException("Property Functions cannot be converted back into queries");
        }

        /// <summary>
        /// Throws an error because property functions cannot be converted back to graph patterns
        /// </summary>
        /// <returns></returns>
        public GraphPattern ToGraphPattern()
        {
            throw new NotSupportedException("Property Functions cannot be converted back into Graph Patterns");
        }

        /// <summary>
        /// Gets the string representation of the algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "PropertyFunction(" + this._algebra.ToString() + "," + this._function.FunctionUri + ")";
        }
    }
}
