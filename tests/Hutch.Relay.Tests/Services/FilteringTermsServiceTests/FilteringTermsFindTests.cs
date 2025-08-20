using System.Data.Common;
using Hutch.Rackit.TaskApi;
using Hutch.Relay.Config.Beacon;
using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services;
using Hutch.Relay.Services.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Hutch.Relay.Tests.Services.FilteringTermsServiceTests;

public class FilteringTermsFindTests : IDisposable
{
  private readonly DbConnection? _connection = null;

  private readonly ApplicationDbContext _dbContext;

  public FilteringTermsFindTests()
  {
    // Ensure a unique DB per Test
    _dbContext = FixtureHelpers.NewDbContext(ref _connection);
    _dbContext.Database.EnsureCreated();
  }

  public void Dispose()
  {
    _dbContext.Database.EnsureDeleted();
    _connection?.Dispose();
  }

  public static TheoryData<List<string>, List<FilteringTerm>, List<CachedFilteringTerm>> GetMatchingTermsCases()
  {
    List<FilteringTerm> dbTerms = [
      new() { Term = "OMOP:123", Description = "Term 123", SourceCategory = CodeCategory.Condition },
      new() { Term = "OMOP:456", Description = "Term 456", SourceCategory = CodeCategory.Observation },
      new() { Term = "OMOP:789", Description = "Term 789", SourceCategory = CodeCategory.Gender, VarCat = CodeCategory.VarCatMap[CodeCategory.Gender] },
    ];

    List<CachedFilteringTerm> expected = [..
      dbTerms.Select(x => new CachedFilteringTerm()
        {
          Term = x.Term,
          Description = x.Description          ,
          SourceCategory = x.SourceCategory,
          VarCat = x.VarCat
        })];

    TheoryData<List<string>, List<FilteringTerm>, List<CachedFilteringTerm>> data = [];

    // None matching
    data.Add(
      [dbTerms[0].Term],
      [dbTerms[1]],
      []);

    // Exact match
    data.Add(
      [dbTerms[0].Term],
      [dbTerms[0]],
      [expected[0]]
    );

    // Partial match
    data.Add(
      [dbTerms[0].Term, dbTerms[1].Term],
      [dbTerms[1]],
      [expected[1]]
    );

    // Subset match
    data.Add(
      [dbTerms[0].Term, dbTerms[1].Term],
      dbTerms,
      [expected[0], expected[1]]
    );

    // Full Match
    data.Add([.. dbTerms.Select(x => x.Term)], dbTerms, expected);

    return data;
  }

  [Fact]
  public async Task Find_NoTerms_ReturnsEmpty()
  {
    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      Options.Create<RelayBeaconOptions>(new()),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>()
    );

    var actual = await service.Find([]);

    Assert.Empty(actual);
  }

  [Theory]
  [MemberData(nameof(GetMatchingTermsCases))]
  public async Task Find_TermsProvided_ReturnsMatching(List<string> inputTerms, List<FilteringTerm> dbTerms, List<CachedFilteringTerm> expected)
  {
    _dbContext.FilteringTerms.AddRange(dbTerms);
    await _dbContext.SaveChangesAsync();

    var service = new FilteringTermsService(
      Mock.Of<ILogger<FilteringTermsService>>(),
      Options.Create<RelayBeaconOptions>(new()),
      Mock.Of<ISubNodeService>(),
      Mock.Of<IDownstreamTaskService>(),
      _dbContext,
      Mock.Of<IRelayTaskService>()
    );

    var actual = await service.Find(inputTerms);

    Assert.Equivalent(expected, actual);
  }
}
