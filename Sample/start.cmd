call "%ProgramFiles%\nodejs\nodejsvars.bat"
call "%ProgramFiles%\nodejs\nodevars.bat"
for /L %%i in (1,0,2) do (node --debug-brk=5858 main.js) 
