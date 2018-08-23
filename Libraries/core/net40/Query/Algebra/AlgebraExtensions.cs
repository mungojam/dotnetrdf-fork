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
    /// Static Helper class containing extensions used in the Algebra evaluation process
    /// </summary>
    public static class AlgebraExtensions
    {
        /// <summary>
        /// Calculates the product of two mutlisets asynchronously with a timeout to restrict long running computations
        /// </summary>
        /// <param name="multiset">Multiset</param>
        /// <param name="other">Other Multiset</param>
        /// <param name="timeout">Timeout, if &lt;=0 no timeout is used and product will be computed sychronously</param>
        /// <returns></returns>
        public static BaseMultiset ProductWithTimeout(this BaseMultiset multiset, BaseMultiset other, long timeout)
        {
            if (other is IdentityMultiset) return multiset;
            if (other is NullMultiset) return other;
            if (other.IsEmpty) return new NullMultiset();

            //If no timeout use default implementation
            if (timeout <= 0)
            {
                return multiset.Product(other);
            }

            //Otherwise Invoke using an Async call
            BaseMultiset productSet;
#if NET40 && !SILVERLIGHT
            if (Options.UsePLinqEvaluation)
            {
                if (multiset.Count >= other.Count)
                {
                    productSet = new PartitionedMultiset(multiset.Count, other.Count);
                }
                else
                {
                    productSet = new PartitionedMultiset(other.Count, multiset.Count);
                }
            }
            else
            {
#endif
                productSet = new Multiset();
#if NET40 && !SILVERLIGHT
            }
#endif
            StopToken stop = new StopToken();
            GenerateProductDelegate d = new GenerateProductDelegate(GenerateProduct);
            IAsyncResult r = d.BeginInvoke(multiset, other, productSet, stop, null, null);

            //Wait
            int t = (int)Math.Min(timeout, Int32.MaxValue);
            r.AsyncWaitHandle.WaitOne(t);
            if (!r.IsCompleted)
            {
                stop.ShouldStop = true;
                r.AsyncWaitHandle.WaitOne();
            }
            return productSet;
        }

        /// <summary>
        /// Delegate for generating product of two multisets asynchronously
        /// </summary>
        /// <param name="multiset">Multiset</param>
        /// <param name="other">Other Multiset</param>
        /// <param name="target">Mutliset to generate the product in</param>
        /// <param name="stop">Stop Token</param>
        private delegate void GenerateProductDelegate(BaseMultiset multiset, BaseMultiset other, BaseMultiset target, StopToken stop);

        /// <summary>
        /// Method for generating product of two multisets asynchronously
        /// </summary>
        /// <param name="multiset">Multiset</param>
        /// <param name="other">Other Multiset</param>
        /// <param name="target">Mutliset to generate the product in</param>
        /// <param name="stop">Stop Token</param>
        private static void GenerateProduct(BaseMultiset multiset, BaseMultiset other, BaseMultiset target, StopToken stop)
        {
#if NET40 && !SILVERLIGHT
            if (Options.UsePLinqEvaluation)
            {
                //Determine partition sizes so we can do a parallel product
                //Want to parallelize over whichever side is larger
                if (multiset.Count >= other.Count)
                {
                    multiset.Sets.AsParallel().ForAll(x => EvalProduct(x, other, target as PartitionedMultiset, stop));
                }
                else
                {
                    other.Sets.AsParallel().ForAll(y => EvalProduct(y, multiset, target as PartitionedMultiset, stop));
                }
            }
            else
            {
#endif
                foreach (ISet x in multiset.Sets)
                {
                    foreach (ISet y in other.Sets)
                    {
                        target.Add(x.Join(y));
                        //if (stop.ShouldStop) break;
                    }
                    if (stop.ShouldStop) break;
                }
#if NET40 && !SILVERLIGHT
            }
#endif
        }

#if NET40 && !SILVERLIGHT
        private static void EvalProduct(ISet x, BaseMultiset other, PartitionedMultiset productSet, StopToken stop)
        {
            if (stop.ShouldStop) return;
            int id = productSet.GetNextBaseID();
            foreach (ISet y in other.Sets)
            {
                id++;
                ISet z = x.Join(y);
                z.ID = id;
                productSet.Add(z);
            }
            if (stop.ShouldStop) return;
        }
#endif
    }

    /// <summary>
    /// Token passed to asynchronous code to allow stop signalling
    /// </summary>
    class StopToken
    {
        private bool _stop = false;

        /// <summary>
        /// Gets/Sets whether the code should stop
        /// </summary>
        /// <remarks>
        /// Once set to true cannot be reset
        /// </remarks>
        public bool ShouldStop
        {
            get 
            {
                return this._stop;
            }
            set 
            {
                if (!this._stop) this._stop = value;
            }
        }
    }
}
