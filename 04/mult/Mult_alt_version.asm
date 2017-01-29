// This file is part of www.nand2tetris.org
// and the book "The Elements of Computing Systems"
// by Nisan and Schocken, MIT Press.
// File name: projects/04/Mult.asm

// Multiplies R0 and R1 and stores the result in R2.
// (R0, R1, R2 refer to RAM[0], RAM[1], and RAM[2], respectively.)


///////
// This version is basically Mult.asm without the zeros check.
// The program covers the zero case, but it performs unnecessary operations e.g. 
//	if R0=0 and R1!=0, a number of R1-1 additions 0+0 will be performed.
///////

@2
M=0
@i
M=0
(LOOP)
	@i
	D=M
	@1
	D=D-M
	@END
	D;JEQ
	@0
	D=M
	@2
	M=M+D
	@i
	M=M+1
	@LOOP
	0;JMP
(END)	
	@END
	0;JMP