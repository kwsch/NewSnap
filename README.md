NewSnap
=====
![License](https://img.shields.io/badge/License-ISC-blue.svg?style=flat-square)

New Pokémon Snap class libary and console application programmed in [C#](https://en.wikipedia.org/wiki/C_Sharp_%28programming_language%29).

Supports handling of `*.drp` archives (from the ROM/patches) and unpacking data from save files.

The console application provides a way to manually execute preprogrammed routines to export your game data.

## Building

NewSnap is a [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet/3.1) project which can be run on Windows/Mac/Linux.

The solution can be built with any compiler that supports **C# 9**. We recommend using IDEs such as [Visual Studio](https://visualstudio.microsoft.com/downloads/) or [Rider](https://www.jetbrains.com/rider/download/). They can open the .sln or .csproj file.

## Dependencies

Decompressing data within a `*.drp` archive requires having the [Oodle Decompressor dll](http://www.radgametools.com/oodlecompressors.htm) in the same folder as the executable. This program has a hardcoded reference to `oo2core_8_win64.dll`, which can be sourced from other games (for example Warframe, which is free on Steam).
