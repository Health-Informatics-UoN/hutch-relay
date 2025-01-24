# 🎾 Hutch.Rackit

RACKit is the Rquest API Client Kit, a .NET library for interacting with BC|RQuest's REST API, and part of the Hutch suite of tooling for federated analytics.

# Design notes

The intent for the library is to have self contained units that provide Rquest functionality (such as fetching query jobs, or submitting query responses), and then to compose those units in useful ways (such as providing a polling service that runs a configurable delegate when jobs are picked up). Users of the library can then use the high level services, or compose the low level pieces themselves in new and interesting ways.

Expected Services/Functionality initially:

- Api Client
  - Check a collection for availability queries
  - Check a collection for distribution queries
  - Submit a query response
- Polling Service
  - robust methods for performing query checks regularly, and triggering actions via configurable delegates
- Polling Hosted Service
  - .NET Hosted Service for running the above Polling Service as a long running background service in a .NET application.

# Getting started

It's early days, so currently work from source:

1. Have the latest .NET LTS SDK
1. Checkout the repo
1. Open the library .csproj

In future, expect:

- Sample apps showing usage
- GitHub packages from merges to `main` ("nightlies")
- GitHub releases
- Nuget.org packages for releases

# Contributing

Hutch.Rackit is © 2024 University of Nottingham and available under the MIT License.

Please engage with GitHub issues and discussions to contribute. PRs to resolve open issues are welcome.
