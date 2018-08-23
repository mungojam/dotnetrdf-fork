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
using VDS.RDF.Query.Patterns;
using VDS.RDF.Query.Algebra;

namespace VDS.RDF.Query.Paths
{
    /// <summary>
    /// Transform Context class that is used in the Path to Algebra Transformation process
    /// </summary>
    public class PathTransformContext
    {
        private List<ITriplePattern> _patterns = new List<ITriplePattern>();
        private int _nextID = 0;
        private PatternItem _currSubj, _currObj, _start, _end;
        private bool _top = true;

        /// <summary>
        /// Creates a new Path Transform Context
        /// </summary>
        /// <param name="start">Subject that is the start of the Path</param>
        /// <param name="end">Object that is the end of the Path</param>
        public PathTransformContext(PatternItem start, PatternItem end)
        {
            this._start = start;
            this._currSubj = start;
            this._end = end;
            this._currObj = end;
        }

        /// <summary>
        /// Creates a new Path Transform Context from an existing context
        /// </summary>
        /// <param name="context">Context</param>
        public PathTransformContext(PathTransformContext context)
        {
            this._start = context._start;
            this._end = context._end;
            this._currSubj = context._currSubj;
            this._currObj = context._currObj;
            this._nextID = context._nextID;
        }

        /// <summary>
        /// Returns the BGP that the Path Transform produces
        /// </summary>
        /// <returns></returns>
        public ISparqlAlgebra ToAlgebra()
        {
            if (this._patterns.Count > 0)
            {
                return new Bgp(this._patterns);
            }
            else
            {
                throw new RdfQueryException("Unexpected Error: Path Transform returned no Patterns");
            }
        }

        /// <summary>
        /// Gets the next available temporary variable
        /// </summary>
        /// <returns></returns>
        public BlankNodePattern GetNextTemporaryVariable()
        {
            this._nextID++;
            return new BlankNodePattern("sparql-path-autos-" + this._nextID);
        }

        /// <summary>
        /// Adds a Triple Pattern to the Path Transform
        /// </summary>
        /// <param name="p">Triple Pattern</param>
        public void AddTriplePattern(ITriplePattern p)
        {
            this._patterns.Add(p);
        }

        /// <summary>
        /// Gets the Next ID to be used
        /// </summary>
        public int NextID
        {
            get
            {
                return this._nextID;
            }
            set
            {
                this._nextID = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Subject of the Triple Pattern at this point in the Path Transformation
        /// </summary>
        public PatternItem Subject
        {
            get
            {
                return this._currSubj;
            }
            set
            {
                this._currSubj = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Object of the Triple Pattern at this point in the Path Transformation
        /// </summary>
        public PatternItem Object
        {
            get
            {
                return this._currObj;
            }
            set
            {
                this._currObj = value;
            }
        }

        /// <summary>
        /// Gets/Sets the Object at the end of the Pattern
        /// </summary>
        public PatternItem End
        {
            get
            {
                return this._end;
            }
            set
            {
                this._end = value;
            }
        }

        /// <summary>
        /// Resets the current Object to be the end Object of the Path
        /// </summary>
        public void ResetObject()
        {
            this._currObj = this._end;
        }

        /// <summary>
        /// Gets/Sets whether this is the Top Level Pattern
        /// </summary>
        public bool Top
        {
            get
            {
                return this._top;
            }
            set
            {
                this._top = value;
            }
        }

        /// <summary>
        /// Creates a Triple Pattern
        /// </summary>
        /// <param name="subj">Subject</param>
        /// <param name="path">Property Path</param>
        /// <param name="obj">Object</param>
        /// <returns></returns>
        public ITriplePattern GetTriplePattern(PatternItem subj, ISparqlPath path, PatternItem obj)
        {
            if (path is Property)
            {
                NodeMatchPattern nodeMatch = new NodeMatchPattern(((Property)path).Predicate);
                return new TriplePattern(subj, nodeMatch, obj);
            }
            else
            {
                return new PropertyPathPattern(subj, path, obj);
            }
        }
    }
}
