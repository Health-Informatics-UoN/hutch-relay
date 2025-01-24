using Hutch.Relay.Data;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Tests;

public class Fixtures
{
  public readonly ApplicationDbContext DbContext;
    
  public Fixtures() 
  {
    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
      .UseInMemoryDatabase(databaseName: "TestDatabase")
      .Options;
        
    DbContext = new ApplicationDbContext(options);
  }
}
