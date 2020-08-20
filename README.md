# GTiff2Tiles

![Icon](Icon/Icon.png)

**GTiff2Tiles** is an analogue of [gdal2tiles.py](https://github.com/OSGeo/gdal/blob/master/gdal/swig/python/scripts/gdal2tiles.py) and [MapTiler](https://www.maptiler.com/) on **C#** for creating tiles.

[![Build status](https://ci.appveyor.com/api/projects/status/wp5bbi08sgd4i9bh/branch/master?svg=true)](https://ci.appveyor.com/project/Gigas002/gtiff2tiles/branch/master)
[![Actions Status](https://github.com/Gigas002/GTiff2Tiles/workflows/.NET%20Core%20CI/badge.svg)](https://github.com/Gigas002/GTiff2Tiles/actions)
[![codecov](https://codecov.io/gh/Gigas002/GTiff2Tiles/branch/master/graph/badge.svg)](https://codecov.io/gh/Gigas002/GTiff2Tiles)

## Table of contents

- [GTiff2Tiles](#gtiff2tiles)
  - [Current version](#current-version)
  - [Docker Images](#docker-images)
  - [Build](#build)
  - [Documentation](#documentation)
  - [Examples](#examples)
  - [TODO](#todo)
  - [Contributing](#contributing)
  - [License](#license)
  - [3rd party resources](#3rd-party-resources)

Table of contents generated with [markdown-toc](http://ecotrust-canada.github.io/markdown-toc/).

## Current version

**Release 1.4.1 is the last release to support .NET Standard 2.0. Starting from version 2.0.0 solution targets .NET Core only!**

Current stable version can be found on GitHub Releases page: [![Release](https://img.shields.io/github/release/Gigas002/GTiff2Tiles.svg)](https://github.com/Gigas002/GTiff2Tiles/releases/latest), on NuGet (*GTiff2Tiles.Core only*): [![NuGet](https://img.shields.io/nuget/v/GTiff2Tiles.svg)](https://www.nuget.org/packages/GTiff2Tiles/) and [GitHub Packages Feed](https://github.com/Gigas002/GTiff2Tiles/packages).

Pre-release versions by CI are also available on [Releases](https://github.com/Gigas002/GTiff2Tiles/releases) page, [nuget](https://www.nuget.org/packages/GTiff2Tiles/) and [docker hub](https://hub.docker.com/r/gigas002/gtiff2tiles-console).

Information about changes since previous releases can be found in [changelog](CHANGELOG.md). This project supports [SemVer 2.0.0](https://semver.org/) (template is `{MAJOR}.{MINOR}.{PATCH}.{BUILD}`).

Previous versions can also be found on [releases](https://github.com/Gigas002/GTiff2Tiles/releases), [tags](https://github.com/Gigas002/GTiff2Tiles/tags) and [branches (source code)](https://github.com/Gigas002/GTiff2Tiles/branches) pages.

## Docker Images

Latest pre-built docker images (*from master branch*) for **GTiff2Tiles.Console** are available on [GitHub Packages Feed](https://github.com/Gigas002/GTiff2Tiles/packages) (`docker pull docker.pkg.github.com/gigas002/gtiff2tiles/gtiff2tiles-console:latest`) and on [Docker Hub](https://hub.docker.com/r/gigas002/gtiff2tiles-console) (`docker pull gigas002/gtiff2tiles-console`).

You can also build docker image by yourself by running [publish-local-docker.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/publish-local-docker.ps1) script with your **PowerShell**/**PowerShell Core**. It’ll create `gtiff2tiles-console` image.

## Build

Preferred way to build the solution is to use **VS2019 (16.7.2+)**, but you can also build projects, using dotnet CLI in your terminal or in **VSCode (1.48.0+)** with **omnisharp-vscode (1.23.1+)** extension.
Projects targets **.NET Core 5.0.0-preview.7**, so you’ll need **.NET Core 5.0.100-preview.7 SDK**.

Apps **release** binaries are made by [publish-github-release.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/publish-github-release.ps1) script, nuget package generated by [publish-github-nupkg.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/publish-github-nupkg.ps1) script, docker images are created by [publish-local-docker.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/publish-local-docker.ps1) and [publish-github-docker.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/publish-github-docker.ps1) scripts.
You can run tests and analyze code coverage by using [codecov-local.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/codecov-local.ps1) script.
Docs are generated by [create-docs.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/create-docs.ps1) script.

Note, that running these scripts requires installed **PowerShell** or **[PowerShell Core](https://github.com/PowerShell/PowerShell)** (also available on **Linux**/**OSX** systems!).

You'll also find full list for dependencies for all projects on the corresponding pages.

## Documentation

Docs are available to browse on [GitHub Pages](https://gigas002.github.io/GTiff2Tiles/). The offline alternative for core is [pdf](docs/pdf/gtiff2tiles.pdf) or generating docs by yourself, using **docfx** and [create-docs.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/create-docs.ps1) script. For offline users, you can also read `.md` files in corresponding directory if each project (info is the same, as on **GitHub Pages**).

Outdated docs for release 1.4.x Core's API are available on [GitHub Wiki](https://github.com/Gigas002/GTiff2Tiles/wiki).

## Examples

In [Examples](https://github.com/Gigas002/GTiff2Tiles/tree/master/Examples) directory you can find **GeoTIFFs** for some tests. Run [codecov-local.ps1](https://github.com/Gigas002/GTiff2Tiles/blob/master/codecov-local.ps1) script, and you'll see the new **Output** folder, in which test results are thrown.

## TODO

You can track, what’s planned to do in future releases on [projects](https://github.com/Gigas002/GTiff2Tiles/projects) and [milestones](https://github.com/Gigas002/GTiff2Tiles/milestones) pages.

## Contributing

Feel free to [contribute](CONTRIBUTING.md), make forks, change some code, add [issues](https://github.com/Gigas002/GTiff2Tiles/issues), etc.

## License

Project is licensed under [DBAD](LICENSE.md) license.

## 3rd party resources

Icon is provided by [Google’s material design](https://material.io/tools/icons/?icon=image&style=baseline) and is used in all of **GTiff2Tiles** projects.
