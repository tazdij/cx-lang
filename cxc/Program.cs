using CXCompiler.Lexing;

namespace CXCompiler {
    public class Program {
        static void Main(string[] args) {
            Console.WriteLine("C eXtended Compiler");

            // Read file specified in the first argument
            string filename = args[0];

            // Check if the file exists
            if (!System.IO.File.Exists(filename)) {
                Console.WriteLine($"File {filename} does not exist.");
                return;
            }

            string code = System.IO.File.ReadAllText(filename);


            // Create a new CX lexer instance (TEMP)
            Lexer cxlexer = new CX.CXLexer();
            Lexer clexer = new C.CLexer();

            Lexer lexer;
            if (filename.EndsWith(".c") || filename.EndsWith(".h")) {
                lexer = clexer;
            } else if (filename.EndsWith(".cx")) {
                lexer = cxlexer;
            } else {
                Console.WriteLine("Unknown file extension.");
                return;
            }

            // Process the file
            try {
                var tokens = lexer.ProcessString(filename, code);

                // print out the tokens
                foreach (var token in tokens) {
                    // Print the token type and the text/value
                    Console.WriteLine($"{token.name} : {token.value}");
                }

            } catch (Exception e) {
                // We should print the last few tokens before the error
                Console.WriteLine(e.Message);
                throw e;
                //return;
            }
    


        }
    }
}
