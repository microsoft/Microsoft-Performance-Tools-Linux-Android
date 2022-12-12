@echo on
java.exe -jar antlr-4.11.1-complete.jar -Dlanguage=CSharp Ctf.g4
move CtfListener.cs ICtfListener.cs

