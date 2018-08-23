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

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Represents a Distinct modifier on a SPARQL Query
    /// </summary>
    public class Distinct
        : IUnaryOperator
    {
        private ISparqlAlgebra _pattern;
        private bool _trimTemporaryVariables = true;

        /// <summary>
        /// Creates a new Distinct Modifier
        /// </summary>
        /// <param name="pattern">Pattern</param>
        public Distinct(ISparqlAlgebra pattern)
        {
            this._pattern = pattern;
        }

        /// <summary>
        /// Creates a new Distinct Modifier
        /// </summary>
        /// <param name="algebra">Inner Algebra</param>
        /// <param name="ignoreTemporaryVariables">Whether to ignore temporary variables</param>
        public Distinct(ISparqlAlgebra algebra, bool ignoreTemporaryVariables)
            : this(algebra)
        {
            this._trimTemporaryVariables = !ignoreTemporaryVariables;
        }

        /// <summary>
        /// Evaluates the Distinct Modifier
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            context.InputMultiset = context.Evaluate(this._pattern);

            if (context.InputMultiset is IdentityMultiset || context.InputMultiset is NullMultiset)
            {
                context.OutputMultiset = context.InputMultiset;
                return context.OutputMultiset;
            }
            else
            {
                if (this._trimTemporaryVariables)
                {
                    //Trim temporary variables
                    context.InputMultiset.Trim();
                }

                //Apply distinctness
                context.OutputMultiset = new Multiset(context.InputMultiset.Variables);
                IEnumerable<ISet> sets = context.InputMultiset.Sets.Distinct();
                foreach (ISet s in sets)
                {
                    context.OutputMultiset.Add(s.Copy());
                }
                return context.OutputMultiset;
            }
        }

        /// <summary>
        /// Gets the Variables used in the Algebra
        /// </summary>
        public IEnumerable<String> Variables
        {
            get
            {
                return this._pattern.Variables;
            }
        }

        /// <summary>
        /// Gets the Inner Algebra
        /// </summary>
        public ISparqlAlgebra InnerAlgebra
        {
            get
            {
                return this._pattern;
            }
        }

        /// <summary>
        /// Gets the String representation of the Algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Distinct(" + this._pattern.ToString() + ")";
        }

        /// <summary>
        /// Converts the Algebra back to a SPARQL Query
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            SparqlQuery q = this._pattern.ToQuery();
            switch (q.QueryType)
            {
                case SparqlQueryType.Select:
                    q.QueryType = SparqlQueryType.SelectDistinct;
                    break;
                case SparqlQueryType.SelectAll:
                    q.QueryType = SparqlQueryType.SelectAllDistinct;
                    break;
                case SparqlQueryType.SelectAllDistinct:
                case SparqlQueryType.SelectAllReduced:
                case SparqlQueryType.SelectDistinct:
                case SparqlQueryType.SelectReduced:
                    throw new NotSupportedException("Cannot convert a Distinct() to a SPARQL Query when the Inner Algebra converts to a Query that already has a DISTINCT/REDUCED modifier applied");
                case SparqlQueryType.Ask:
                case SparqlQueryType.Construct:
                case SparqlQueryType.Describe:
                case SparqlQueryType.DescribeAll:
                    throw new NotSupportedException("Cannot convert a Distinct() to a SPARQL Query when the Inner Algebra converts to a Query that is not a SELECT query");
                case SparqlQueryType.Unknown:
                    q.QueryType = SparqlQueryType.SelectDistinct;
                    break;
                default:
                    throw new NotSupportedException("Cannot convert a Distinct() to a SPARQL Query when the Inner Algebra converts to a Query with an unexpected Query Type");
            }
            return q;
        }

        /// <summary>
        /// Throws an exception since a Distinct() cannot be converted back to a Graph Pattern
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Thrown since a Distinct() cannot be converted to a Graph Pattern</exception>
        public GraphPattern ToGraphPattern()
        {
            throw new NotSupportedException("A Distinct() cannot be converted to a GraphPattern");
        }

        /// <summary>
        /// Transforms the Inner Algebra using the given Optimiser
        /// </summary>
        /// <param name="optimiser">Optimiser</param>
        /// <returns></returns>
        public ISparqlAlgebra Transform(IAlgebraOptimiser optimiser)
        {
            return new Distinct(optimiser.Optimise(this._pattern));
        }
    }

    /// <summary>
    /// Represents a Reduced modifier on a SPARQL Query
    /// </summary>
    public class Reduced 
        : IUnaryOperator
    {
        private ISparqlAlgebra _pattern;

        /// <summary>
        /// Creates a new Reduced Modifier
        /// </summary>
        /// <param name="pattern">Pattern</param>
        public Reduced(ISparqlAlgebra pattern)
        {
            this._pattern = pattern;
        }

        /// <summary>
        /// Evaluates the Reduced Modifier
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            context.InputMultiset = context.Evaluate(this._pattern);//this._pattern.Evaluate(context);

            if (context.InputMultiset is IdentityMultiset || context.InputMultiset is NullMultiset)
            {
                context.OutputMultiset = context.InputMultiset;
                return context.OutputMultiset;
            }
            else
            {
                if (context.Query.Limit > 0)
                {
                    context.OutputMultiset = new Multiset(context.InputMultiset.Variables);
                    foreach (ISet s in context.InputMultiset.Sets.Distinct())
                    {
                        context.OutputMultiset.Add(s.Copy());
                    }
                }
                else
                {
                    context.OutputMultiset = context.InputMultiset;
                }
                return context.OutputMultiset;
            }
        }

        /// <summary>
        /// Gets the Variables used in the Algebra
        /// </summary>
        public IEnumerable<String> Variables
        {
            get
            {
                return this._pattern.Variables.Distinct();
            }
        }

        /// <summary>
        /// Gets the Inner Algebra
        /// </summary>
        public ISparqlAlgebra InnerAlgebra
        {
            get
            {
                return this._pattern;
            }
        }

        /// <summary>
        /// Gets the String representation of the Algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Reduced(" + this._pattern.ToString() + ")";
        }

        /// <summary>
        /// Converts the Algebra back to a SPARQL Query
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            SparqlQuery q = this._pattern.ToQuery();
            switch (q.QueryType)
            {
                case SparqlQueryType.Select:
                    q.QueryType = SparqlQueryType.SelectReduced;
                    break;
                case SparqlQueryType.SelectAll:
                    q.QueryType = SparqlQueryType.SelectAllReduced;
                    break;
                case SparqlQueryType.SelectAllDistinct:
                case SparqlQueryType.SelectAllReduced:
                case SparqlQueryType.SelectDistinct:
                case SparqlQueryType.SelectReduced:
                    throw new NotSupportedException("Cannot convert a Reduced() to a SPARQL Query when the Inner Algebra converts to a Query that already has a DISTINCT/REDUCED modifier applied");
                case SparqlQueryType.Ask:
                case SparqlQueryType.Construct:
                case SparqlQueryType.Describe:
                case SparqlQueryType.DescribeAll:
                    throw new NotSupportedException("Cannot convert a Reduced() to a SPARQL Query when the Inner Algebra converts to a Query that is not a SELECT query");
                case SparqlQueryType.Unknown:
                    q.QueryType = SparqlQueryType.SelectReduced;
                    break;
                default:
                    throw new NotSupportedException("Cannot convert a Reduced() to a SPARQL Query when the Inner Algebra converts to a Query with an unexpected Query Type");
            }
            return q;
        }

        /// <summary>
        /// Throws an exception since a Reduced() cannot be converted back to a Graph Pattern
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Thrown since a Reduced() cannot be converted to a Graph Pattern</exception>
        public GraphPattern ToGraphPattern()
        {
            throw new NotSupportedException("A Reduced() cannot be converted to a GraphPattern");
        }

        /// <summary>
        /// Transforms the Inner Algebra using the given Optimiser
        /// </summary>
        /// <param name="optimiser">Optimiser</param>
        /// <returns></returns>
        public ISparqlAlgebra Transform(IAlgebraOptimiser optimiser)
        {
            return new Reduced(optimiser.Optimise(this._pattern));
        }
    }
}
