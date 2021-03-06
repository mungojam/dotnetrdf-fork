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
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query.Filters;

namespace VDS.RDF.Query.Grouping
{
    /// <summary>
    /// Abstract Base Class for classes representing Sparql GROUP BY clauses
    /// </summary>
    public abstract class BaseGroupBy
        : ISparqlGroupBy
    {
        /// <summary>
        /// Child Grouping
        /// </summary>
        protected ISparqlGroupBy _child = null;

        private String _assignVariable;

        /// <summary>
        /// Gets/Sets the Child GROUP BY Clause
        /// </summary>
        public ISparqlGroupBy Child
        {
            get
            {
                return this._child;
            }
            set
            {
                this._child = value;
            }
        }

        /// <summary>
        /// Applies the Grouping to the Binder
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public abstract List<BindingGroup> Apply(SparqlEvaluationContext context);

        /// <summary>
        /// Applies the Grouping to the Binder subdividing Groups from a previous Grouping
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="groups">Groups to subdivide</param>
        /// <returns></returns>
        public abstract List<BindingGroup> Apply(SparqlEvaluationContext context, List<BindingGroup> groups);

        /// <summary>
        /// Gets the Variables involved in this Group By
        /// </summary>
        public abstract IEnumerable<String> Variables
        {
            get;
        }

        /// <summary>
        /// Gets the Projectable Variables used in the GROUP BY i.e. Variables that are grouped upon and Assigned Variables
        /// </summary>
        public abstract IEnumerable<String> ProjectableVariables
        {
            get;
        }

        /// <summary>
        /// Gets the Expression used to GROUP BY
        /// </summary>
        public abstract ISparqlExpression Expression
        {
            get;
        }

        /// <summary>
        /// Gets/Sets the Variable that the grouped upon value should be assigned to
        /// </summary>
        public String AssignVariable
        {
            get
            {
                return this._assignVariable;
            }
            set
            {
                this._assignVariable = value;
            }
        }
    }

