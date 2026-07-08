@echo off
echo =======================================================
echo STRESS TEST - 5000 CCU (Ramp-up over 15 seconds)
echo =======================================================
CCUTest.exe -bots 5000 -rampup 15s -duration 2m
pause
