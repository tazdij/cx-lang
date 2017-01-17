using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gda_compiler
{
    class Program
    {
        static void Main(string[] args)
        {

            CharDFA cdfa = new CharDFA();


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
            sta_start.NewDelta("start_to_int", "[1-9]", sta_int);
            sta_start.NewDelta("start_to_zero_int", "[0]", sta_int_end);
            sta_subtract.NewDelta("subtract_to_int", "[0-9]", sta_int);
            sta_int.NewDelta("int_to_int", "[0-9]", sta_int);
            sta_int.NewDelta("int_to_end", "[^0-9\\.]", sta_int_end, false, false);

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

            // Parse Assign
            CharDFA.State sta_assign = cdfa.NewState("assign", "ASSIGN");
            sta_start.NewDelta("start_to_assign", "[=]", sta_assign);

            // Parse Equality
            CharDFA.State sta_lt = cdfa.NewState("lt", "LT");
            sta_start.NewDelta("start_to_lt", "[<]", sta_lt);

            List<CharDFA.Token> tokens = cdfa.ProcessFile("filename Memory", @"
                
                int main() {
                    int rg:i = 0;
                    for ( ; rg:i < 30; rg:i++) {
                        printf(rg:i);
                    }
                }
            ");

            foreach (CharDFA.Token token in tokens)
            {
                Console.WriteLine(token.name + ": " + token.value);
            }

            Console.WriteLine("Press any key to end.");
            Console.ReadKey();

        }
    }
}
