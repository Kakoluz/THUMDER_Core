# THUMDER Core
***THUMDER Core*** is a **DeLuXe** (*DLX*) CPU emulator written in C# and tries to be a replacement for *WinDLX*. It does read and accept the same directives and labels.

For the Web UI Written by ***Nonondev96*** go to this repo: [THUMDER](https://github.com/nonodev96/THUMDER)

 **THUMDER Core is not ready yet.**

## Installation
* **Windows:** Just grab the exe file from the release tab or build it. There are plans to add a *.msi* installer.
* **Linux:** If using debian based distros, you can get the *.deb* file on release tab, other distros will need to manually build it.
* **MacOS:** No installer or precompiled binary, you will have to build it.

## Building
Building ***THUMDER Core*** is as simple as opening the project in Visual Studio and building it. It comes with preconfigured profiles for Windows, Linux and Mac. And versions for x64 and ARM.

***Dependencies***
``` console
- Visual Studio 2019 or newer
- .Net Core 6 SDK
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