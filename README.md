# codeless

### What is it?
A tiny weird scripting language for Windows. (Includes a debugger (!))

### How to use it?
##### How to 'compile' a file?
```
codeless <filename>
```
This will produce a file `<filename>!`.

##### How to execute an 'executable'?
```
codeless <filename>!
```

##### How to debug an 'executable'?
```
codeless ?<filename>!
```

### Instructions
Instructions end with a new line (or carriage return) or `;`.<br>
Stored variable values are persistent, the stack is usually empty or contains arguments passed to the application via the command line.

|Instruction|Explanation|
|--|--|
|`FLUSH`|Removes all values from the stack.|
|`CR`|Prints a newline char to stdout.|
|`EXIT`|Exits the program|
|`CLR <VariableName>`|Deletes the variable `<VariableName>`|
|`PUTS <VariableName>`|Prints the contents of the variable `<VariableName>` to stdout.|
|`PUSH <VariableName>`|Pushes the value of the variable `<VariableName>` to the back of the stack.|
|`PUSHF <VariableName>`|Pushes the value of the variable `<VariableName>` to the front of the stack.|
|`NOT <VariableName>`|Negates the value of the variable `<VariableName>` and pushes the resulting value to the back of the stack.<br>`TRUE` => `FALSE`<br>everything else => `TRUE`|
|`POP <VariableName>`|Retrieves the value from the back of the stack, removes it from the stack and stores the contents in the variable `<VariableName>`.<br>If the stack is empty stores an empty string instead.|
|`POPF <VariableName>`|Retrieves the value from the front of the stack, removes it from the stack and stores the contents in the variable `<VariableName>`.<br>If the stack is empty stores an empty string instead.|
|`CALL <FileName>`|Executes a script called `<FileName>!`. Base directory is always the local directory of the currently executed script.<br>The called script operates on a copy of the stack and cannot corrupt the stack of the current executable unless that executable is called either directly or indirectly.<br><br>**Calling Convention:**<br>- The Stack is pushed to the arguments.<br>- The Return Value is the last value on the stack and is returned via the Clipboard.|
|`SETSP <VariableName>`|Sets the whitespace character ` ` to a variable called `<VariableName>`.|
|`JMP <InstructionIndex>`|Sets the instruction pointer to the 1 based instruction index specified in `<InstructionIndex>`.<br><br>See `#LABEL`.|
|`DJM <VariableName>`|Sets the instruction pointer to the 1 based instruction index stored in the variable `<VariableName>`.|
|`SETIP <VariableName>`|Retrieves the current instruction pointer and stores it in the variable `<VariableName>`.|
|`SAT <VariableName> <VariableName2>`|Retrieves the character at the index stored in the variable `<VariableName2>` of the string stored in the variable `<VariableName>`.<br>If the index is outside the bounds of the string retrieves an empty string.|
|`SET <VariableName> <Value>`|Sets the contents of the variable `<VariableName>` to the value `<Value>`.|
|`MOV <VariableName> <VariableName2>`|Stores the contents of the variable `<VariableName2>` in the variable `<VariableName>`.|
|`IADD <VariableName> <VariableName2>`|Performs an integer addition on `<VariableName>` + `<VariableName2>` and pushes the result to the back of the stack.|
|`ISUB <VariableName> <VariableName2>`|Performs an integer subtraction on `<VariableName>` - `<VariableName2>` and pushes the result to the back of the stack.|
|`IMUL <VariableName> <VariableName2>`|Performs an integer multiplication on `<VariableName>` * `<VariableName2>` and pushes the result to the back of the stack.|
|`IDIV <VariableName> <VariableName2>`|Performs an integer division on `<VariableName>` / `<VariableName2>` and pushes the result to the back of the stack.|
|`SCAT <VariableName> <VariableName2>`|Appends the string stored in the variable `<VariableName2>` to the string stored in variable `<VariableName2>` and pushes the result to the back of the stack.|
|`EQ <VariableName> <VariableName2>`|Compares the contents of the variables `<VariableName>` and `<VariableName2>` for equality and pushes the result (`TRUE`/`FALSE`) to the back of the stack.|
|`NE <VariableName> <VariableName2>`|Compares the contents of the variables `<VariableName>` and `<VariableName2>` for inequality and pushes the result (`TRUE`/`FALSE`) to the back of the stack.|
|`IGCMP <VariableName> <VariableName2>`|Performs an integer comparison of the contents of the variable <VariableName> being greater than the contents of the variable `<VariableName2>` and pushes the result (`TRUE`/`FALSE`) to the back of the stack.|
|`IFCALL <VariableName> <FileName>`|If the content of the variable `<VariableName>` is `TRUE` executes the script called `<FileName>`.<br><br>See: `CALL`.|
|`IFJMP <VariableName> <InstructionIndex>`|If the content of the variable `<VariableName>` is `TRUE` sets the instruction pointer to the 1 based instruction index specified in `<InstructionIndex>`.<br><br>See `#LABEL`.|
|`IFDJMP <VariableName> <VariableName2>`|If the content of the variable `<VariableName>` is `TRUE` sets the instruction pointer to the 1 based instruction index stored in the variable `<VariableName2>`.|
|`SETS <VariableName> (...)`|Sets the string _value_ specified in the variable arguments (that will be separated by a whitespace ` ` character).<br><br>`SETS empty` will set the variable `empty` to an empty string.<br>`SETS str a    b   c` will set the variable `str` to `a b c`.|


### Preprocessor Statements
Preprocessor Statements end with a new line (or carriage return) or `;`.

|Instruction|Explanation|
|--|--|
|`## <Comment>`|A Comment.<br>`## This is a comment.`|
|`#LABEL <LabelName>`|Defines a label. The label can be referenced with `$<LabelName>`.<br>`## Loop forever:`<br>`#LABEL loop`<br>`JMP $loop`|
|`#SET <VariableName> <Value>`|Sets the variable `<VariableName>` in the execution context to `<Value>`.<br>`#SET i 0`|
|`#SETX <VariableName> <Value>`|Sets the variable `<VariableName>` in the execution context to `<Value>` whilst replacing `_` with ` `.<br>`#SETX space _`|
|`#AP <VariableName> <Value>`|Reads the variable `<VariableName>` from the execution context and appends `<Value>` to it.<br>`#AP space ,`|
|`#APX <VariableName> <Value>`|Reads the variable `<VariableName>` from the execution context and appends `<Value>` to it whilst replacing `_` with ` `.<br>`#APX space _`|

### Debugger Commands

|Command|Explanation|
|--|--|
||Press enter to continue to next statement.|                                                                   
|`EXEC <STATEMENT>`|Move the Instruction Pointer to the previous instruction and execute the given instruction `<STATEMENT>`.|
|`RUN`|Continue Execution.|
|`CODE`|Show the next few lines of code.|
|`CODEAT <INSTRUCTION_INDEX>`|Show the next few lines of code starting at `<INSTRUCTION_INDEX>`.|
|`ADDB <INSTRUCTION_INDEX ...>`|Add breakpoints at all specified instruction indices.|
|`PUTB`|Show all breakpoints.|
|`RMB <INSTRUCTION_INDEX>`|Remove the breakpoint at `<INSTRUCTION_INDEX>`.|
|`CLRB`|Remove All Breakpoints.|