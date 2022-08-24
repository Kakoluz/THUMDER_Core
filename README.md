# THUMDER Core
***THUMDER Core*** is a **DeLuXe** (*DLX*) CPU emulator written in C# and tries to be a replacement for *WinDLX*. It does read and accept the same directives and labels.

For the Web UI Written by ***Nonondev96*** go to this repo: [THUMDER](https://github.com/nonodev96/THUMDER)

 **THUMDER Core is not ready yet.**

## Installation
Currently there are no installer or package as the project isn't ready yet. There are plans to add a *.msi* installer with every release as well as a *.deb* package

## Building
Building ***THUMDER Core*** is as simple as opening the project in Visual Studio and building it. It comes with preconfigured profiles for Windows, Linux and Mac. And versions for x64 and ARM.

***Dependencies***
``` cmd
- Visual Studio 2019 or newer
- .Net Core 6 SDK
```

## How to use
Currently the only way to use ***THUMDER Core*** is as a local command line emulator, it accepts the following sintax:
``` sh
./THUMDER_Core [File] [Options]
    -h --help         Show the help message
    -S --server       Launch as a network server
    -v --version      Show version information
```

## DLX Assembly
Here are the ***WinDLX*** assembly directives currently supported or planned to be supported.

### Assembly directives
Directives are used to control the way data or code is loaded into the emulator. The following directives are working currently:
* **.global** "*label*"
    * Make the label available for reference by code found in files loaded after this file.
* **.aling** *n*
    * Causes the next data/code loaded to be at the next higher address with the lower n bits zeroed.
* **.space** "*size*"
    * Move the current storage pointer forward size bytes (to leave some empty space in memory)
* **.byte** "*byte 1*", "*byte 2*", *...*
    * Store the bytes listed on the line sequentially in memory.
* **.word** "*word 1*", "*word 2*", *...*
    * Store the words listed on the line sequentially in memory.
* **.float** "*numer 1*", "*number 2*", *...*
    * Store the numbers listed on the line sequentially in memory as single-precision floating point numbers.
* **.double** "*numer 1*", "*number 2*", *...*
    * Store the numbers listed on the line sequentially in memory as double-precision floating point numbers.
* ~~**.ascii** "*string 1*", "*string 2*", *...*~~ - **TODO**
    * Store the strings listed on the line in the memory as a list of characters. The strings are not terminated by a 0 byte.
* ~~**.asciiz** "*string 1*", "*string 2*", *...*~~ - **TODO**
    * Similiar to *.ascii* except each string is followed by a 0 byte.

### Traps - TODO

Traps - the System Interface

Traps build the interface between DLX programs and the I/O-system. There were five traps defined in WinDLX. Zero is an invalid parameter for a trap instruction, used to terminate a program.
* **Trap 0**: Terminate a program
* **Trap 1**: Open File.
* **Trap 2**: Close File.
* **Trap 3**: Read Block from File.
* **Trap 4**: Write Block to File.
* **Trap 5**: Formatted Output to Standard-Output.

Except trap 0 none of the traps are implemented.

### Instruction

#### Arithmetic and Logic R-TYPE instructions
``` ASM
nop         NOP                          
add         Add                          
addu        Add Unsigned                 
sub         SUB                          
subu        Sub Unsigned                 
mult        MULTIPLY                     
multu       MULTIPLY Unsigned            
div         DIVIDE                       
divu        DIVIDE Unsigned              
and         AND                          
or          OR                           
xor         Exclusive OR                 
sll         SHIFT LEFT LOGICAL           
sra         SHIFT RIGHT ARITHMETIC       
srl         SHIFT RIGHT LOGICAL          
seq         Set if equal                 
sne         Set if not equal             
slt         Set if less                  
sgt         Set if greater               
sle         Set if less or equal         
sge         Set if greater or equal      
sequ        Set if equal unsigned        
sneu        Set if not equal unsigned    
sltu        Set if less unsigned         
sgtu        Set if greater unsigned      
sleu        Set if less or equal unsigned
sgeu        Set if greater or equal      
mvts        Move to special register     
mvfs        Move from special register
bswap       ??? Was not documented           NOT IMPLEMENTED  
lut         ????? same as above              NOT IMPLEMENTED  
```
#### Arithmetic and Logical Immediate I-TYPE instructions
``` ASM
addi         Add Immediate                
addui        Add Usigned Immediate        
subi         Sub Immediate                
subui        Sub Unsigned Immedated       
andi         AND Immediate                
ori          OR  Immediate                
xori         Exclusive OR  Immediate      
slli         SHIFT LEFT LOCICAL Immediate 
srai         SHIFT RIGHT ARITH. Immediate 
srli         SHIFT RIGHT LOGICAL Immediate
seqi         Set if equal                 
snei         Set if not equal             
slti         Set if less                  
sgti         Set if greater               
slei         Set if less or equal         
sgei         Set if greater or equal      
sequi        Set if equal                 
sneui        Set if not equal             
sltui        Set if less                  
sgtui        Set if greater               
sleui        Set if less or equal         
sgeui        Set if greater or equal      
```
#### Others
``` ASM
Macros for I type instructions
mov          A move macro
movu         A move macro, unsigned
```
``` ASM
Load high Immediate I-TYPE instruction
lhi          Load High Immediate
lui          Load High Immediate
sethi        Load High Immediate
```
``` ASM
LOAD/STORE BYTE 8 bits I-TYPE
lb           Load Byte               
lbu          Load Byte Unsigned      
ldstbu       Load store Byte Unsigned
sb           Store Byte              
```
``` ASM
LOAD/STORE HALFWORD 16 bits
lh           Load Halfword               
lhu          Load Halfword Unsigned      
ldsthu       Load Store Halfword Unsigned
sh           Store Halfword       
```
``` ASM
LOAD/STORE WORD 32 bits
lw           Load Word      
sw           Store Word     
ldstw        Load Store Word
```
``` ASM
Branch PC-relative, 16 bits offset
bneqz        Branch if a == 0
bnez         Branch if a != 0
beq          Branch if a == 0
bne          Branch if a != 0
```
``` ASM
Jumps Trap and RFE J-TYPE
j            Jump, PC-relative 26 bits
jal          JAL, PC-relative 26 bits 
break        break to OS              
trap         TRAP to OS               
rfe          Return From Exception    
call         Jump, PC-relative 26 bits
```
``` ASM
Jumps Trap and RFE I-TYPE
jr           Jump Register, Abs (32 bits)
jalr         JALR, Abs (32 bits)         
```
``` ASM
Macros
retr         Jump Register, Abs (32 bits)
```

## License
***THUMDER Core*** is available on Github under the [GNU GPLv3 license](https://github.com/Kakoluz/THUMDER_Core/blob/development/LICENSE.txt)

Copyright © 2022 Alberto Rodríguez Torres