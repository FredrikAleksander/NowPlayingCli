# NowPlayingCli

Get Now Playing information in the console on Windows.

I wanted a solution for getting Spotify now playing information in Tmux when running
inside WSL (Windows Subsystem for Linux) on Windows. I could not find any available solutions, so I made this simple C# program, that will use the Windows 10 SystemMediaTransportControls API to listen for what is currently playing, and printing it to the console. Because there is alot of applications that normally sends Now Playing information to Windows, it is useful to use a whitelist that filters information from some sources. By passing program names (eg spotify.exe) as a parameter to the program, it will only listen for information from those programs. By default spotify.exe is used if none is specified.

Here is an example of how to use all the options in one command:

```
NowPlayingCli --type music --icon-music M spotify.exe
```

Requires dotnet core SDK 3.1 or higher to build
