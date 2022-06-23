﻿// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpYaml.Events;
using SharpYaml.Tokens;
using AnchorAlias = SharpYaml.Tokens.AnchorAlias;
using DocumentEnd = SharpYaml.Tokens.DocumentEnd;
using DocumentStart = SharpYaml.Tokens.DocumentStart;
using Event = SharpYaml.Events.ParsingEvent;
using Scalar = SharpYaml.Tokens.Scalar;
using StreamEnd = SharpYaml.Tokens.StreamEnd;
using StreamStart = SharpYaml.Tokens.StreamStart;
using YamlStyle = SharpYaml.YamlStyle;

namespace SharpYaml
{
    public static class Parser
    {
        public static IParser CreateParser(TextReader reader)
        {
            if (reader is StringReader stringReader)
                return new Parser<StringLookAheadBuffer>(new StringLookAheadBuffer(stringReader.ReadToEnd()));

            else return new Parser<LookAheadBuffer>(new LookAheadBuffer(reader, Scanner<LookAheadBuffer>.MaxBufferLength));
        }
    }

    /// <summary>
    /// Parses YAML streams.
    /// </summary>
    public class Parser<TBuffer> : IParser where TBuffer : ILookAheadBuffer
    {
        private readonly Stack<ParserState> states = new Stack<ParserState>();
        private readonly TagDirectiveCollection tagDirectives = new TagDirectiveCollection();
        private ParserState state;

        private readonly Scanner<TBuffer> scanner;
        private Token currentToken;

