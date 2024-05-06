using CXCompiler.Lexing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CXCompiler.C
{
    internal class CLexer : Lexing.Lexer {

        private CharDFA _cdfa;

        public CLexer() {
            _cdfa = new CharDFA();

            // Munch white space when in start state
            CharDFA.State sta_start = _cdfa.NewState("start");
            sta_start.NewDelta("white_space", "[\\ \\t\x03]", sta_start, false, true);

            // Newline
            CharDFA.State sta_newline = _cdfa.NewState("newline");
            CharDFA.State sta_newline_end = _cdfa.NewState("newline_end", "NEWLINE");
            sta_start.NewDelta("newline", "[\\r\\n]", sta_newline, false);
            sta_newline.NewDelta("newline_multi_byte", "[\\r\\n]", sta_newline, false, true);
            sta_newline.NewDelta("newline_end", "[^\\r\\n]", sta_newline_end, false, false);

            // Macro tokens?
            // in C the start of a MACRO is #, and inside a macro you get the source text in a parameter
            // via the #arg syntax, also there is a ## operator that concatenates two tokens
            CharDFA.State sta_macro_start = _cdfa.NewState("macro_start");
            CharDFA.State sta_macro = _cdfa.NewState("macro");
            CharDFA.State sta_macro_concat = _cdfa.NewState("macro_concat", "MACRO_CONCAT");
            CharDFA.State sta_macro_end = _cdfa.NewState("macro_end", "MACRO_SYMBOL");
            sta_start.NewDelta("macro_start", "[\\#]", sta_macro_start, false);
            sta_macro_start.NewDelta("macro", "[a-zA-Z0-9_]", sta_macro);
            sta_macro.NewDelta("macro", "[a-zA-Z0-9_]", sta_macro);
            sta_macro.NewDelta("macro_end", "[^a-zA-Z0-9_]", sta_macro_end, false, false);

            // Macro Concat
            sta_macro_start.NewDelta("macro_concat", "[\\#]", sta_macro_concat);    // No end needed, leaf state

            // In the Lexing we are going to convert each symbol into a token, even in a <stdio.h> we will have a token for each symbol
            // The parsing of the validity will be done in a subsequent parsing stage
            // 

            // Less Than <
            CharDFA.State sta_lt = _cdfa.NewState("lt", "LT");
            sta_start.NewDelta("lt", "[\\<]", sta_lt, false);

            // Greater Than >
            CharDFA.State sta_gt = _cdfa.NewState("gt", "GT");
            sta_start.NewDelta("gt", "[\\>]", sta_gt, false);

            // LEFT_PAREN (
            CharDFA.State sta_left_paren = _cdfa.NewState("left_paren", "LEFT_PAREN");
            sta_start.NewDelta("left_paren", "[\\(]", sta_left_paren, false);

            // RIGHT_PAREN )
            CharDFA.State sta_right_paren = _cdfa.NewState("right_paren", "RIGHT_PAREN");
            sta_start.NewDelta("right_paren", "[\\)]", sta_right_paren, false);

            // LEFT_BRACE {
            CharDFA.State sta_left_brace = _cdfa.NewState("left_brace", "LEFT_BRACE");
            sta_start.NewDelta("left_brace", "[\\{]", sta_left_brace, false);

            // RIGHT_BRACE }
            CharDFA.State sta_right_brace = _cdfa.NewState("right_brace", "RIGHT_BRACE");
            sta_start.NewDelta("right_brace", "[\\}]", sta_right_brace, false);

            // LEFT_BRACKET [
            CharDFA.State sta_left_bracket = _cdfa.NewState("left_bracket", "LEFT_BRACKET");
            sta_start.NewDelta("left_bracket", "[\\[]", sta_left_bracket, false);

            // RIGHT_BRACKET ]
            CharDFA.State sta_right_bracket = _cdfa.NewState("right_bracket", "RIGHT_BRACKET");
            sta_start.NewDelta("right_bracket", "[\\]]", sta_right_bracket, false);

            // Comma ,
            CharDFA.State sta_comma = _cdfa.NewState("comma", "COMMA");
            sta_start.NewDelta("comma", "[,]", sta_comma, false);

            // Semicolon ;
            CharDFA.State sta_semicolon = _cdfa.NewState("semicolon", "SEMICOLON");
            sta_start.NewDelta("semicolon", "[;]", sta_semicolon, false);

            // is there a colon in C?  I don't recall ever using one.
            // Colon :

            // Star *
            CharDFA.State sta_star = _cdfa.NewState("star", "STAR");
            sta_start.NewDelta("star", "[\\*]", sta_star, false);

            // Symbol (a symbol is any word made of letters, numbers, and underscores, that doesn't start with a number)
            //CharDFA.State sta_symbol_start = _cdfa.NewState("symbol_start");
            CharDFA.State sta_symbol = _cdfa.NewState("symbol");
            CharDFA.State sta_symbol_end = _cdfa.NewState("symbol_end", "SYMBOL");

            sta_start.NewDelta("symbol_start", "[a-zA-Z_]", sta_symbol);
            sta_symbol.NewDelta("symbol", "[a-zA-Z0-9_]", sta_symbol);
            sta_symbol.NewDelta("symbol_end", "[^a-zA-Z0-9_]", sta_symbol_end, false, false);

            // Dot .
            CharDFA.State sta_dot_start = _cdfa.NewState("dot_start");
            CharDFA.State sta_dot = _cdfa.NewState("dot", "DOT");
            sta_start.NewDelta("dot_start", "[\\.]", sta_dot_start, false);
            sta_dot_start.NewDelta("dot", "[^0-9]", sta_dot, false, false);

            // Plus +
            CharDFA.State sta_plus_start = _cdfa.NewState("plus_start");
            CharDFA.State sta_plus = _cdfa.NewState("plus", "PLUS");
            CharDFA.State sta_plus_equal = _cdfa.NewState("plus_equal", "PLUS_EQUAL");
            sta_start.NewDelta("plus", "[\\+]", sta_plus_start, false);
            sta_plus_start.NewDelta("plus", "[^=]", sta_plus, false, false);
            sta_plus_start.NewDelta("plus_equal", "[=]", sta_plus_equal);

            // Minus - AND Minus Equal -=
            CharDFA.State sta_minus_start = _cdfa.NewState("minus_start");
            CharDFA.State sta_minus = _cdfa.NewState("minus", "MINUS");
            CharDFA.State sta_minus_equal = _cdfa.NewState("minus_equal", "MINUS_EQUAL");
            sta_start.NewDelta("minus_start", "[\\-]", sta_minus_start, false);
            sta_minus_start.NewDelta("minus", "[^=]", sta_minus, false, false);
            sta_minus_start.NewDelta("minus_equal", "[=]", sta_minus_equal);

            //  Equal = AND Equal Equal ==
            CharDFA.State sta_equal_start = _cdfa.NewState("equal_start");
            CharDFA.State sta_equal = _cdfa.NewState("equal", "EQUAL");
            CharDFA.State sta_equal_equal = _cdfa.NewState("equal_equal", "EQUAL_EQUAL");
            sta_start.NewDelta("equal_start", "[\\=]", sta_equal_start, false);
            sta_equal_start.NewDelta("equal", "[^=]", sta_equal, false, false);
            sta_equal_start.NewDelta("equal_equal", "[=]", sta_equal_equal, false);

            // Number literals
            //CharDFA.State sta_int_start = _cdfa.NewState("int");
            CharDFA.State sta_int_zero_start = _cdfa.NewState("int_zero_start");
            CharDFA.State sta_int_digits = _cdfa.NewState("int_digits");
            CharDFA.State sta_int_end = _cdfa.NewState("int_end", "INT_LITERAL");

            // Binary Number 0b#### or 0B#### (>=C17)
            CharDFA.State sta_binary_start = _cdfa.NewState("binary_start");
            CharDFA.State sta_binary_digits = _cdfa.NewState("binary_digits");
            CharDFA.State sta_binary_end = _cdfa.NewState("binary_end", "BINARY_LITERAL");

            // Hex Number 0x#### or 0X####
            CharDFA.State sta_hex_start = _cdfa.NewState("hex_start");
            CharDFA.State sta_hex_digits = _cdfa.NewState("hex_digits");
            CharDFA.State sta_hex_end = _cdfa.NewState("hex_end", "HEX_LITERAL");

            // Octal Number 0#### or 0####
            CharDFA.State sta_octal_start = _cdfa.NewState("octal_start");
            CharDFA.State sta_octal_digits = _cdfa.NewState("octal_digits");
            CharDFA.State sta_octal_end = _cdfa.NewState("octal_end", "OCTAL_LITERAL");

            CharDFA.State sta_decimal_from_int = _cdfa.NewState("decimal_from_int");
            CharDFA.State sta_decimal_digits = _cdfa.NewState("decimal_digits");
            CharDFA.State sta_decimal_end = _cdfa.NewState("decimal_end", "DECIMAL_LITERAL");

            CharDFA.State sta_decimal_float = _cdfa.NewState("decimal_float", "FLOAT_LITERAL"); // A Decimal number suffixed with f or F

            // From a Zero, we can have a Hex, Octal, or Decimal numbers
            sta_start.NewDelta("int_start", "[1-9]", sta_int_digits);
            sta_start.NewDelta("int_zero_start", "[0]", sta_int_zero_start);
            sta_int_digits.NewDelta("int_digits", "[0-9]", sta_int_digits);

            sta_int_digits.NewDelta("int_digit_decimal", "[\\.]", sta_decimal_from_int);  // int to decimal
            sta_int_digits.NewDelta("int_digit_float", "[fF]", sta_decimal_float);  // int to float
            sta_int_digits.NewDelta("int_digits_end", "[^0-9\\.fF]", sta_int_end, false, false);

            // Binary Number 0b#### or 0B####
            sta_int_zero_start.NewDelta("binary_start", "[bB]", sta_binary_start, false);
            sta_binary_start.NewDelta("binary_digits", "[01]", sta_binary_digits);
            sta_binary_digits.NewDelta("binary_digits", "[01]", sta_binary_digits);
            sta_binary_digits.NewDelta("binary_end", "[^01]", sta_binary_end, false, false);

            // Hex Number 0x#### or 0X####
            sta_int_zero_start.NewDelta("hex_start", "[xX]", sta_hex_start, false);
            sta_hex_start.NewDelta("hex_digits", "[0-9a-fA-F]", sta_hex_digits);
            sta_hex_digits.NewDelta("hex_digits", "[0-9a-fA-F]", sta_hex_digits);
            sta_hex_digits.NewDelta("hex_end", "[^0-9a-fA-F]", sta_hex_end, false, false);

            // Octal Number 0#### or 0####
            sta_int_zero_start.NewDelta("octal_start", "[0-7]", sta_octal_digits);
            sta_octal_digits.NewDelta("octal_digits", "[0-7]", sta_octal_digits);
            sta_octal_digits.NewDelta("octal_end", "[^0-7]", sta_octal_end, false, false);

            // Decimal and Floating Point Number like 0?.####f? (Is this supported in C???)
            sta_dot_start.NewDelta("decimal_from_dot", "[0-9]", sta_decimal_digits);    // . to numbers
            sta_decimal_from_int.NewDelta("decimal_digits", "[0-9]", sta_decimal_digits); // int to decimal (with .####)
            sta_decimal_from_int.NewDelta("decimal_dot_suffixed", "[\\.]", sta_decimal_end); // int decimal suffixed with a dot (###.)
            sta_decimal_digits.NewDelta("decimal_digits", "[0-9]", sta_decimal_digits);
            sta_decimal_digits.NewDelta("decimal_to_float", "[fF]", sta_decimal_float);
            sta_decimal_digits.NewDelta("decimal_end", "[^0-9fF]", sta_decimal_end, false, false); // If the next char is not a valid number digit, terminate the decimal and reparse the char

            // A 0 by itself is a valid int literal, we need to allow the zero_start to end if any non valid char is reached, and then reparse it
            sta_int_zero_start.NewDelta("int_zero_end", "[^0-9fFbBxX\\.]", sta_int_end, false, false);


            // Comments /* */ and // and divide /
            CharDFA.State sta_divide_start = _cdfa.NewState("divide");
            CharDFA.State sta_divide_end = _cdfa.NewState("divide_end", "DIVIDE");

            CharDFA.State sta_comment_start = _cdfa.NewState("comment_start");
            CharDFA.State sta_comment = _cdfa.NewState("comment");
            CharDFA.State sta_comment_end = _cdfa.NewState("comment_end", "COMMENT");

            CharDFA.State sta_comment_multiline_start = _cdfa.NewState("comment_multiline_start");
            CharDFA.State sta_comment_multiline = _cdfa.NewState("comment_multiline");
            CharDFA.State sta_comment_multiline_end = _cdfa.NewState("comment_multiline_end", "COMMENT");

            // Divide symbol /      - we don't need to collect the token text, just the fact that it is a divide symbol
            sta_start.NewDelta("divide", "[\\/]", sta_divide_start, false);
            sta_divide_start.NewDelta("divide_end", "[^\\/\\*]", sta_divide_end, false, false); // If anything besides a / or * the previous was a divide, dont collect into divide token and reparse char

            // Single line comment //   - we don't need to collect the token text of the // , just the fact that it is a comment, and then all subsequent text until the end of the line
            sta_divide_start.NewDelta("comment_start", "[\\/]", sta_comment, false);
            //sta_comment_start.NewDelta("comment", "[^\\r\\n]", sta_comment);
            sta_comment.NewDelta("comment", "[^\\r\\n]", sta_comment);
            sta_comment.NewDelta("comment_end", "[\\r\\n]", sta_comment_end, false);    // Don't collect the line-endings

            // Multi-line comment /* */  - we don't need to collect the token text of the /* */, just the fact that it is a comment, and then all subsequent text until the end of the comment
            sta_divide_start.NewDelta("comment_multiline_start", "[\\*]", sta_comment_multiline_start, false);
            sta_comment_multiline_start.NewDelta("comment_multiline", "[^\\*]", sta_comment_multiline);
            sta_comment_multiline.NewDelta("comment_multiline", "[^\\*]", sta_comment_multiline);
            sta_comment_multiline.NewDelta("comment_multiline_end", "[\\*]", sta_comment_multiline_end, false);


            // String literals
            CharDFA.State sta_string = _cdfa.NewState("string");
            CharDFA.State sta_string_escaped = _cdfa.NewState("string_escaped");
            CharDFA.State sta_string_end = _cdfa.NewState("string_end", "STRING");

            sta_start.NewDelta("string_start", "[\"]", sta_string, false);
            // Start to any other character (non escaping)
            sta_string.NewDelta("string", "[^\\\\\"]", sta_string);
            // Escaping & Ending (from string state)
            sta_string.NewDelta("string_escaped", "[\\\\]", sta_string_escaped);
            sta_string.NewDelta("string_end", "[\\\"]", sta_string_end, false);
            // Escaped Character
            sta_string_escaped.NewDelta("string_escaped_character", ".", sta_string);



        }

        public List<CharDFA.Token> ProcessFile(string filename) {
            string data = File.ReadAllText(Environment.CurrentDirectory + Path.DirectorySeparatorChar + filename);
            return _cdfa.ProcessFile(filename, data);
        }

        public List<CharDFA.Token> ProcessString(string filename, string data) {
            return _cdfa.ProcessFile(filename, data);
        }


    }
}
