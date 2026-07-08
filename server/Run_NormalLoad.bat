@echo off
echo =======================================================
echo NORMAL LOAD TEST - 5000 CCU (Ramp-up over 2 minutes)
echo =======================================================
CCUTest.exe -bots 5000 -rampup 2m -duration 5m
pause
