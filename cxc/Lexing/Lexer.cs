using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CXCompiler.Lexing {
    internal interface Lexer {

        public List<CharDFA.Token> ProcessFile(string filename);

        public List<CharDFA.Token> ProcessString(string filename, string data);

    }
}
