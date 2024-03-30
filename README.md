<a name="readme-top"></a>

<!-- PROJECT LOGO -->
<div align="center">
<h3 align="center">UPak</h3>

  <p align="center">
    A CLI for automating unity package operations
    <br />
    <br />
    <a href="https://github.com/errorStream/UPak/releases">Download</a>
    ·
    <a href="https://github.com/errorStream/UPak/issues">Report Bug</a>
    ·
    <a href="https://github.com/errorStream/UPak/issues">Request Feature</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents
  </summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project
      </a>
      <ul>
        <li>
          <a href="#built-with">Built With
          </a>
        </li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started
      </a>
      <ul>
        <li>
          <a href="#installation">Installation
          </a>
        </li>
      </ul>
    </li>
    <li>
      <a href="#usage">Usage
      </a>
      <ul>
        <li>
          <a href="#generate-a-unity-package">Generate a unity package
          </a>
        </li>
      </ul>
      <ul>
        <li>
          <a href="#download-a-library-from-nuget">Download a library from NuGet
          </a>
        </li>
      </ul>
    </li>
    <li>
      <a href="#roadmap">Roadmap
      </a>
    </li>
    <li>
      <a href="#contributing">Contributing
      </a>
    </li>
    <li>
      <a href="#license">License
      </a>
    </li>
    <li>
      <a href="#contact">Contact
      </a>
    </li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

![Screen recording of usage](./images/screenrecording.gif)

This is the project for UPak, a command line program which provides functionality for automating various package management operations in Unity.

See the [usage](#usage) section to see what kinds of operations this tool can currently automate.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



### Built With

* [Sharprompt](https://github.com/shibayan/Sharprompt)
* [Newtonsoft.Json](https://www.newtonsoft.com/json)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- GETTING STARTED -->
## Getting Started

This is an example of how you may give instructions on setting up your project locally.
To get a local copy up and running follow these simple example steps.

### Installation

1. Go to [releases](https://github.com/errorStream/UPak/releases)
2. Download the version which matches your platform
3. Unzip it and store the `upak` file somewhere
4. (Optional) Move the `upak` file to a directory in your path for easy calling

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- USAGE EXAMPLES -->
## Usage

Execute the upak file to see usage instructions. 

Note that you can use the `--safe` option to provide confirmation every time upak performs an operation which will mutate the state of your system. Used as such `upak --safe <command>`.

### Generate a unity package

Custom Unity package generation is done through `upak pack init`. 

```shell
upak pack init
```

It runs you through a series of questions with accompanying explanation, once this is complete it caries out generating the package with the appropriate settings.

This command supports both embedded (packages inside the Packages directory of a Unity project, which are installed into that project automatically) and local packages (packages which are stored anywhere on disk and can be linked to a Unity project by clicking "Install from disk" in the package manager and selecting the `package.json` file).

This command is useful because the requirements for a unity package are throughly and explicitly defined in the [unity documentation](https://docs.unity3d.com/Manual/CustomPackages.html), but the process of setting up this configuration is failry lengthly and error prone.

### Download a library from NuGet

Downloading a pre-built library from the NuGet repository is done through the `upak nuget install <package_name> <version>` command.

This command downloads a compatible dll of a given package of a given version from upak to the Unity project which this command is run inside of. When Unity finds such a file it installs it and makes the library code accessable to your scripts.

```shell
upak nuget install
```

This won't work on all libraries, and dependencies currently also have to be installed manually through this command, but it does make the process much less tediouse when it is needed.


<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ROADMAP -->
## Roadmap

- [ ] Samples generation
- [ ] Modification tools
- [ ] Ability to skip/add/remove assemblies
- [ ] Git repo generation
- [ ] License picking and generation
- [ ] Changelog automation

See the [open issues](https://github.com/errorStream/UPak/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>

<!-- CONTRIBUTING -->
## Contributing

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- LICENSE -->
## License

Distributed under the GNU GPLv3 License. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

ErrorStream - [itch.io profile](https://errorstream.itch.io) - errorstream@amequus.com

Project Link: [https://github.com/errorStream/UPak](https://github.com/errorStream/UPak)

<p align="right">(<a href="#readme-top">back to top</a>)</p>

