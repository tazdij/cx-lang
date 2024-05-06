using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CXCompiler.Lexing
{
    internal class CharDFA
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
            string name;
            public bool collect_char { private set; get; }
            public bool advance_char { private set; get; }
            public State to_state { private set; get; }

            public Delta(string name, string pattern, State to_state) : this(name, pattern, to_state, true, true) { }

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

            public string Name { get { return _name; } }

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
            // To make it much easier to display errors formatted, we will simply
            //  - replace all tabs with spaces
            //  - replace all \r\n with \n
            //  - replace all \r with \n
            fileData = fileData.Replace("\t", "    ");    // Maybe lets use the current buffer to check for display positioning and tabs?
            fileData = fileData.Replace("\r\n", "\n");
            fileData = fileData.Replace("\r", "\n");


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

                    // We need to get the entire line to display in the exception
                    // Split the fileData into lines
                    string[] lines = fileData.Split('\n');
                    string line = lines[lineNum - 1];
                    string line_before = lines[lineNum - 2];
                    string line_after = lines[lineNum] ?? "";

                    // Get the length of the last line number
                    int lineNumLength = (lineNum + 1).ToString().Length;


                    // Create Padded Line Number strings for each of the three lines
                    string line_before_num_str = (lineNum - 2).ToString().PadLeft(lineNumLength, '0');
                    string line_num_str = (lineNum - 1).ToString().PadLeft(lineNumLength, '0');
                    string line_after_num_str = lineNum.ToString().PadLeft(lineNumLength, '0');


                    // Get the line up to the character
                    string line_to_char = line.Substring(0, charNum);

                    // Get the line after the character
                    string line_after_char = line.Substring(charNum);

                    // Generate a ^ at the character position left padded by spaces
                    //  pad = Char Position + LineNumber length + 1 (for the :)
                    string pointer = new string(' ', charNum - 1) + "^";

                    // Generate the error message
                    string error_message = $"Unexpected char '{fileData[i]}' at Position: {lineNum}, {charNum}; Expected " + _activeState.AcceptOptions();

                    // Add the line to the error message
                    error_message +=
                          $"\n\nSyntax error in source at {lineNum}, {charNum}: \n\n"
                        + $"{line_before_num_str}:\t{line_before}\n{line_num_str}:\t{line}\n"
                        + $"\t{pointer}\n"
                        + $"{line_after_num_str}:\t{line_after}\n\n";

                    // Print the DFA State, to know where the lexer is in.
                    error_message += $"Active State: {_activeState.Name}\n";


                    //throw new Exception($"Unexpected char '{fileData[i]}' at Line {lineNum} Char {charNum}; Expected " + _activeState.AcceptOptions());
                    throw new Exception(error_message);
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
