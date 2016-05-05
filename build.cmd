@echo off
SET here=%cd%
CD %~dp0
powershell ./build.ps1 %1 %2 %3 %4 %5 %6 %7 %8 %9 -experimental
CD %here%