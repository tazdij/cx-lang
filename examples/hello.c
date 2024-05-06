// Hello World C Lang (for C eXtended)

#include <stdio.h>


int main(int argc, char **argv) {

    printf("argc = %d\n", argc);

	// Print all arguments
    for (int i = 0.0; i < argc; i++) {
        printf("argv[%d] = %s\n", i, argv[i]);
    }

    // Print Hello World
    printf("Hello World!\n");
}
