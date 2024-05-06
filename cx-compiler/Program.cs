using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CXCompiler
{
    class Program
    {
        static void Main(string[] args)
        {

            CXLexer lex = new CXLexer();



            List<CharDFA.Token> tokens = lex.ProcessString("filename Memory", @"
                
                int main() {
                    int rg:i = 0;
                    int st:binInt = 0b1100;
                    int st:octInt = 0o137;
                    int st:hexInt = 0xAFFB10;
                    for ( ; rg:i < 30; rg:i++) {
                        printf(rg:i);
                    }

                    if (rg:i == 31) {
                        return 1;
                    }
                    return 0;
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