        private Token GetCurrentToken()
        {
            if (currentToken == null)
            {
                if (scanner.InternalMoveNext())
                {
                    currentToken = scanner.Current;
                }
            }
            return currentToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IParser"/> class.
        /// </summary>
        /// <param name="buffer">The input where the YAML stream is to be read.</param>
        public Parser(TBuffer buffer)
        {
            scanner = new Scanner<TBuffer>(buffer);
        }

        /// <summary>
        /// Gets the current event.
        /// </summary>
        public Event Current { get; private set; }

        /// <summary>
        /// Moves to the next event.
        /// </summary>
        /// <returns>Returns true if there are more events available, otherwise returns false.</returns>
        public bool MoveNext()
        {
            // No events after the end of the stream or error.
            if (state == ParserState.YAML_PARSE_END_STATE)
            {
                Current = null;
                return false;
            }
            else
            {
                // Generate the next event.
                Current = StateMachine();
                return true;
            }
        }

        private Event StateMachine()
        {
            switch (state)
            {
                case ParserState.YAML_PARSE_STREAM_START_STATE:
                    return ParseStreamStart();

                case ParserState.YAML_PARSE_IMPLICIT_DOCUMENT_START_STATE:
                    return ParseDocumentStart(true);

                case ParserState.YAML_PARSE_DOCUMENT_START_STATE:
                    return ParseDocumentStart(false);

                case ParserState.YAML_PARSE_DOCUMENT_CONTENT_STATE:
                    return ParseDocumentContent();

                case ParserState.YAML_PARSE_DOCUMENT_END_STATE:
                    return ParseDocumentEnd();

                case ParserState.YAML_PARSE_BLOCK_NODE_STATE:
                    return ParseNode(true, false);

                case ParserState.YAML_PARSE_BLOCK_NODE_OR_INDENTLESS_SEQUENCE_STATE:
                    return ParseNode(true, true);

                case ParserState.YAML_PARSE_FLOW_NODE_STATE:
                    return ParseNode(false, false);

                case ParserState.YAML_PARSE_BLOCK_SEQUENCE_FIRST_ENTRY_STATE:
                    return ParseBlockSequenceEntry(true);

                case ParserState.YAML_PARSE_BLOCK_SEQUENCE_ENTRY_STATE:
                    return ParseBlockSequenceEntry(false);

                case ParserState.YAML_PARSE_INDENTLESS_SEQUENCE_ENTRY_STATE:
                    return ParseIndentlessSequenceEntry();

                case ParserState.YAML_PARSE_BLOCK_MAPPING_FIRST_KEY_STATE:
                    return ParseBlockMappingKey(true);

                case ParserState.YAML_PARSE_BLOCK_MAPPING_KEY_STATE:
                    return ParseBlockMappingKey(false);

                case ParserState.YAML_PARSE_BLOCK_MAPPING_VALUE_STATE:
                    return ParseBlockMappingValue();

                case ParserState.YAML_PARSE_FLOW_SEQUENCE_FIRST_ENTRY_STATE:
                    return ParseFlowSequenceEntry(true);

                case ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_STATE:
                    return ParseFlowSequenceEntry(false);

                case ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_KEY_STATE:
                    return ParseFlowSequenceEntryMappingKey();

                case ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_VALUE_STATE:
                    return ParseFlowSequenceEntryMappingValue();

                case ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_END_STATE:
                    return ParseFlowSequenceEntryMappingEnd();

                case ParserState.YAML_PARSE_FLOW_MAPPING_FIRST_KEY_STATE:
                    return ParseFlowMappingKey(true);

                case ParserState.YAML_PARSE_FLOW_MAPPING_KEY_STATE:
                    return ParseFlowMappingKey(false);

                case ParserState.YAML_PARSE_FLOW_MAPPING_VALUE_STATE:
                    return ParseFlowMappingValue(false);

                case ParserState.YAML_PARSE_FLOW_MAPPING_EMPTY_VALUE_STATE:
                    return ParseFlowMappingValue(true);

                default:
                    Debug.Assert(false, "Invalid state"); // Invalid state.
                    throw new InvalidOperationException();
            }
        }

        private void Skip()
        {
            if (currentToken != null)
            {
                currentToken = null;
                scanner.ConsumeCurrent();
            }
        }

        /// <summary>
        /// Parse the production:
        /// stream   ::= STREAM-START implicit_document? explicit_document* STREAM-END
        ///              ************
        /// </summary>
        private Event ParseStreamStart()
        {
            if (GetCurrentToken() is not StreamStart streamStart)
            {
                var current = GetCurrentToken();
                throw new SemanticErrorException(current.Start, current.End, "Did not find expected <stream-start>.");
            }
            Skip();

            state = ParserState.YAML_PARSE_IMPLICIT_DOCUMENT_START_STATE;
            return new Events.StreamStart(streamStart.Start, streamStart.End);
        }

        /// <summary>
        /// Parse the productions:
        /// implicit_document    ::= block_node DOCUMENT-END*
        ///                          *
        /// explicit_document    ::= DIRECTIVE* DOCUMENT-START block_node? DOCUMENT-END*
        ///                          *************************
        /// </summary>
        private Event ParseDocumentStart(bool isImplicit)
        {
            // Parse extra document end indicators.

            if (!isImplicit)
            {
                while (GetCurrentToken() is DocumentEnd)
                {
                    Skip();
                }
            }

            // Parse an isImplicit document.

            if (isImplicit && !(GetCurrentToken() is VersionDirective || GetCurrentToken() is TagDirective || GetCurrentToken() is DocumentStart || GetCurrentToken() is StreamEnd))
            {
                var directives = new TagDirectiveCollection();
                ProcessDirectives(directives);

                states.Push(ParserState.YAML_PARSE_DOCUMENT_END_STATE);

                state = ParserState.YAML_PARSE_BLOCK_NODE_STATE;

                return new Events.DocumentStart(null, directives, true, GetCurrentToken().Start, GetCurrentToken().End);
            }

            // Parse an explicit document.

            else if (GetCurrentToken() is not StreamEnd)
            {
                var start = GetCurrentToken().Start;
                var directives = new TagDirectiveCollection();
                var versionDirective = ProcessDirectives(directives);

                var current = GetCurrentToken();
                if (current is not DocumentStart)
                {
                    throw new SemanticErrorException(current.Start, current.End, "Did not find expected <document start>.");
                }

                states.Push(ParserState.YAML_PARSE_DOCUMENT_END_STATE);

                state = ParserState.YAML_PARSE_DOCUMENT_CONTENT_STATE;

                Event evt = new Events.DocumentStart(versionDirective, directives, false, start, current.End);
                Skip();
                return evt;
            }

            // Parse the stream end.

            else
            {
                state = ParserState.YAML_PARSE_END_STATE;

                Event evt = new Events.StreamEnd(GetCurrentToken().Start, GetCurrentToken().End);
                // Do not call skip here because that would throw an exception
                if (scanner.InternalMoveNext())
                {
                    throw new InvalidOperationException("The scanner should contain no more tokens.");
                }
                return evt;
            }
        }

        /// <summary>
        /// Parse directives.
        /// </summary>
        private VersionDirective ProcessDirectives(TagDirectiveCollection tags)
        {
            VersionDirective version = null;

            while (true)
            {
                if (GetCurrentToken() is VersionDirective currentVersion)
                {
                    if (version != null)
                    {
                        throw new SemanticErrorException(currentVersion.Start, currentVersion.End, "Found duplicate %YAML directive.");
                    }

                    if (currentVersion.Version.Major != Constants.MajorVersion || currentVersion.Version.Minor != Constants.MinorVersion)
                    {
                        throw new SemanticErrorException(currentVersion.Start, currentVersion.End, "Found incompatible YAML document.");
                    }

                    version = currentVersion;
                }
                else if (GetCurrentToken() is TagDirective tag)
                {
                    if (tagDirectives.Contains(tag.Handle))
                    {
                        throw new SemanticErrorException(tag.Start, tag.End, "Found duplicate %TAG directive.");
                    }
                    tagDirectives.Add(tag);
                    if (tags != null)
                    {
                        tags.Add(tag);
                    }
                }
                else
                {
                    break;
                }

                Skip();
            }

            if (tags != null)
            {
                AddDefaultTagDirectives(tags);
            }
            AddDefaultTagDirectives(tagDirectives);

            return version;
        }

        private static void AddDefaultTagDirectives(TagDirectiveCollection directives)
        {
            foreach (var directive in Constants.DefaultTagDirectives)
            {
                if (!directives.Contains(directive))
                {
                    directives.Add(directive);
                }
            }
        }

        /// <summary>
        /// Parse the productions:
        /// explicit_document    ::= DIRECTIVE* DOCUMENT-START block_node? DOCUMENT-END*
        ///                                                    ***********
        /// </summary>
        private Event ParseDocumentContent()
        {
            if (
                GetCurrentToken() is VersionDirective ||
                GetCurrentToken() is TagDirective ||
                GetCurrentToken() is DocumentStart ||
                GetCurrentToken() is DocumentEnd ||
                GetCurrentToken() is StreamEnd
                )
            {
                state = states.Pop();
                return ProcessEmptyScalar(scanner.CurrentPosition);
            }
            else
            {
                return ParseNode(true, false);
            }
        }

        /// <summary>
        /// Generate an empty scalar event.
        /// </summary>
        private static Event ProcessEmptyScalar(Mark position)
        {
            return new Events.Scalar(null, null, string.Empty, ScalarStyle.Plain, true, false, position, position);
        }

        /// <summary>
        /// Parse the productions:
        /// block_node_or_indentless_sequence    ::=
        ///                          ALIAS
        ///                          *****
        ///                          | properties (block_content | indentless_block_sequence)?
        ///                            **********  *
        ///                          | block_content | indentless_block_sequence
        ///                            *
        /// block_node           ::= ALIAS
        ///                          *****
        ///                          | properties block_content?
        ///                            ********** *
        ///                          | block_content
        ///                            *
        /// flow_node            ::= ALIAS
        ///                          *****
        ///                          | properties flow_content?
        ///                            ********** *
        ///                          | flow_content
        ///                            *
        /// properties           ::= TAG ANCHOR? | ANCHOR TAG?
        ///                          *************************
        /// block_content        ::= block_collection | flow_collection | SCALAR
        ///                                                               ******
        /// flow_content         ::= flow_collection | SCALAR
        ///                                            ******
        /// </summary>
        private Event ParseNode(bool isBlock, bool isIndentlessSequence)
        {
            if (GetCurrentToken() is AnchorAlias alias)
            {
                state = states.Pop();
                Event evt = new Events.AnchorAlias(alias.Value, alias.Start, alias.End);
                Skip();
                return evt;
            }

            var start = GetCurrentToken().Start;

            Anchor anchor = null;
            Tag tag = null;

            // The anchor and the tag can be in any order. This loop repeats at most twice.
            while (true)
            {
                if (anchor == null && (anchor = GetCurrentToken() as Anchor) != null)
                {
                    Skip();
                }
                else if (tag == null && (tag = GetCurrentToken() as Tag) != null)
                {
                    Skip();
                }
                else
                {
                    break;
                }
            }

            string tagName = null;
            if (tag != null)
            {
                if (string.IsNullOrEmpty(tag.Handle))
                {
                    tagName = tag.Suffix;
                }
                else if (tagDirectives.Contains(tag.Handle))
                {
                    tagName = string.Concat(tagDirectives[tag.Handle].Prefix, tag.Suffix);
                }
                else
                {
                    throw new SemanticErrorException(tag.Start, tag.End, "While parsing a node, find undefined tag handle.");
                }
            }
            if (string.IsNullOrEmpty(tagName))
            {
                tagName = null;
            }

            var anchorName = anchor != null ? string.IsNullOrEmpty(anchor.Value) ? null : anchor.Value : null;

            bool isImplicit = string.IsNullOrEmpty(tagName);

            if (isIndentlessSequence && GetCurrentToken() is BlockEntry)
            {
                state = ParserState.YAML_PARSE_INDENTLESS_SEQUENCE_ENTRY_STATE;

                return new Events.SequenceStart(
                    anchorName,
                    tagName,
                    isImplicit,
                    YamlStyle.Block,
                    start,
                    GetCurrentToken().End
                    );
            }
            else
            {
                if (GetCurrentToken() is Scalar scalar)
                {
                    bool isPlainImplicit = false;
                    bool isQuotedImplicit = false;
                    if ((scalar.Style == ScalarStyle.Plain && tagName == null) || tagName == Constants.DefaultHandle)
                    {
                        isPlainImplicit = true;
                    }
                    else if (tagName == null)
                    {
                        isQuotedImplicit = true;
                    }

                    state = states.Pop();
                    Event evt = new Events.Scalar(anchorName, tagName, scalar.Value, scalar.Style, isPlainImplicit, isQuotedImplicit, start, scalar.End);

                    Skip();
                    return evt;
                }

                if (GetCurrentToken() is FlowSequenceStart flowSequenceStart)
                {
                    state = ParserState.YAML_PARSE_FLOW_SEQUENCE_FIRST_ENTRY_STATE;
                    return new Events.SequenceStart(anchorName, tagName, isImplicit, YamlStyle.Flow, start, flowSequenceStart.End);
                }

                if (GetCurrentToken() is FlowMappingStart flowMappingStart)
                {
                    state = ParserState.YAML_PARSE_FLOW_MAPPING_FIRST_KEY_STATE;
                    return new Events.MappingStart(anchorName, tagName, isImplicit, YamlStyle.Flow, start, flowMappingStart.End);
                }

                if (isBlock)
                {
                    if (GetCurrentToken() is BlockSequenceStart blockSequenceStart)
                    {
                        state = ParserState.YAML_PARSE_BLOCK_SEQUENCE_FIRST_ENTRY_STATE;
                        return new Events.SequenceStart(anchorName, tagName, isImplicit, YamlStyle.Block, start, blockSequenceStart.End);
                    }

                    if (GetCurrentToken() is BlockMappingStart blockMappingStart)
                    {
                        state = ParserState.YAML_PARSE_BLOCK_MAPPING_FIRST_KEY_STATE;
                        return new Events.MappingStart(anchorName, tagName, isImplicit, YamlStyle.Block, start, GetCurrentToken().End);
                    }
                }

                if (anchorName != null || tag != null)
                {
                    state = states.Pop();
                    return new Events.Scalar(anchorName, tagName, string.Empty, ScalarStyle.Plain, isImplicit, false, start, GetCurrentToken().End);
                }

                var current = GetCurrentToken();
                throw new SemanticErrorException(current.Start, current.End, "While parsing a node, did not find expected node content.");
            }
        }

        /// <summary>
        /// Parse the productions:
        /// implicit_document    ::= block_node DOCUMENT-END*
        ///                                     *************
        /// explicit_document    ::= DIRECTIVE* DOCUMENT-START block_node? DOCUMENT-END*
        ///                                                                *************
        /// </summary>
        private Event ParseDocumentEnd()
        {
            bool isImplicit = true;
            var start = GetCurrentToken().Start;
            var end = start;

            if (GetCurrentToken() is DocumentEnd)
            {
                end = GetCurrentToken().End;
                Skip();
                isImplicit = false;
            }

            tagDirectives.Clear();

            state = ParserState.YAML_PARSE_DOCUMENT_START_STATE;
            return new Events.DocumentEnd(isImplicit, start, end);
        }

        /// <summary>
        /// Parse the productions:
        /// block_sequence ::= BLOCK-SEQUENCE-START (BLOCK-ENTRY block_node?)* BLOCK-END
        ///                    ********************  *********** *             *********
        /// </summary>
        private Event ParseBlockSequenceEntry(bool isFirst)
        {
            if (isFirst)
            {
                GetCurrentToken();
                Skip();
            }

            if (GetCurrentToken() is BlockEntry)
            {
                var mark = GetCurrentToken().End;

                Skip();
                if (!(GetCurrentToken() is BlockEntry || GetCurrentToken() is BlockEnd))
                {
                    states.Push(ParserState.YAML_PARSE_BLOCK_SEQUENCE_ENTRY_STATE);
                    return ParseNode(true, false);
                }
                else
                {
                    state = ParserState.YAML_PARSE_BLOCK_SEQUENCE_ENTRY_STATE;
                    return ProcessEmptyScalar(mark);
                }
            }

            else if (GetCurrentToken() is BlockEnd)
            {
                state = states.Pop();
                Event evt = new Events.SequenceEnd(GetCurrentToken().Start, GetCurrentToken().End);
                Skip();
                return evt;
            }

            else
            {
                var current = GetCurrentToken();
                throw new SemanticErrorException(current.Start, current.End, "While parsing a block collection, did not find expected '-' indicator.");
            }
        }

        /// <summary>
        /// Parse the productions:
        /// indentless_sequence  ::= (BLOCK-ENTRY block_node?)+
        ///                           *********** *
        /// </summary>
        private Event ParseIndentlessSequenceEntry()
        {
            if (GetCurrentToken() is BlockEntry)
            {
                var mark = GetCurrentToken().End;
                Skip();

                if (!(GetCurrentToken() is BlockEntry || GetCurrentToken() is Key || GetCurrentToken() is Value || GetCurrentToken() is BlockEnd))
                {
                    states.Push(ParserState.YAML_PARSE_INDENTLESS_SEQUENCE_ENTRY_STATE);
                    return ParseNode(true, false);
                }
                else
                {
                    state = ParserState.YAML_PARSE_INDENTLESS_SEQUENCE_ENTRY_STATE;
                    return ProcessEmptyScalar(mark);
                }
            }
            else
            {
                state = states.Pop();
                return new Events.SequenceEnd(GetCurrentToken().Start, GetCurrentToken().End);
            }
        }

        /// <summary>
        /// Parse the productions:
        /// block_mapping        ::= BLOCK-MAPPING_START
        ///                          *******************
        ///                          ((KEY block_node_or_indentless_sequence?)?
        ///                            *** *
        ///                          (VALUE block_node_or_indentless_sequence?)?)*
        ///
        ///                          BLOCK-END
        ///                          *********
        /// </summary>
        private Event ParseBlockMappingKey(bool isFirst)
        {
            if (isFirst)
            {
                GetCurrentToken();
                Skip();
            }

            if (GetCurrentToken() is Key)
            {
                var mark = GetCurrentToken().End;
                Skip();
                if (!(GetCurrentToken() is Key || GetCurrentToken() is Value || GetCurrentToken() is BlockEnd))
                {
                    states.Push(ParserState.YAML_PARSE_BLOCK_MAPPING_VALUE_STATE);
                    return ParseNode(true, true);
                }
                else
                {
                    state = ParserState.YAML_PARSE_BLOCK_MAPPING_VALUE_STATE;
                    return ProcessEmptyScalar(mark);
                }
            }

            else if (GetCurrentToken() is BlockEnd)
            {
                state = states.Pop();
                Event evt = new Events.MappingEnd(GetCurrentToken().Start, GetCurrentToken().End);
                Skip();
                return evt;
            }

            else
            {
                var current = GetCurrentToken();
                throw new SemanticErrorException(current.Start, current.End, "While parsing a block mapping, did not find expected key.");
            }
        }

        /// <summary>
        /// Parse the productions:
        /// block_mapping        ::= BLOCK-MAPPING_START
        ///
        ///                          ((KEY block_node_or_indentless_sequence?)?
        ///
        ///                          (VALUE block_node_or_indentless_sequence?)?)*
        ///                           ***** *
        ///                          BLOCK-END
        ///
        /// </summary>
        private Event ParseBlockMappingValue()
        {
            if (GetCurrentToken() is Value)
            {
                var mark = GetCurrentToken().End;
                Skip();

                if (!(GetCurrentToken() is Key || GetCurrentToken() is Value || GetCurrentToken() is BlockEnd))
                {
                    states.Push(ParserState.YAML_PARSE_BLOCK_MAPPING_KEY_STATE);
                    return ParseNode(true, true);
                }
                else
                {
                    state = ParserState.YAML_PARSE_BLOCK_MAPPING_KEY_STATE;
                    return ProcessEmptyScalar(mark);
                }
            }

            else
            {
                state = ParserState.YAML_PARSE_BLOCK_MAPPING_KEY_STATE;
                return ProcessEmptyScalar(GetCurrentToken().Start);
            }
        }

        /// <summary>
        /// Parse the productions:
        /// flow_sequence        ::= FLOW-SEQUENCE-START
        ///                          *******************
        ///                          (flow_sequence_entry FLOW-ENTRY)*
        ///                           *                   **********
        ///                          flow_sequence_entry?
        ///                          *
        ///                          FLOW-SEQUENCE-END
        ///                          *****************
        /// flow_sequence_entry  ::= flow_node | KEY flow_node? (VALUE flow_node?)?
        ///                          *
        /// </summary>
        private Event ParseFlowSequenceEntry(bool isFirst)
        {
            if (isFirst)
            {
                GetCurrentToken();
                Skip();
            }

            Event evt;
            if (GetCurrentToken() is not FlowSequenceEnd)
            {
                if (!isFirst)
                {
                    if (GetCurrentToken() is FlowEntry)
                    {
                        Skip();
                    }
                    else
                    {
                        var current = GetCurrentToken();
                        throw new SemanticErrorException(current.Start, current.End, "While parsing a flow sequence, did not find expected ',' or ']'.");
                    }
                }

                if (GetCurrentToken() is Key)
                {
                    state = ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_KEY_STATE;
                    evt = new Events.MappingStart(null, null, true, YamlStyle.Flow);
                    Skip();
                    return evt;
                }
                else if (GetCurrentToken() is not FlowSequenceEnd)
                {
                    states.Push(ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_STATE);
                    return ParseNode(false, false);
                }
            }

            state = states.Pop();
            evt = new Events.SequenceEnd(GetCurrentToken().Start, GetCurrentToken().End);
            Skip();
            return evt;
        }

        /// <summary>
        /// Parse the productions:
        /// flow_sequence_entry  ::= flow_node | KEY flow_node? (VALUE flow_node?)?
        ///                                      *** *
        /// </summary>
        private Event ParseFlowSequenceEntryMappingKey()
        {
            if (!(GetCurrentToken() is Value || GetCurrentToken() is FlowEntry || GetCurrentToken() is FlowSequenceEnd))
            {
                states.Push(ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_VALUE_STATE);
                return ParseNode(false, false);
            }
            else
            {
                var mark = GetCurrentToken().End;
                Skip();
                state = ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_VALUE_STATE;
                return ProcessEmptyScalar(mark);
            }
        }

        /// <summary>
        /// Parse the productions:
        /// flow_sequence_entry  ::= flow_node | KEY flow_node? (VALUE flow_node?)?
        ///                                                      ***** *
        /// </summary>
        private Event ParseFlowSequenceEntryMappingValue()
        {
            if (GetCurrentToken() is Value)
            {
                Skip();
                if (!(GetCurrentToken() is FlowEntry || GetCurrentToken() is FlowSequenceEnd))
                {
                    states.Push(ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_END_STATE);
                    return ParseNode(false, false);
                }
            }
            state = ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_MAPPING_END_STATE;
            return ProcessEmptyScalar(GetCurrentToken().Start);
        }

        /// <summary>
        /// Parse the productions:
        /// flow_sequence_entry  ::= flow_node | KEY flow_node? (VALUE flow_node?)?
        ///                                                                      *
        /// </summary>
        private Event ParseFlowSequenceEntryMappingEnd()
        {
            state = ParserState.YAML_PARSE_FLOW_SEQUENCE_ENTRY_STATE;
            return new Events.MappingEnd(GetCurrentToken().Start, GetCurrentToken().End);
        }

        /// <summary>
        /// Parse the productions:
        /// flow_mapping         ::= FLOW-MAPPING-START
        ///                          ******************
        ///                          (flow_mapping_entry FLOW-ENTRY)*
        ///                           *                  **********
        ///                          flow_mapping_entry?
        ///                          ******************
        ///                          FLOW-MAPPING-END
        ///                          ****************
        /// flow_mapping_entry   ::= flow_node | KEY flow_node? (VALUE flow_node?)?
        ///                          *           *** *
        /// </summary>
        private Event ParseFlowMappingKey(bool isFirst)
        {
            if (isFirst)
            {
                GetCurrentToken();
                Skip();
            }

            if (GetCurrentToken() is not FlowMappingEnd)
            {
                if (!isFirst)
                {
                    if (GetCurrentToken() is FlowEntry)
                    {
                        Skip();
                    }
                    else
                    {
                        var current = GetCurrentToken();
                        throw new SemanticErrorException(current.Start, current.End, "While parsing a flow mapping,  did not find expected ',' or '}'.");
                    }
                }

                if (GetCurrentToken() is Key)
                {
                    Skip();

                    if (!(GetCurrentToken() is Value || GetCurrentToken() is FlowEntry || GetCurrentToken() is FlowMappingEnd))
                    {
                        states.Push(ParserState.YAML_PARSE_FLOW_MAPPING_VALUE_STATE);
                        return ParseNode(false, false);
                    }
                    else
                    {
                        state = ParserState.YAML_PARSE_FLOW_MAPPING_VALUE_STATE;
                        return ProcessEmptyScalar(GetCurrentToken().Start);
                    }
                }
                else if (GetCurrentToken() is not FlowMappingEnd)
                {
                    states.Push(ParserState.YAML_PARSE_FLOW_MAPPING_EMPTY_VALUE_STATE);
                    return ParseNode(false, false);
                }
            }

            state = states.Pop();
            Event evt = new Events.MappingEnd(GetCurrentToken().Start, GetCurrentToken().End);
            Skip();
            return evt;
        }

        /// <summary>
        /// Parse the productions:
        /// flow_mapping_entry   ::= flow_node | KEY flow_node? (VALUE flow_node?)?
        ///                                   *                  ***** *
        /// </summary>
        private Event ParseFlowMappingValue(bool isEmpty)
        {
            if (isEmpty)
            {
                state = ParserState.YAML_PARSE_FLOW_MAPPING_KEY_STATE;
                return ProcessEmptyScalar(GetCurrentToken().Start);
            }

            if (GetCurrentToken() is Value)
            {
                Skip();
                if (!(GetCurrentToken() is FlowEntry || GetCurrentToken() is FlowMappingEnd))
                {
                    states.Push(ParserState.YAML_PARSE_FLOW_MAPPING_KEY_STATE);
                    return ParseNode(false, false);
                }
            }

            state = ParserState.YAML_PARSE_FLOW_MAPPING_KEY_STATE;
            return ProcessEmptyScalar(GetCurrentToken().Start);
        }
    }
}