    /// <summary>
    /// Represents a Grouping on a given Variable
    /// </summary>
    public class GroupByVariable
        : BaseGroupBy
    {
        private String _name;

        /// <summary>
        /// Creates a new Group By which groups by a given Variable
        /// </summary>
        /// <param name="name">Variable Name</param>
        public GroupByVariable(String name)
        {
            this._name = name;
        }

        /// <summary>
        /// Creates a new Group By which groups by a given Variable and assigns to another variable
        /// </summary>
        /// <param name="name">Variable Name</param>
        /// <param name="assignVariable">Assign Variable</param>
        public GroupByVariable(String name, String assignVariable)
            : this(name)
        {
            this.AssignVariable = assignVariable;
        }

        /// <summary>
        /// Applies a Grouping on a given Variable to the Binder
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public override List<BindingGroup> Apply(SparqlEvaluationContext context)
        {
            Dictionary<INode, BindingGroup> groups = new Dictionary<INode, BindingGroup>();
            BindingGroup nulls = new BindingGroup();

            foreach (int id in context.Binder.BindingIDs)
            {
                INode value = context.Binder.Value(this._name, id);

                if (value != null)
                {
                    if (!groups.ContainsKey(value))
                    {
                        groups.Add(value, new BindingGroup());
                        if (this.AssignVariable != null)
                        {
                            groups[value].AddAssignment(this.AssignVariable, value);
                        }
                    }

                    groups[value].Add(id);
                }
                else
                {
                    nulls.Add(id);
                }
            }

            List<BindingGroup> outGroups = (from g in groups.Values select g).ToList();
            if (nulls.Any())
            {
                outGroups.Add(nulls);
                if (this.AssignVariable != null) nulls.AddAssignment(this.AssignVariable, null);
            }
            if (this._child == null)
            {
                return outGroups;
            }
            else
            {
                return this._child.Apply(context, outGroups);
            }
        }

        /// <summary>
        /// Applies a Grouping on a given Variable to the Binder Groups from a previous Grouping
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="groups">Binder Group to subgroup</param>
        /// <returns></returns>
        public override List<BindingGroup> Apply(SparqlEvaluationContext context, List<BindingGroup> groups)
        {
            List<BindingGroup> outgroups = new List<BindingGroup>();

            foreach (BindingGroup group in groups)
            {
                Dictionary<INode, BindingGroup> subgroups = new Dictionary<INode, BindingGroup>();
                BindingGroup nulls = new BindingGroup(group);

                foreach (int id in group.BindingIDs)
                {
                    INode value = context.Binder.Value(this._name, id);

                    if (value != null)
                    {
                        if (!subgroups.ContainsKey(value))
                        {
                            subgroups.Add(value, new BindingGroup(group));
                            if (this.AssignVariable != null)
                            {
                                subgroups[value].AddAssignment(this.AssignVariable, value);
                            }
                        }

                        subgroups[value].Add(id);
                    }
                    else
                    {
                        nulls.Add(id);
                    }
                }

                foreach (BindingGroup g in subgroups.Values)
                {
                    outgroups.Add(g);
                }
                if (nulls.Any())
                {
                    outgroups.Add(nulls);
                    if (this.AssignVariable != null) nulls.AddAssignment(this.AssignVariable, null);
                }
            }

            if (this._child == null)
            {
                return outgroups;
            }
            else
            {
                return this._child.Apply(context, outgroups);
            }
        }

        /// <summary>
        /// Gets the Variables used in the GROUP BY
        /// </summary>
        public override IEnumerable<string> Variables
        {
            get 
            {
                if (this._child == null)
                {
                    return this._name.AsEnumerable<String>();
                }
                else
                {
                    return this._child.Variables.Concat(this._name.AsEnumerable<String>());
                }
            }
        }

        /// <summary>
        /// Gets the Projectable Variables used in the GROUP BY i.e. Variables that are grouped upon and Assigned Variables
        /// </summary>
        public override IEnumerable<String> ProjectableVariables
        {
            get
            {
                List<String> vars = new List<string>();
                if (this.AssignVariable != null) vars.Add(this.AssignVariable);
                vars.Add(this._name);

                if (this._child != null) vars.AddRange(this._child.ProjectableVariables);
                return vars.Distinct();
            }
        }

        /// <summary>
        /// Gets the Variable Expression Term used by this GROUP BY
        /// </summary>
        public override ISparqlExpression Expression
        {
            get 
            {
                return new VariableTerm(this._name); 
            }
        }

        /// <summary>
        /// Gets the String representation of the GROUP BY
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            if (this.AssignVariable != null && !this.AssignVariable.Equals(this._name))
            {
                output.Append('(');
            }
            output.Append('?');
            output.Append(this._name);
            if (this.AssignVariable != null && !this.AssignVariable.Equals(this._name))
            {
                output.Append(" AS ?");
                output.Append(this.AssignVariable);
                output.Append(')');
            }

            if (this._child != null)
            {
                output.Append(' ');
                output.Append(this._child.ToString());
            }

            return output.ToString();
        }
    }

    /// <summary>
    /// Represents a Grouping on a given Expression
    /// </summary>
    public class GroupByExpression
        : BaseGroupBy
    {
        private ISparqlExpression _expr;

        /// <summary>
        /// Creates a new Group By which groups by a given Expression
        /// </summary>
        /// <param name="expr">Expression</param>
        public GroupByExpression(ISparqlExpression expr)
        {
            this._expr = expr;
        }

        /// <summary>
        /// Applies a Grouping on a given Expression to the Binder
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <returns></returns>
        public override List<BindingGroup> Apply(SparqlEvaluationContext context)
        {
            Dictionary<INode, BindingGroup> groups = new Dictionary<INode, BindingGroup>();
            BindingGroup error = new BindingGroup();
            BindingGroup nulls = new BindingGroup();

            foreach (int id in context.Binder.BindingIDs)
            {
                try
                {
                    INode value = this._expr.Evaluate(context, id);

                    if (value != null)
                    {
                        if (!groups.ContainsKey(value))
                        {
                            groups.Add(value, new BindingGroup());
                            if (this.AssignVariable != null)
                            {
                                groups[value].AddAssignment(this.AssignVariable, value);
                            }
                        }

                        groups[value].Add(id);
                    }
                    else
                    {
                        nulls.Add(id);
                    }
                }
                catch (RdfQueryException)
                {
                    error.Add(id);
                }
            }

            //Build the List of Groups
            //Null and Error Group are included if required
            List<BindingGroup> parentGroups = (from g in groups.Values select g).ToList();
            if (error.BindingIDs.Any())
            {
                parentGroups.Add(error);
                if (this.AssignVariable != null) error.AddAssignment(this.AssignVariable, null);
            }
            if (nulls.BindingIDs.Any())
            {
                parentGroups.Add(nulls);
                if (this.AssignVariable != null) nulls.AddAssignment(this.AssignVariable, null);
            }

            if (this._child != null)
            {
                return this._child.Apply(context, parentGroups);
            }
            else
            {
                return parentGroups;
            }
        }

        /// <summary>
        /// Applies a Grouping on a given Variable to the Binder Groups from a previous Grouping
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        /// <param name="groups">Binder Group to subgroup</param>
        /// <returns></returns>
        public override List<BindingGroup> Apply(SparqlEvaluationContext context, List<BindingGroup> groups)
        {
            List<BindingGroup> outgroups = new List<BindingGroup>();

            foreach (BindingGroup group in groups)
            {
                Dictionary<INode, BindingGroup> subgroups = new Dictionary<INode, BindingGroup>();
                BindingGroup error = new BindingGroup();
                BindingGroup nulls = new BindingGroup();

                foreach (int id in group.BindingIDs)
                {
                    try
                    {
                        INode value = this._expr.Evaluate(context, id);

                        if (value != null)
                        {
                            if (!subgroups.ContainsKey(value))
                            {
                                subgroups.Add(value, new BindingGroup(group));
                                if (this.AssignVariable != null)
                                {
                                    subgroups[value].AddAssignment(this.AssignVariable, value);
                                }
                            }

                            subgroups[value].Add(id);
                        }
                        else
                        {
                            nulls.Add(id);
                        }
                    }
                    catch (RdfQueryException)
                    {
                        error.Add(id);
                    }
                }

                //Build the List of Groups
                //Null and Error Group are included if required
                foreach (BindingGroup g in subgroups.Values)
                {
                    outgroups.Add(g);
                }
                if (error.BindingIDs.Any())
                {
                    outgroups.Add(error);
                    if (this.AssignVariable != null) error.AddAssignment(this.AssignVariable, null);
                    error = new BindingGroup();
                }
                if (nulls.BindingIDs.Any())
                {
                    outgroups.Add(nulls);
                    if (this.AssignVariable != null) nulls.AddAssignment(this.AssignVariable, null);
                    nulls = new BindingGroup();
                }
            }

            if (this._child == null)
            {
                return outgroups;
            }
            else
            {
                return this._child.Apply(context, outgroups);
            }
        }

        /// <summary>
        /// Gets the Fixed Variables used in the Grouping
        /// </summary>
        public override IEnumerable<string> Variables
        {
            get
            {
                if (this._child == null)
                {
                    return this._expr.Variables;
                }
                else
                {
                    return this._expr.Variables.Concat(this._child.Variables);
                }
            }
        }

        /// <summary>
        /// Gets the Projectable Variables used in the GROUP BY i.e. Variables that are grouped upon and Assigned Variables
        /// </summary>
        public override IEnumerable<String> ProjectableVariables
        {
            get
            {
                List<String> vars = new List<string>();
                if (this.AssignVariable != null) vars.Add(this.AssignVariable);
                if (this._expr is VariableTerm)
                {
                    vars.AddRange(this._expr.Variables);
                }

                if (this._child != null) vars.AddRange(this._child.ProjectableVariables);
                return vars.Distinct();
            }
        }

        /// <summary>
        /// Gets the Expression used to GROUP BY
        /// </summary>
        public override ISparqlExpression Expression
        {
            get 
            {
                return this._expr;
            }
        }

        /// <summary>
        /// Gets the String representation of the GROUP BY
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder output = new StringBuilder();
            output.Append('(');
            output.Append(this._expr.ToString());
            if (this.AssignVariable != null)
            {
                output.Append(" AS ?");
                output.Append(this.AssignVariable);
            }
            output.Append(')');

            if (this._child != null)
            {
                output.Append(' ');
                output.Append(this._child.ToString());
            }

            return output.ToString();
        }
    }
}