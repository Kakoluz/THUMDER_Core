# THUMDER Core
***THUMDER Core*** is a **DeLuXe** (*DLX*) CPU emulator written in C# and tries to be a replacement for *WinDLX*. It does read and accept the same directives and labels.

For the Web UI Written by ***Nonondev96*** go to this repo: [THUMDER](https://github.com/nonodev96/THUMDER)

## Installation
To install **THUMDER Core** go to the [release](https://github.com/Kakoluz/THUMDER_Core/releases/latest) tab and download the version for your operating system and architecture.

## Building
***Dependencies***
* .NET Core 7 SDK : [Dotnet Installation](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)

After .NET is installed, just navigate to the folder containing the .csproj file and run:
``` console
dotnet publish -c Release
```

## How to use
Currently the only way to use ***THUMDER Core*** is as a local command line emulator, it accepts the following sintax:
``` console
./THUMDER_Core [File] [Options]
    -h --help         Show the help message
    -S --server       Launch as a network server
    -v --version      Show version information
```

## DLX Assembly
For a complete list of how to code DLX assembly and supported instructions, go to the wiki 


## License
***THUMDER Core*** is available on Github under the [GNU GPLv3 license](https://github.com/Kakoluz/THUMDER_Core/blob/master/LICENSE.txt)

Copyright © 2022 Alberto Rodríguez Torres
