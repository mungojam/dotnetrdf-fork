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
using VDS.RDF.Nodes;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Filters;
using VDS.RDF.Query.Optimisation;

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Algebra operator which combines a Filter and a Product into a single operation for improved performance and reduced memory usage
    /// </summary>
    public class FilteredProduct
        : IAbstractJoin
    {
        private ISparqlAlgebra _lhs, _rhs;
        private ISparqlExpression _expr;

        /// <summary>
        /// Creates a new Filtered Product
        /// </summary>
        /// <param name="lhs">LHS Algebra</param>
        /// <param name="rhs">RHS Algebra</param>
        /// <param name="expr">Expression to filter with</param>
        public FilteredProduct(ISparqlAlgebra lhs, ISparqlAlgebra rhs, ISparqlExpression expr)
        {
            this._lhs = lhs;
            this._rhs = rhs;
            this._expr = expr;
        }

        /// <summary>
        /// Gets the LHS Algebra
        /// </summary>
        public ISparqlAlgebra Lhs
        {
            get
            {
                return this._lhs;
            }
        }

        /// <summary>
        /// Gets the RHS Algebra
        /// </summary>
        public ISparqlAlgebra Rhs
        {
            get 
            {
                return this._rhs;
            }
        }

        /// <summary>
        /// Transforms the inner algebra with the given optimiser
        /// </summary>
        /// <param name="optimiser">Algebra Optimiser</param>
        /// <returns></returns>
        public ISparqlAlgebra Transform(IAlgebraOptimiser optimiser)
        {
            if (optimiser is IExpressionTransformer)
            {
                return new FilteredProduct(optimiser.Optimise(this._lhs), optimiser.Optimise(this._rhs), ((IExpressionTransformer)optimiser).Transform(this._expr));
            }
            else
            {
                return new FilteredProduct(optimiser.Optimise(this._lhs), optimiser.Optimise(this._rhs), this._expr);
            }
        }

        /// <summary>
        /// Transforms the LHS algebra only with the given optimiser
        /// </summary>
        /// <param name="optimiser">Algebra Optimiser</param>
        /// <returns></returns>
        public ISparqlAlgebra TransformLhs(IAlgebraOptimiser optimiser)
        {
            return new FilteredProduct(optimiser.Optimise(this._lhs), this._rhs, this._expr);
        }

        /// <summary>
        /// Transforms the RHS algebra only with the given optimiser
        /// </summary>
        /// <param name="optimiser">Algebra Optimiser</param>
        /// <returns></returns>
        public ISparqlAlgebra TransformRhs(Optimisation.IAlgebraOptimiser optimiser)
        {
            return new FilteredProduct(this._lhs, optimiser.Optimise(this._rhs), this._expr);
        }

        /// <summary>
        /// Evaluates the filtered product
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public BaseMultiset Evaluate(SparqlEvaluationContext context)
        {
            BaseMultiset initialInput = context.InputMultiset;
            BaseMultiset lhsResults = context.Evaluate(this._lhs);

            if (lhsResults is NullMultiset || lhsResults.IsEmpty)
            {
                //If LHS Results are Null/Empty then end result will always be null so short circuit
                context.OutputMultiset = new NullMultiset();
            }
            else
            {

                context.InputMultiset = initialInput;
                BaseMultiset rhsResults = context.Evaluate(this._rhs);
                if (rhsResults is NullMultiset || rhsResults.IsEmpty)
                {
                    //If RHS Results are Null/Empty then end results will always be null so short circuit
                    context.OutputMultiset = new NullMultiset();
                }
                else if (rhsResults is IdentityMultiset)
                {
                    //Apply Filter over LHS Results only - defer evaluation to filter implementation
                    context.InputMultiset = lhsResults;
                    UnaryExpressionFilter filter = new UnaryExpressionFilter(this._expr);
                    filter.Evaluate(context);
                    context.OutputMultiset = lhsResults;
                }
                else
                {
                    //Calculate the product applying the filter as we go
#if NET40 && !SILVERLIGHT
                    if (Options.UsePLinqEvaluation && this._expr.CanParallelise)
                    {
                        PartitionedMultiset partitionedSet;
                        SparqlResultBinder binder = context.Binder;
                        if (lhsResults.Count >= rhsResults.Count)
                        {
                            partitionedSet = new PartitionedMultiset(lhsResults.Count, rhsResults.Count);
                            context.Binder = new LeviathanLeftJoinBinder(partitionedSet);
                            lhsResults.Sets.AsParallel().ForAll(x => this.EvalFilteredProduct(context, x, rhsResults, partitionedSet));
                        }
                        else
                        {
                            partitionedSet = new PartitionedMultiset(rhsResults.Count, lhsResults.Count);
                            context.Binder = new LeviathanLeftJoinBinder(partitionedSet);
                            rhsResults.Sets.AsParallel().ForAll(y => this.EvalFilteredProduct(context, y, lhsResults, partitionedSet));
                        }

                        context.Binder = binder;
                        context.OutputMultiset = partitionedSet;
                    }
                    else
                    {
#endif
                        BaseMultiset productSet = new Multiset();
                        SparqlResultBinder binder = context.Binder;
                        context.Binder = new LeviathanLeftJoinBinder(productSet);
                        foreach (ISet x in lhsResults.Sets)
                        {
                            foreach (ISet y in rhsResults.Sets)
                            {
                                ISet z = x.Join(y);
                                productSet.Add(z);
                                try
                                {
                                    if (!this._expr.Evaluate(context, z.ID).AsSafeBoolean())
                                    {
                                        //Means the expression evaluates to false so we discard the solution
                                        productSet.Remove(z.ID);
                                    }
                                }
                                catch
                                {
                                    //Means this solution does not meet the FILTER and can be discarded
                                    productSet.Remove(z.ID);
                                }
                            }
                            //Remember to check for timeouts occassionaly
                            context.CheckTimeout();
                        }
                        context.Binder = binder;
                        context.OutputMultiset = productSet;
#if NET40 && !SILVERLIGHT
                    }
#endif
                }
            }
            return context.OutputMultiset;
        }

#if NET40 && !SILVERLIGHT

        private void EvalFilteredProduct(SparqlEvaluationContext context, ISet x, BaseMultiset other, PartitionedMultiset partitionedSet)
        {
            int id = partitionedSet.GetNextBaseID();
            foreach (ISet y in other.Sets)
            {
                id++;
                ISet z = x.Join(y);
                z.ID = id;
                partitionedSet.Add(z);
                try
                {
                    if (!this._expr.Evaluate(context, z.ID).AsSafeBoolean())
                    {
                        //Means the expression evaluates to false so we discard the solution
                        partitionedSet.Remove(z.ID);
                    }
                }
                catch
                {
                    //Means the solution does not meet the FILTER and can be discarded
                    partitionedSet.Remove(z.ID);
                }
            }
            //Remember to check for timeouts occassionally
            context.CheckTimeout();
        }

#endif

        /// <summary>
        /// Gets the Variables used in the Algebra
        /// </summary>
        public IEnumerable<string> Variables
        {
            get
            {
                return this._lhs.Variables.Concat(this._rhs.Variables).Concat(this._expr.Variables).Distinct();
            }
        }

        /// <summary>
        /// Converts the algebra back into a query
        /// </summary>
        /// <returns></returns>
        public SparqlQuery ToQuery()
        {
            ISparqlAlgebra algebra = new Filter(new Join(this._lhs, this._rhs), new UnaryExpressionFilter(this._expr));
            return algebra.ToQuery();
        }

        /// <summary>
        /// Converts the algebra back into a Graph Pattern
        /// </summary>
        /// <returns></returns>
        public Patterns.GraphPattern ToGraphPattern()
        {
            ISparqlAlgebra algebra = new Filter(new Join(this._lhs, this._rhs), new UnaryExpressionFilter(this._expr));
            return algebra.ToGraphPattern();
        }

        /// <summary>
        /// Gets the string represenation of the algebra
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "FilteredProduct(" + this._lhs.ToString() + ", " + this._rhs.ToString() + ", " + this._expr.ToString() + ")";
        }
    }
}
