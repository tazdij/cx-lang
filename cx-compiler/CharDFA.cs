using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CXCompiler
{
    class CharDFA
    {
        public struct Token
        {
            public string name;
            public string value;
            public int charNum;
            public int lineNum;
            public string fileName;
        }

        public class Delta
        {

            public Regex comparator { private set; get; }
            string name = null;
            public bool collect_char { private set; get; }
            public bool advance_char { private set; get; }
            public State to_state { private set; get; }

            public Delta(string name, string pattern, State to_state) : this(name, pattern, to_state, true, true)
            { }

            public Delta(string name, string pattern, State to_state, bool collect, bool advance)
            {
                this.name = name;
                comparator = new Regex(pattern, RegexOptions.Compiled);
                this.to_state = to_state;

                collect_char = collect;
                advance_char = advance;
            }

            public bool IsMatch(char curChar)
            {
                if (comparator.IsMatch(curChar.ToString()))
                {
                    return true;
                }
                return false;
            }

        }

        public class State
        {
            List<Delta> _deltas;
            string _name;
            public string _tok_id { private set; get; }


            public State(string name, string tok_id)
            {
                _deltas = new List<Delta>();
                _name = name;
                _tok_id = tok_id;
            }

            public Delta NewDelta(string name, string pattern, State to_state, bool collect = true, bool advance = true)
            {
                Delta delta = new Delta(name, pattern, to_state, collect, advance);
                _deltas.Add(delta);
                return delta;
            }

            public string AcceptOptions()
            {
                string accepts = "";
                foreach (Delta delta in _deltas)
                {
                    accepts += delta.comparator.ToString() + ", ";
                }

                return accepts;
            }

            public bool IsLeaf()
            {
                if (_deltas.Count == 0)
                    return true;

                return false;
            }


            public bool IsValidChar(char character)
            {
                foreach (Delta delta in _deltas)
                {
                    if (delta.IsMatch(character))
                    {
                        return true;
                    }
                }

                return false;
            }

            public char? AcceptCharacter(char character, out State next_state, out bool advance)
            {
                next_state = null;
                advance = false;
                foreach (Delta delta in _deltas)
                {
                    if (delta.IsMatch(character))
                    {
                        next_state = delta.to_state;

                        advance = delta.advance_char;

                        if (!delta.collect_char)
                            return null;

                        return character;
                    }
                }

                return null;
            }
        }

        private Dictionary<string, State> _states;
        private State _activeState = null;
        private State _startState = null;



        public CharDFA()
        {
            _states = new Dictionary<string, State>();

        }

        public State NewState(string name, string tok_id = null)
        {
            State state = new State(name, tok_id);
            _states.Add(name, state);

            // Initialize the DFA to the first created state
            if (_startState == null)
            {
                _startState = state;
                _activeState = state;
            }

            return state;
        }


        public List<Token> ProcessFile(string filename, string fileData)
        {
            // TEST: Add null or ETX (03) to end of fileData
            //  This should avoid any States not completing
            fileData += (char)3;
            // END TEST:

            string value_buffer = "";
            char character; //TODO Add Character and Line Count monitoring
            int lineNum = 1;
            int charNum = 1;

            State next_state;
            bool advance;
            char? accepted_char;

            List<Token> tokens = new List<Token>();

            Token tok;

            for (int i = 0; i < fileData.Length;)
            {
                character = fileData[i];
                accepted_char = _activeState.AcceptCharacter(fileData[i], out next_state, out advance);

                // Collect into buffer
                if (accepted_char.HasValue)
                    value_buffer += accepted_char.Value;

                // Check if the transition returned a null
                if (next_state == null)
                {
                    // This might mean there is an error, as the character had no place to be processed
                    // TODO: Throw an error here?
                    throw new Exception("Unexpected char '" + fileData[i] + "'; Expected " + _activeState.AcceptOptions());
                }
                else
                {
                    _activeState = next_state;
                }

                // If the next State is a leaf
                // we can just create the token now, and move back to the start 
                if (next_state.IsLeaf())
                {
                    // Create Token
                    tok.charNum = charNum;
                    tok.lineNum = lineNum;
                    tok.fileName = filename;
                    tok.name = next_state._tok_id;
                    tok.value = value_buffer;

                    // Add to stream
                    tokens.Add(tok);

                    // Clear buffer
                    value_buffer = "";

                    // Reset to start State
                    _activeState = _startState;
                }

                // Only advance i, if the used delta allows it
                if (advance)
                {
                    if (fileData[i] == '\n')
                    {
                        lineNum++;
                        charNum = 0;
                    }
                    charNum++;
                    i++;
                }

            }

            // Capture last token if it is terminated by EOF
            /*if (value_buffer.Length > 0)
            {
                // Create Token
                tok.charNum = charNum;
                tok.lineNum = lineNum;
                tok.fileName = filename;
                tok.name = _activeState._tok_id;
                tok.value = value_buffer;

                // Add to stream
                tokens.Add(tok);

                // Clear buffer
                value_buffer = "";
            }*/


            return tokens;
        }

    }
}
