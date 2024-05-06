using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CXCompiler
{
    class CXLexer
    {

        private CharDFA cdfa;

        public CXLexer()
        {
            this.cdfa = new CharDFA();


            CharDFA.State sta_start = cdfa.NewState("start");
            sta_start.NewDelta("white_space", "[\\r\\n\\ \x03]", sta_start, false, true);

            // Parse Add
            CharDFA.State sta_add = cdfa.NewState("add", "ADD");
            sta_start.NewDelta("start_to_add", "[\\+]", sta_add);

            // Parse Subtract, can turn into a negative number if followed by any digit
            CharDFA.State sta_subtract = cdfa.NewState("subtract", "SUBTRACT");
            CharDFA.State sta_subtract_end = cdfa.NewState("subtract_end", "SUBTRACT");
            sta_start.NewDelta("start_to_subtract", "[\\-]", sta_subtract);
            sta_subtract.NewDelta("subtract_to_end", "[^\\-0-9]", sta_subtract_end);

            // Parse Divide
            CharDFA.State sta_divide = cdfa.NewState("divide", "DIVIDE");
            sta_start.NewDelta("start_to_divide", "[\\/]", sta_divide);

            // Parse Multiply
            CharDFA.State sta_multiply = cdfa.NewState("multiply", "MULTIPLY");
            sta_start.NewDelta("start_to_multiply", "[\\*]", sta_multiply);

            // Parse Int, can start from a dash (SUBTRACT Token)
            //  Int can transition into Float
            CharDFA.State sta_int = cdfa.NewState("int");
            CharDFA.State sta_int_end = cdfa.NewState("end_int", "INT_B10");

            CharDFA.State sta_int_zero = cdfa.NewState("int_zero");
            CharDFA.State sta_int_bin = cdfa.NewState("int_bin");
            CharDFA.State sta_int_bin_end = cdfa.NewState("int_bin_end", "INT_B2");
            CharDFA.State sta_int_oct = cdfa.NewState("int_oct");
            CharDFA.State sta_int_oct_end = cdfa.NewState("int_oct_end", "INT_B8");
            CharDFA.State sta_int_hex = cdfa.NewState("int_hex");
            CharDFA.State sta_int_hex_end = cdfa.NewState("int_hex_end", "INT_B16");

            sta_start.NewDelta("start_to_int", "[1-9]", sta_int);
            sta_start.NewDelta("start_to_int_zero", "[0]", sta_int_zero);
            sta_subtract.NewDelta("subtract_to_int", "[0-9]", sta_int);

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
            CharDFA.State sta_float = cdfa.NewState("float");
            CharDFA.State sta_float_end = cdfa.NewState("end_float", "FLOAT");
            sta_int.NewDelta("int_to_float", "[\\.]", sta_float);
            sta_float.NewDelta("float_to_float", "[0-9]", sta_float);
            sta_float.NewDelta("float_to_end", "[^0-9]", sta_float_end, false, false);


            // Parse symbol
            CharDFA.State sta_symbol_start = cdfa.NewState("symbol_start");
            CharDFA.State sta_symbol_chars = cdfa.NewState("symbol_chars");
            CharDFA.State sta_symbol_designator = cdfa.NewState("symbol_designator");
            CharDFA.State sta_symbol_post_designator = cdfa.NewState("symbol_post_designator");
            CharDFA.State sta_symbol_end = cdfa.NewState("symbol_end", "SYMBOL");
            sta_start.NewDelta("start_to_symbol", "[a-zA-Z_]", sta_symbol_start);
            sta_symbol_start.NewDelta("symbol_start_to_symbol_chars", "[a-zA-Z_0-9]", sta_symbol_chars);
            sta_symbol_start.NewDelta("symbol_start_to_symbol_designator", "[:]", sta_symbol_designator);
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_designator", "[:]", sta_symbol_designator);
            sta_symbol_designator.NewDelta("symbol_chars_to_symbol_designator", "[a-zA-Z_]", sta_symbol_chars);
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_chars", "[a-zA-Z_0-9]", sta_symbol_chars);
            sta_symbol_chars.NewDelta("symbol_chars_to_symbol_end", "[^a-zA-Z_0-9:]", sta_symbol_end, false, false);
            sta_symbol_start.NewDelta("symbol_chars_to_symbol_end", "[^a-zA-Z_0-9:]", sta_symbol_end, false, false);

            // Parse Left Brace
            CharDFA.State sta_lbrace = cdfa.NewState("lbrace", "LBRACE");
            sta_start.NewDelta("start_to_lbrace", "[\\{]", sta_lbrace);

            // Parse Right Brace
            CharDFA.State sta_rbrace = cdfa.NewState("rbrace", "RBRACE");
            sta_start.NewDelta("start_to_rbrace", "[\\}]", sta_rbrace);

            // Parse Left Brace
            CharDFA.State sta_lparen = cdfa.NewState("lparen", "LPAREN");
            sta_start.NewDelta("start_to_lparen", "[\\(]", sta_lparen);

            // Parse Right Brace
            CharDFA.State sta_rparen = cdfa.NewState("rparen", "RPAREN");
            sta_start.NewDelta("start_to_rparen", "[\\)]", sta_rparen);

            // Parse end expr
            CharDFA.State sta_semicolon = cdfa.NewState("semicolon", "SEMICOLON");
            sta_start.NewDelta("start_to_semicolon", "[;]", sta_semicolon);

            // Parse Assign & equality
            CharDFA.State sta_assign = cdfa.NewState("assign", "ASSIGN");
            CharDFA.State sta_assign_end = cdfa.NewState("assign_end", "ASSIGN");
            sta_start.NewDelta("start_to_assign", "[=]", sta_assign);
            sta_assign.NewDelta("start_to_assign", "[^=]", sta_assign_end, false, false);
            CharDFA.State sta_equals = cdfa.NewState("equals", "EQ");
            sta_assign.NewDelta("start_to_assign", "[=]", sta_equals);

            // Parse Equality
            CharDFA.State sta_lt = cdfa.NewState("lt", "LT");
            CharDFA.State sta_lt_end = cdfa.NewState("lt_end", "LT");
            sta_start.NewDelta("start_to_lt", "[<]", sta_lt);
            CharDFA.State sta_lteq = cdfa.NewState("lteq", "LTEQ");
            sta_lt.NewDelta("lt_to_lteq", "[=]", sta_lteq);
            sta_lt.NewDelta("start_to_lt", "[^=]", sta_lt_end, false, false);

            CharDFA.State sta_gt = cdfa.NewState("gt", "GT");
            CharDFA.State sta_gt_end = cdfa.NewState("gt_end", "GT");
            sta_start.NewDelta("start_to_gt", "[>]", sta_gt);
            CharDFA.State sta_gteq = cdfa.NewState("gteq", "GTEQ");
            sta_gt.NewDelta("gt_to_gteq", "[=]", sta_gteq);
            sta_gt.NewDelta("start_to_gt", "[^=]", sta_gt_end, false, false);
        }

        public List<CharDFA.Token> ProcessFile(string filename)
        {
            string data = File.ReadAllText(Environment.CurrentDirectory + Path.DirectorySeparatorChar + filename);
            return cdfa.ProcessFile(filename, data);
        }

        public List<CharDFA.Token> ProcessString(string filename, string data)
        {
            return cdfa.ProcessFile(filename, data);
        }

    }
}
