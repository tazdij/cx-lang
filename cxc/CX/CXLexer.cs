using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CXCompiler.Lexing;

namespace CXCompiler.CX {
    internal class CXLexer : Lexing.Lexer {

        private CharDFA _cdfa;

        public CXLexer()
        {
            _cdfa = new CharDFA();


            CharDFA.State sta_start = _cdfa.NewState("start");
            sta_start.NewDelta("white_space", "[\\r\\n\\ \\t\x03]", sta_start, false, true);

            // Parse Add and INC
            CharDFA.State sta_add_start = _cdfa.NewState("add_start");
            CharDFA.State sta_inc = _cdfa.NewState("inc", "INC");
            CharDFA.State sta_add_end = _cdfa.NewState("add_end", "ADD");
            sta_start.NewDelta("start_to_add", "[\\+]", sta_add_start);
            sta_add_start.NewDelta("add_start_to_inc", "[\\+]", sta_inc);
            sta_add_start.NewDelta("add_start_to_end", "[^\\+]", sta_add_end, false, false);


            // Parse Subtract, can turn into a negative number if followed by any digit
            CharDFA.State sta_subtract_start = _cdfa.NewState("subtract", "SUBTRACT");
            CharDFA.State sta_subtract_end = _cdfa.NewState("subtract_end", "SUBTRACT");
            CharDFA.State sta_subtract_dec = _cdfa.NewState("dec", "DEC");
            sta_start.NewDelta("start_to_subtract", "[\\-]", sta_subtract_start);
            sta_subtract_start.NewDelta("subtract_to_end", "[^\\-0-9]", sta_subtract_end);
            sta_subtract_start.NewDelta("subtract_to_dec", "[\\-]", sta_subtract_dec);


            // Parse Divide
            CharDFA.State sta_divide = _cdfa.NewState("divide", "DIVIDE");
            sta_start.NewDelta("start_to_divide", "[\\/]", sta_divide);

            // Parse Multiply
            CharDFA.State sta_multiply = _cdfa.NewState("multiply", "MULTIPLY");
            sta_start.NewDelta("start_to_multiply", "[\\*]", sta_multiply);

            // Parse Int, can start from a dash (SUBTRACT Token)
            //  Int can transition into Float
            CharDFA.State sta_int = _cdfa.NewState("int");
            CharDFA.State sta_int_end = _cdfa.NewState("end_int", "INT_B10");

            CharDFA.State sta_int_zero = _cdfa.NewState("int_zero");
            CharDFA.State sta_int_bin = _cdfa.NewState("int_bin");
            CharDFA.State sta_int_bin_end = _cdfa.NewState("int_bin_end", "INT_B2");
            CharDFA.State sta_int_oct = _cdfa.NewState("int_oct");
            CharDFA.State sta_int_oct_end = _cdfa.NewState("int_oct_end", "INT_B8");
            CharDFA.State sta_int_hex = _cdfa.NewState("int_hex");
            CharDFA.State sta_int_hex_end = _cdfa.NewState("int_hex_end", "INT_B16");

            sta_start.NewDelta("start_to_int", "[1-9]", sta_int);
            sta_start.NewDelta("start_to_int_zero", "[0]", sta_int_zero);
            sta_subtract_start.NewDelta("subtract_to_int", "[0-9]", sta_int);   // If the subtract is followed by a number, it is a negative number

            sta_int.NewDelta("int_to_int", "[0-9]", sta_int);
            sta_int.NewDelta("int_to_end", "[^0-9\\.]", sta_int_end, false, false);

            sta_int_zero.NewDelta("int_zero_to_int_bin", "[b]", sta_int_bin);
            sta_int_zero.NewDelta("int_zero_to_int_oct", "[o]", sta_int_oct);
            sta_int_zero.NewDelta("int_zero_to_int_hex", "[x]", sta_int_hex);
            sta_int_zero.NewDelta("int_zero_to_int_end", "[^box]", sta_int_end, false, false);

            sta_int_bin.NewDelta("int_bin_to_int_bin", "[01]", sta_int_bin);
            sta_int_bin.NewDelta("int_bin_to_int_bin_end", "[^01]", sta_int_bin_end);

            sta_int_oct.NewDelta("int_oct_to_int_oct", "[0-7]", sta_int_oct);
            sta_int_oct.NewDelta("int_oct_to_int_oct_end", "[^0-7]", sta_int_oct_end);

            sta_int_hex.NewDelta("int_hex_to_int_hex", "[0-9A-F]", sta_int_hex);
            sta_int_hex.NewDelta("int_hex_to_int_hex_end", "[^0-9A-F]", sta_int_hex_end);


            // Parse Float
            // Floats are only started from an int, once a decimal point is found
            CharDFA.State sta_float = _cdfa.NewState("float");
            CharDFA.State sta_float_end = _cdfa.NewState("end_float", "FLOAT");
            sta_int.NewDelta("int_to_float", "[\\.]", sta_float);
            sta_float.NewDelta("float_to_float", "[0-9]", sta_float);
            sta_float.NewDelta("float_to_end", "[^0-9]", sta_float_end, false, false);


            // Parse symbol
            CharDFA.State sta_symbol_start = _cdfa.NewState("symbol_start");
            CharDFA.State sta_symbol_chars = _cdfa.NewState("symbol_chars");
            CharDFA.State sta_symbol_designator = _cdfa.NewState("symbol_designator");
            CharDFA.State sta_symbol_post_designator = _cdfa.NewState("symbol_post_designator");
            CharDFA.State sta_symbol_end = _cdfa.NewState("symbol_end", "SYMBOL");
            sta_start.NewDelta("start_to_symbol", "[a-zA-Z_]", sta_symbol_start);
            sta_symbol_start.NewDelta("symbol_start_to_symbol_chars", "[a-zA-Z_0-9]", sta_symbol_chars);
            sta_symbol_start.NewDelta("symbol_start_to_symbol_designator", "[:]", sta_symbol_designator);
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_designator", "[:]", sta_symbol_designator);
            sta_symbol_designator.NewDelta("symbol_chars_to_symbol_designator", "[a-zA-Z_]", sta_symbol_chars);
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_chars", "[a-zA-Z_0-9]", sta_symbol_chars);
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_end", "[^a-zA-Z_0-9:]", sta_symbol_end, false, false);
            sta_symbol_start.NewDelta("symbol_chars_to_symbol_end", "[^a-zA-Z_0-9:]", sta_symbol_end, false, false);

            // Symbol char to end (if any non symbol char is found)
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_end", "[^a-zA-Z_0-9]", sta_symbol_end, false, false);

            //
            // STRING LITERAL
            // Parse a String Literal
            CharDFA.State sta_string_start = _cdfa.NewState("string_start");
            CharDFA.State sta_string_chars = _cdfa.NewState("string_chars");
            CharDFA.State sta_string_escape = _cdfa.NewState("string_escape");
            CharDFA.State sta_string_end = _cdfa.NewState("string_end", "STRING");

            sta_start.NewDelta("start_to_string", "[\"]", sta_string_start, false, true);   // Don't collect the quote, still advance

            // to char and loop
            sta_string_start.NewDelta("string_start_to_string_chars", "[^\"\\\\]", sta_string_chars);
            sta_string_chars.NewDelta("string_chars_to_string_chars", "[^\"\\\\]", sta_string_chars);

            // to escape
            sta_string_start.NewDelta("string_start_to_string_escape", "[\\\\]", sta_string_escape);    // Escape character as first character in string
            sta_string_chars.NewDelta("string_chars_to_string_escape", "[\\\\]", sta_string_escape);    // Escape character in string

            // Escape sequence to next char (any character is valid after an escape)
            sta_string_escape.NewDelta("string_escape_to_string_chars", "[.]", sta_string_chars);

            // End of string
            sta_string_chars.NewDelta("string_chars_to_string_end", "[\"]", sta_string_end, false, true); // Don't collect the quote, still advance

            // END STRING LITERAL
            //


            // Parse Comma
            CharDFA.State sta_comma = _cdfa.NewState("comma", "COMMA");
            sta_start.NewDelta("start_to_comma", "[,]", sta_comma);

            // Parse Left Brace
            CharDFA.State sta_lbrace = _cdfa.NewState("lbrace", "LBRACE");
            sta_start.NewDelta("start_to_lbrace", "[\\{]", sta_lbrace);

            // Parse Right Brace
            CharDFA.State sta_rbrace = _cdfa.NewState("rbrace", "RBRACE");
            sta_start.NewDelta("start_to_rbrace", "[\\}]", sta_rbrace);

            // Parse Left Brace
            CharDFA.State sta_lparen = _cdfa.NewState("lparen", "LPAREN");
            sta_start.NewDelta("start_to_lparen", "[\\(]", sta_lparen);

            // Parse Right Brace
            CharDFA.State sta_rparen = _cdfa.NewState("rparen", "RPAREN");
            sta_start.NewDelta("start_to_rparen", "[\\)]", sta_rparen);

            // Parse Left Bracket
            CharDFA.State sta_lbracket = _cdfa.NewState("lbracket", "LBRACKET");
            sta_start.NewDelta("start_to_lbracket", "[\\[]", sta_lbracket);

            // Parse Right Bracket
            CharDFA.State sta_rbracket = _cdfa.NewState("rbracket", "RBRACKET");
            sta_start.NewDelta("start_to_rbracket", "[\\]]", sta_rbracket);

            // Parse end expr
            CharDFA.State sta_semicolon = _cdfa.NewState("semicolon", "SEMICOLON");
            sta_start.NewDelta("start_to_semicolon", "[;]", sta_semicolon);

            // Parse Assign & equality
            CharDFA.State sta_assign = _cdfa.NewState("assign", "ASSIGN");
            CharDFA.State sta_assign_end = _cdfa.NewState("assign_end", "ASSIGN");
            sta_start.NewDelta("start_to_assign", "[=]", sta_assign);
            sta_assign.NewDelta("start_to_assign", "[^=]", sta_assign_end, false, false);
            CharDFA.State sta_equals = _cdfa.NewState("equals", "EQ");
            sta_assign.NewDelta("start_to_assign", "[=]", sta_equals);

            // Parse Equality
            CharDFA.State sta_lt = _cdfa.NewState("lt", "LT");
            CharDFA.State sta_lt_end = _cdfa.NewState("lt_end", "LT");
            sta_start.NewDelta("start_to_lt", "[<]", sta_lt);
            CharDFA.State sta_lteq = _cdfa.NewState("lteq", "LTEQ");
            sta_lt.NewDelta("lt_to_lteq", "[=]", sta_lteq);
            sta_lt.NewDelta("start_to_lt", "[^=]", sta_lt_end, false, false);

            CharDFA.State sta_gt = _cdfa.NewState("gt", "GT");
            CharDFA.State sta_gt_end = _cdfa.NewState("gt_end", "GT");
            sta_start.NewDelta("start_to_gt", "[>]", sta_gt);
            CharDFA.State sta_gteq = _cdfa.NewState("gteq", "GTEQ");
            sta_gt.NewDelta("gt_to_gteq", "[=]", sta_gteq);
            sta_gt.NewDelta("start_to_gt", "[^=]", sta_gt_end, false, false);
        }

        public List<CharDFA.Token> ProcessFile(string filename)
        {
            string data = File.ReadAllText(Environment.CurrentDirectory + Path.DirectorySeparatorChar + filename);
            return _cdfa.ProcessFile(filename, data);
        }

        public List<CharDFA.Token> ProcessString(string filename, string data)
        {
            return _cdfa.ProcessFile(filename, data);
        }

    }
}
