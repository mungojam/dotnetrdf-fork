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

namespace VDS.RDF.Query.Algebra
{
    /// <summary>
    /// Represents one possible set of values which is a solution to the query
    /// </summary>
    public sealed class Set 
        : BaseSet, IEquatable<Set>
    {
        private Dictionary<String, INode> _values;

        /// <summary>
        /// Creates a new Set
        /// </summary>
        public Set()
        {
            this._values = new Dictionary<string, INode>();
        }

        /// <summary>
        /// Creates a new Set which is the Join of the two Sets
        /// </summary>
        /// <param name="x">A Set</param>
        /// <param name="y">A Set</param>
        internal Set(ISet x, ISet y)
        {
            this._values = new Dictionary<string, INode>();
            foreach (String var in x.Variables)
            {
                this._values.Add(var, x[var]);
            }
            foreach (String var in y.Variables)
            {
                if (!this._values.ContainsKey(var))
                {
                    this._values.Add(var, y[var]);
                }
                else if (this._values[var] == null)
                {
                    this._values[var] = y[var];
                }
            }
        }

        //internal Set(JoinedSet x)
        //{
        //    this._values = new Dictionary<string, INode>();
        //    foreach (String var in x.Variables)
        //    {
        //        this._values.Add(var, x[var]);
        //    }
        //}

        /// <summary>
        /// Creates a new Set which is a copy of an existing Set
        /// </summary>
        /// <param name="x">Set to copy</param>
        internal Set(ISet x)
        {
            this._values = new Dictionary<string, INode>();
            foreach (String var in x.Variables)
            {
                this._values.Add(var, x[var]);
            }
        }

        /// <summary>
        /// Creates a new Set from a SPARQL Result
        /// </summary>
        /// <param name="result">Result</param>
        internal Set(SparqlResult result)
        {
            this._values = new Dictionary<string, INode>();
            foreach (String var in result.Variables)
            {
                this.Add(var, result[var]);
            }
        }

        /// <summary>
        /// Creates a new Set from a Binding Tuple
        /// </summary>
        /// <param name="tuple">Tuple</param>
        internal Set(BindingTuple tuple)
        {
            this._values = new Dictionary<string, INode>();
            foreach (KeyValuePair<String, PatternItem> binding in tuple.Values)
            {
                this.Add(binding.Key, tuple[binding.Key]);
            }
        }

        /// <summary>
        /// Retrieves the Value in this set for the given Variable
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <returns>Either a Node or a null</returns>
        public override INode this[String variable]
        {
            get
            {
                if (this._values.ContainsKey(variable))
                {
                    return this._values[variable];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Adds a Value for a Variable to the Set
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <param name="value">Value</param>
        public override void Add(String variable, INode value)
        {
            if (!this._values.ContainsKey(variable))
            {
                this._values.Add(variable, value);
            }
            else
            {
                throw new RdfQueryException("The value of a variable in a Set cannot be changed");
            }
        }

        /// <summary>
        /// Removes a Value for a Variable from the Set
        /// </summary>
        /// <param name="variable">Variable</param>
        public override void Remove(String variable)
        {
            if (this._values.ContainsKey(variable)) this._values.Remove(variable);
        }

        /// <summary>
        /// Checks whether the Set contains a given Variable
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <returns></returns>
        public override bool ContainsVariable(String variable)
        {
            return this._values.ContainsKey(variable);
        }

        /// <summary>
        /// Gets whether the Set is compatible with a given set based on the given variables
        /// </summary>
        /// <param name="s">Set</param>
        /// <param name="vars">Variables</param>
        /// <returns></returns>
        public override bool IsCompatibleWith(ISet s, IEnumerable<string> vars)
        {
            return vars.All(v => this[v] == null || s[v] == null || this[v].Equals(s[v]));
        }

        /// <summary>
        /// Gets whether the Set is minus compatible with a given set based on the given variables
        /// </summary>
        /// <param name="s">Set</param>
        /// <param name="vars">Variables</param>
        /// <returns></returns>
        public override bool IsMinusCompatibleWith(ISet s, IEnumerable<string> vars)
        {
            return vars.Any(v => this[v] != null && this[v].Equals(s[v]));
        }

        /// <summary>
        /// Gets the Variables in the Set
        /// </summary>
        public override IEnumerable<String> Variables
        {
            get
            {
                return (from var in this._values.Keys
                        select var);
            }
        }

        /// <summary>
        /// Gets the Values in the Set
        /// </summary>
        public override IEnumerable<INode> Values
        {
            get
            {
                return (from value in this._values.Values
                        select value);
            }
        }

        /// <summary>
        /// Joins the set to another set
        /// </summary>
        /// <param name="other">Other Set</param>
        /// <returns></returns>
        public override ISet Join(ISet other)
        {
            return new Set(this, other);
            //return new JoinedSet(other, this);
        }

        /// <summary>
        /// Copies the Set
        /// </summary>
        /// <returns></returns>
        public override ISet Copy()
        {
            return new Set(this);
            //return new JoinedSet(this);
        }



        /// <summary>
        /// Gets whether the Set is equal to another set
        /// </summary>
        /// <param name="other">Set to compare with</param>
        /// <returns></returns>
        public bool Equals(Set other)
        {
            if (other == null) return false;
            return this._values.All(pair => other.ContainsVariable(pair.Key) && ((pair.Value == null && other[pair.Key] == null) || pair.Value.Equals(other[pair.Key])));
        }
    }

#if EXPERIMENTAL

    /// <summary>
    /// Represents one possible set of values which is a solution to the query where those values are the result of joining one or more possible sets
    /// </summary>
    public sealed class JoinedSet
        : BaseSet, IEquatable<JoinedSet>
    {
        private List<ISet> _sets = new List<ISet>();
        private bool _added = false;
        private Dictionary<String, INode> _cache = new Dictionary<string, INode>();

        /// <summary>
        /// Creates a Joined Set
        /// </summary>
        /// <param name="x">Set</param>
        /// <param name="y">Another Set</param>
        public JoinedSet(ISet x, ISet y)
        {
            this._sets.Add(x);
            this._sets.Add(y);
        }

        public JoinedSet(ISet x, JoinedSet y)
        {
            this._sets.Add(x);
            this._sets.AddRange(y._sets);
            this._cache = y._cache;
        }

        /// <summary>
        /// Creates a Joined Set
        /// </summary>
        /// <param name="x">Set</param>
        /// <param name="ys">Other Set(s)</param>
        internal JoinedSet(ISet x, IEnumerable<ISet> ys)
        {
            this._sets.Add(x);
            this._sets.AddRange(ys);
        }

        /// <summary>
        /// Creates a Joined Set which is simply a copy of another set
        /// </summary>
        /// <param name="x">Set</param>
        internal JoinedSet(ISet x)
        {
            this._sets.Add(x);
        }

        /// <summary>
        /// Creates a Joined Set
        /// </summary>
        /// <param name="x">Set</param>
        internal JoinedSet(JoinedSet x)
        {
            this._sets.AddRange(x._sets);
            this._cache = x._cache;
        }

        /// <summary>
        /// Adds a Value for a Variable to the Set
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <param name="value">Value</param>
        public override void Add(string variable, INode value)
        {
            //When we first add to the joined set we create a new empty set to make the adds into as
            //we cannot add into the existing sets since they may well be being used in multiple
            //places and trying to do so will break things horribly
            if (!this._added)
            {
                this._sets.Insert(0, new Set());
                //this._sets.Add(new Set());
                this._added = true;
            }
            //Joined Sets are thus left associative so always add to the leftmost set
            this._sets[0].Add(variable, value);
            //this._sets[this._sets.Count - 1].Add(variable, value);
        }

        /// <summary>
        /// Checks whether the Set contains a given Variable
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <returns></returns>
        public override bool ContainsVariable(string variable)
        {
            return this._sets.Any(s => s.ContainsVariable(variable));
        }

        /// <summary>
        /// Gets whether the Set is compatible with a given set based on the given variables
        /// </summary>
        /// <param name="s">Set</param>
        /// <param name="vars">Variables</param>
        /// <returns></returns>
        public override bool IsCompatibleWith(ISet s, IEnumerable<string> vars)
        {
            return vars.All(v => this[v] == null || s[v] == null || this[v].Equals(s[v]));
        }

        /// <summary>
        /// Removes a Value for a Variable from the Set
        /// </summary>
        /// <param name="variable">Variable</param>
        public override void Remove(string variable)
        {
            this._sets.ForEach(s => s.Remove(variable));
        }

        /// <summary>
        /// Retrieves the Value in this set for the given Variable
        /// </summary>
        /// <param name="variable">Variable</param>
        /// <returns>Either a Node or a null</returns>
        public override INode this[string variable]
        {
            get
            {
                INode temp = null;

                if (this._cache.TryGetValue(variable, out temp))
                {
                    //Use cache wherever possible
                    return temp;
                }
                else
                {
                    int i = 0;

                    //Find the first set that has a value for the variable and return it
                    do
                    {
                        temp = this._sets[i][variable];
                        if (temp != null)
                        {
                            this._cache.Add(variable, temp);
                            return temp;
                        }
                        i++;
                    } while (i < this._sets.Count);

                    //Return null if no sets have a value for the variable
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the Values in the Set
        /// </summary>
        public override IEnumerable<INode> Values
        {
            get
            {
                return (from v in this.Variables
                        select this[v]);
            }
        }

        /// <summary>
        /// Gets the Variables in the Set
        /// </summary>
        public override IEnumerable<string> Variables
        {
            get
            {
                return (from s in this._sets
                        from v in s.Variables
                        select v).Distinct();
            }
        }

        /// <summary>
        /// Joins the set to another set
        /// </summary>
        /// <param name="other">Other Set</param>
        /// <returns></returns>
        public override ISet Join(ISet other)
        {
            //return new Set(this, other);
            //if (this._sets.Count > 3)
            //{
            //    //After a certain point it is better to flatten
            //    return new JoinedSet(other, new Set(this));
            //}
            //else
            //{
                return new JoinedSet(other, this);
            //}
        }

        /// <summary>
        /// Copies the Set
        /// </summary>
        /// <returns></returns>
        public override ISet Copy()
        {
            //return new Set(this);
            return new JoinedSet(this);
        }

        /// <summary>
        /// Gets the Hash Code of the Set
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Gets the String representation of the Set
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            foreach (String v in this.Variables)
            {
                if (output.Length > 0) output.Append(" , ");
                output.Append("?" + v + " = " + this[v].ToSafeString());
            }
            return output.ToString();
        }

        /// <summary>
        /// Gets whether the Set is equal to another set
        /// </summary>
        /// <param name="other">Set to compare with</param>
        /// <returns></returns>
        public bool Equals(JoinedSet other)
        {
            return this.Equals((ISet)other);
        }
    }
    
#endif
}
