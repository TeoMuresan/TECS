// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.
// File name: projects/04/Fill.asm

// Runs an infinite loop that listens to the keyboard input. 
// When a key is pressed (any key), the program blackens the screen,
// i.e. writes "black" in every pixel. When no key is pressed, the
// program clears the screen, i.e. writes "white" in every pixel.

(INF_LOOP)
	// Set D to the base address of the keyboard memory map.
	@KBD
	D=M
	
	// Set color to white.
	@color
	M=0
	// If D=0 i.e. no key is being pressed, jump to fill algorithm.
	// Color was set to white, so the algorithm will clear the screen.
	@KEY_INTERACTION
	D;JEQ
	// Set color to black
	// (-1 is the decimal value of the binary number 1111111111111111, which is used to set all the pixels of a word to 1).
	@color
	M=-1
	(KEY_INTERACTION)
		// Check if the current color of the top left word of the screen coincides with the the set color.
		// If yes, jump to the beginning of the infinite loop.
		// Else, proceed with filling/clearing the screen according to the set color.
		@color
		D=M
		@SCREEN
		D=D-M
		@INF_LOOP
		D;JEQ
		
		// Start the algorithm with the top left word of the screen.
		// The algorithm proceeds row by row.
		@SCREEN
		D=A
		// addr stores the current position i.e. the word to be filled/cleared next.
		@addr
		M=D
		// The screen consists of 256 rows of 32 words each. So there are 8192 words in total.
		@8192
		D=A		
		@i
		M=D
		(LOOP)		
			@color
			D=M
			@addr
			A=M			
			M=D				
			@addr
			M=M+1				
			@i
			M=M-1
			@i
			D=M
			@LOOP
			D;JGT
		(END)
	
	@INF_LOOP
	0;JMP