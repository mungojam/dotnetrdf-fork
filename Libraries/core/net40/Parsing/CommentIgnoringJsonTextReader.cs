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
using System.IO;
using Newtonsoft.Json;

namespace VDS.RDF.Parsing
{
    /// <summary>
    /// A subclass of <see cref="JsonTextReader">JsonTextReader</see> which automatically ignores all comments
    /// </summary>
    class CommentIgnoringJsonTextReader 
        : JsonTextReader
    {
        public CommentIgnoringJsonTextReader(ParsingTextReader reader)
            : base(reader) { }

        public CommentIgnoringJsonTextReader(TextReader reader) 
            : this(ParsingTextReader.Create(reader)) { }

        /// <summary>
        /// Reads the next non-comment Token if one is available
        /// </summary>
        /// <returns>True if a Token was read, False otherwise</returns>
        public override bool Read()
        {
            //Read next token
            bool result = base.Read();

            if (result)
            {
                //Keep reading next Token while Token is a Comment
                while (base.TokenType == JsonToken.Comment)
                {
                    result = base.Read();

                    //If we hit end of stream return false
                    if (!result) return false;
                }

                //If we get here we've read a Token which isn't a comment
                return true;
            }
            else
            {
                //Couldn't read to start with as already at end of stream
                return false;
            }
        }
    }
}
