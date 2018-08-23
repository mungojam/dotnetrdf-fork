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

namespace VDS.RDF.Update.Commands
{
    /// <summary>
    /// Mode by which to clear Graphs
    /// </summary>
    public enum ClearMode
    {
        /// <summary>
        /// Clears a specific Graph of Triples
        /// </summary>
        Graph,
        /// <summary>
        /// Clears all Named Graphs of Triples
        /// </summary>
        Named,
        /// <summary>
        /// Clears the Default Graph of Triples
        /// </summary>
        Default,
        /// <summary>
        /// Clears all Graphs of Triples
        /// </summary>
        All
    }

    /// <summary>
    /// Represents the SPARQL Update CLEAR command
    /// </summary>
    public class ClearCommand : SparqlUpdateCommand
    {
        private Uri _graphUri;
        private ClearMode _mode = ClearMode.Graph;
        private bool _silent = false;

        /// <summary>
        /// Creates a Command which clears the given Graph or Graphs depending on the Clear Mode specified
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <param name="mode">Clear Mode</param>
        /// <param name="silent">Whether errors should be suppressed</param>
        public ClearCommand(Uri graphUri, ClearMode mode, bool silent)
            : base(SparqlUpdateCommandType.Clear)
        {
            this._graphUri = graphUri;
            this._mode = mode;
            if (this._graphUri == null && this._mode == ClearMode.Graph) this._mode = ClearMode.Default;
            if (this._mode == ClearMode.Default) this._graphUri = null;
            this._silent = silent;
        }

        /// <summary>
        /// Creates a Command which clears the given Graph
        /// </summary>
        /// <param name="graphUri">URI of the Graph to clear</param>
        public ClearCommand(Uri graphUri)
            : this(graphUri, ClearMode.Graph, false) { }

        /// <summary>
        /// Creates a Command which clears the Default Graph (if any)
        /// </summary>
        public ClearCommand()
            : this(null, ClearMode.Default, false) { }

        /// <summary>
        /// Creates a Command which performs the specified type of clear
        /// </summary>
        /// <param name="mode">Clear Mode</param>
        /// <param name="silent">Whether errors should be suppressed</param>
        public ClearCommand(ClearMode mode, bool silent)
            : this(null, mode, silent) { }

        /// <summary>
        /// Creates a Command which performs the specified type of clear
        /// </summary>
        /// <param name="mode">Clear Mode</param>
        public ClearCommand(ClearMode mode)
            : this(mode, false) { }

        /// <summary>
        /// Gets whether this Command affects a Single Graph
        /// </summary>
        public override bool AffectsSingleGraph
        {
            get 
            {
                return this._mode == ClearMode.Graph || this._mode == ClearMode.Default;
            }
        }

        /// <summary>
        /// Gets whether this Command affects the given Graph
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <returns></returns>
        public override bool AffectsGraph(Uri graphUri)
        {
            switch (this._mode)
            {
                case ClearMode.All:
                    return true;
                case ClearMode.Default:
                    return graphUri == null;
                case ClearMode.Named:
                    return graphUri != null;
                case ClearMode.Graph:
                    if (this._graphUri == null)
                    {
                        return true;
                    }
                    else
                    {
                        return this._graphUri.AbsoluteUri.Equals(graphUri.ToSafeString());
                    }
                default:
                    //No Other Clear Modes but have to keep the compiler happy
                    return true;
            }
        }

        /// <summary>
        /// Gets the URI of the Graph to be cleared (or null if the default graph should be cleared)
        /// </summary>
        public Uri TargetUri
        {
            get
            {
                return this._graphUri;
            }
        }

        /// <summary>
        /// Gets whether errors should be suppressed
        /// </summary>
        public bool Silent
        {
            get
            {
                return this._silent;
            }
        }

        /// <summary>
        /// Gets the Mode by which Graphs are to be cleared
        /// </summary>
        public ClearMode Mode
        {
            get
            {
                return this._mode;
            }
        }

        /// <summary>
        /// Evaluates the Command in the given Context
        /// </summary>
        /// <param name="context">Evaluation Context</param>
        public override void Evaluate(SparqlUpdateEvaluationContext context)
        {
            try
            {
                switch (this._mode)
                {
                    case ClearMode.Graph:
                    case ClearMode.Default:
                        if (context.Data.HasGraph(this._graphUri))
                        {
                            context.Data.GetModifiableGraph(this._graphUri).Clear();
                        }
                        break;
                    case ClearMode.Named:
                        foreach (Uri u in context.Data.GraphUris)
                        {
                            if (u != null)
                            {
                                context.Data.GetModifiableGraph(u).Clear();
                            }
                        }
                        break;
                    case ClearMode.All:
                        foreach (Uri u in context.Data.GraphUris)
                        {
                                context.Data.GetModifiableGraph(u).Clear();
                        }
                        break;
                }
            }
            catch
            {
                if (!this._silent) throw;
            }
        }

        /// <summary>
        /// Processes the Command using the given Update Processor
        /// </summary>
        /// <param name="processor">SPARQL Update Processor</param>
        public override void Process(ISparqlUpdateProcessor processor)
        {
            processor.ProcessClearCommand(this);
        }

        /// <summary>
        /// Gets the String representation of the Command
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            String silent = (this._silent) ? "SILENT " : String.Empty;
            switch (this._mode)
            {
                case ClearMode.All:
                    return "CLEAR " + silent + "ALL";
                case ClearMode.Default:
                    return "CLEAR " + silent + "DEFAULT";
                case ClearMode.Graph:
                    return "CLEAR " + silent + "GRAPH <" + this._graphUri.AbsoluteUri.Replace(">", "\\>") + ">";
                case ClearMode.Named:
                    return "CLEAR " + silent + "NAMED";
                default:
                    throw new NotSupportedException("Cannot convert a CLEAR Command to a string when the Command Mode is unknown");
            }
        }
    }
}
