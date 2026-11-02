#!/usr/bin/env python3
"""Read one environment object on stdin, then execute the remaining arguments. Never echo values."""
import json,os,sys
if sys.stdin.isatty():
 import termios
 attrs=termios.tcgetattr(sys.stdin);attrs[3]&=~termios.ECHO;termios.tcsetattr(sys.stdin,termios.TCSANOW,attrs)
print('Ready for environment JSON on stdin (input is hidden).',flush=True)
values=json.loads(sys.stdin.readline())
os.execvpe(sys.argv[1],sys.argv[1:],dict(os.environ,**values))
