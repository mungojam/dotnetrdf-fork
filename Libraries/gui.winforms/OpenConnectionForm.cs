/*

Copyright dotNetRDF Project 2009-12
dotnetrdf-develop@lists.sf.net

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VDS.RDF;
using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Storage;

namespace VDS.RDF.GUI.WinForms
{
    /// <summary>
    /// A Form that can be used to select an IStorageProvider instance defined in an RDF Graph using the dotNetRDF Configuration Vocabulary
    /// </summary>
    public partial class OpenConnectionForm : Form
    {
        private IGraph _g;
        private List<INode> _connectionNodes = new List<INode>();
        private IStorageProvider _connection;

        /// <summary>
        /// Creates a new Open Connection Form
        /// </summary>
        /// <param name="g">Graph contaning Connection Definitions</param>
        public OpenConnectionForm(IGraph g)
        {
            InitializeComponent();

            //Find connections defined in the Configuration Graph
            this._g = g;
            INode storageProvider = g.CreateUriNode(UriFactory.Create(ConfigurationLoader.ClassStorageProvider));
            INode rdfsLabel = g.CreateUriNode(new Uri(NamespaceMapper.RDFS + "label"));

            SparqlParameterizedString getConnections = new SparqlParameterizedString();
            getConnections.CommandText = "SELECT ?obj ?label WHERE { ?obj a @type1 . OPTIONAL { ?obj @label ?label } } ORDER BY ?label";
            getConnections.SetParameter("type", storageProvider);
            getConnections.SetParameter("label", rdfsLabel);

            Object results = this._g.ExecuteQuery(getConnections.ToString());
            if (results is SparqlResultSet)
            {
                SparqlResultSet rset = (SparqlResultSet)results;
                foreach (SparqlResult r in rset)
                {
                    this._connectionNodes.Add(r["obj"]);
                    if (r.HasValue("label"))
                    {
                        this.lstConnections.Items.Add(r["label"]);
                    }
                    else
                    {
                        this.lstConnections.Items.Add(r["obj"]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Connection created when the User clicked the Open button
        /// </summary>
        /// <remarks>
        /// May be null if the User closes/cancels the Form
        /// </remarks>
        public IStorageProvider Connection
        {
            get
            {
                return this._connection;
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (this.lstConnections.SelectedIndex != -1)
            {
                int i = this.lstConnections.SelectedIndex;
                INode objNode = this._connectionNodes[i];

                try
                {
                    Object temp = ConfigurationLoader.LoadObject(this._g, objNode);
                    if (temp is IStorageProvider)
                    {
                        this._connection = (IStorageProvider)temp;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Unable to open the selected connection as it was loaded by the Configuration Loader as an object of type '" + temp.GetType().ToString() + "' which does not implement the IStorageProvider interface", "Open Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to open the selected connection due to the following error:\n" + ex.Message, "Open Connection Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
