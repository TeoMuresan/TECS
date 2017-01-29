// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.
// File name: projects/04/Mult.asm

// Multiplies R0 and R1 and stores the result in R2.
// (R0, R1, R2 refer to RAM[0], RAM[1], and RAM[2], respectively.)

///////
// This version sets R2 to 0 and then checks if any of the values R0 and R1 are zero.
// If yes, the execution jumps at the end of the program.
// Else, the program proceeds with the calculation.
///////

// The product is calculated in R2.
@2
M=0

// If M0=0, jump at the end.
@0
D=M
@END
D;JEQ
// If M0=1, jump at the end.
@1
D=M
@END
D;JEQ

// i is used as counter for the repeated addition.
@i
M=0
(LOOP)
	@i
	D=M
	@1
	D=D-M
	//If i=R1, jump at the end.
	@END
	D;JEQ
	//If i<R1, perform addition and increment counter.
	@0
	D=M
	@2
	M=M+D
	@i
	M=M+1
	//Execute loop again.
	@LOOP
	0;JMP
(END)	
	@END
	0;JMP