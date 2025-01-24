[![Hutch][hutch-logo]][hutch-repo]

# 🐇 Hutch Relay ![MIT License][license-badge]

| | | |
|-|-|-|
| ![.NET][dotnet-badge] | [![Relay Docker Images][docker-badge]][relay-containers] | [![Relay Docs][docs-badge]][relay-docs] |

A Federated Proxy for the HDR UK Cohort Discovery Tool's Task API.

- Connects to an upstream Task API (e.g. the HDR UK Cohort Discovery Tool).
- Fetches tasks.
- Queues them for one or more downstream sub nodes (e.g. [Bunny][bunny-repo] instances).
- Accepts task results from the downstream nodes.
- Submits aggregate results to the upstream Task API.

Implements a subset of the Task API for the downstream nodes to interact with.

[hutch-logo]: https://raw.githubusercontent.com/HDRUK/hutch/main/assets/Hutch%20splash%20bg.svg
[hutch-repo]: https://github.com/health-informatics-uon/hutch

[bunny-repo]: https://github.com/Health-Informatics-UoN/hutch-bunny

[relay-docs]: https://health-informatics-uon.github.io/hutch/relay
[relay-containers]: https://github.com/Health-Informatics-UoN/hutch-relay/pkgs/container/hutch%2Frelay

[rackit-packages]: https://github.com/Health-Informatics-UoN/hutch-relay/pkgs/nuget/Hutch.Rackit
[rackit-readme]: https://github.com/Health-Informatics-UoN/hutch-relay/blob/main/lib/Hutch.Rackit/README.md

[license-badge]: https://img.shields.io/github/license/health-informatics-uon/hutch-relay.svg
[dotnet-badge]: https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white
[docker-badge]: https://img.shields.io/badge/docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white
[nuget-badge]: https://img.shields.io/badge/nuget-%23004880?style=for-the-badge&logo=nuget&logoColor=white
[docs-badge]: https://img.shields.io/badge/docs-black?style=for-the-badge&labelColor=%23222
[readme-badge]: https://img.shields.io/badge/readme-lightgrey?style=for-the-badge&labelColor=%23222

## 🎾 RACKit

| | | |
|-|-|-|
| ![.NET][dotnet-badge] | [![RACKit NuGet package][nuget-badge]][rackit-packages] | [![RACKit Readme][readme-badge]][rackit-readme] |

RACKit is the RQuest API Client Kit, a .NET Library for interacting with the HDR UK Cohort Discovery Task API.

### Samples

The `samples/` directory contains a sample application showcasing the use of RACKit to connect to a Task API.
