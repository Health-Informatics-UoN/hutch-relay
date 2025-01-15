using Hutch.Relay.Data;
using Hutch.Relay.Data.Entities;
using Hutch.Relay.Models;
using Hutch.Relay.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hutch.Relay.Services;

public class RelayTaskService(ApplicationDbContext db) : IRelayTaskService
{
  /// <summary>
  /// Get a RelayTask by id
  /// </summary>
  /// <param name="id">id of the RelayTask to get</param>
  /// <returns>The RelayTaskModel of the id</returns>
  /// <exception cref="KeyNotFoundException">The RelayTask does not exist.</exception>
  public async Task<RelayTaskModel> Get(string id)
  {
    var entity = await db.RelayTasks
                   .AsNoTracking()
                   .SingleOrDefaultAsync(t => t.Id == id)
                 ?? throw new KeyNotFoundException();

    return new RelayTaskModel
    {
      Id = entity.Id,
      CreatedAt = entity.CreatedAt,
      CompletedAt = entity.CompletedAt,
      Collection = entity.Collection
    };
  }

  /// <summary>
  /// Create a new RelayTask
  /// </summary>
  /// <param name="model">Model to Create.</param>
  /// <returns>The newly created RelayTask.</returns>
  public async Task<RelayTaskModel> Create(RelayTaskModel model)
  {
    var entity = new RelayTask
    {
      Id = model.Id,
      Collection = model.Collection
    };

    db.RelayTasks.Add(entity);
    await db.SaveChangesAsync();

    return new RelayTaskModel
    {
      Id = entity.Id,
      CreatedAt = entity.CreatedAt,
      CompletedAt = entity.CompletedAt,
      Collection = entity.Collection
    };
  }

  /// <summary>
  /// Set a RelayTask as complete.
  /// </summary>
  /// <param name="id">The id of the task to complete.</param>
  /// <returns></returns>
  /// <exception cref="KeyNotFoundException">The RelayTask does not exist.</exception>
  public async Task<RelayTaskModel> SetComplete(string id)
  {
    var entity = await db.RelayTasks
                   .SingleOrDefaultAsync(t => t.Id == id)
                 ?? throw new KeyNotFoundException();

    entity.CompletedAt = DateTimeOffset.Now;

    db.RelayTasks.Update(entity);
    await db.SaveChangesAsync();

    return new RelayTaskModel
    {
      Id = entity.Id,
      CreatedAt = entity.CreatedAt,
      CompletedAt = entity.CompletedAt,
      Collection = entity.Collection
    };
  }

  /// <summary>
  /// List RelayTasks that have not been completed.
  /// </summary>
  /// <returns>The list of incomplete RelayTasks</returns>
  public async Task<IEnumerable<RelayTaskModel>> ListIncomplete()
  {
    var query = await db.RelayTasks
      .AsNoTracking()
      .Where(x => x.CompletedAt == null)
      .ToListAsync();

    return query.Select(x => new RelayTaskModel
    {
      Id = x.Id,
      Collection = x.Collection,
      CreatedAt = x.CreatedAt
    });
  }

  /// <summary>
  /// Create a new RelaySubTask
  /// </summary>
  /// <returns>The newly created RelaySubTask</returns>
  public async Task<RelaySubTaskModel> CreateSubTask(string relayTaskId, Guid ownerId)
  {
    // Not 100% convinced by the multiple db reads here to satisfy nice error messages?
    var subNode = await db.SubNodes
                    .Include(x => x.RelayUsers)
                    .SingleAsync(x => x.Id == ownerId)
                  ?? throw new KeyNotFoundException($"The specified owner does not exist: {ownerId}");

    var parent = await db.RelayTasks.FindAsync(relayTaskId)
                 ?? throw new KeyNotFoundException($"The specified parent Relay Task does not exist: {relayTaskId}");

    var entity = new RelaySubTask
    {
      Owner = subNode,
      RelayTask = parent
    };

    db.RelaySubTasks.Add(entity);
    await db.SaveChangesAsync();

    // TODO: what's actually useful here?
    return new()
    {
      Id = entity.Id,
      Owner = new()
      {
        Id = subNode.Id,
        Owner = subNode.RelayUsers.First().UserName ?? string.Empty
      },
      RelayTask = new() // TODO: Automapper or something more sane than this?
      {
        Id = parent.Id,
        Collection = parent.Collection,
        CreatedAt = parent.CreatedAt,
        CompletedAt = parent.CompletedAt,
      }
    };
  }

  /// <summary>
  /// Set the Result of a RelaySubTask
  /// </summary>
  /// <param name="id">id of the RelaySubTask</param>
  /// <param name="result">Result value to set</param>
  /// <returns>The updated RelaySubTask</returns>
  /// <exception cref="KeyNotFoundException"></exception>
  public async Task<RelaySubTaskModel> SetSubTaskResult(Guid id, string result)
  {
    var entity = await db.RelaySubTasks
                   .Include(x => x.Owner)
                   .ThenInclude(x => x.RelayUsers)
                   .Include(x => x.RelayTask)
                   .SingleAsync(x => x.Id == id)
                 ?? throw new KeyNotFoundException();

    entity.Result = result;
    db.RelaySubTasks.Update(entity);
    await db.SaveChangesAsync();

    return new()
    {
      Id = entity.Id,
      Owner = new()
      {
        Id = entity.Owner.Id,
        Owner = entity.Owner.RelayUsers.First()
                  .UserName ??
                string.Empty
      },
      Result = entity.Result,
      RelayTask = new()
      {
        Id = entity.RelayTask.Id,
        Collection = entity.RelayTask.Collection,
        CreatedAt = entity.RelayTask.CreatedAt,
        CompletedAt = entity.RelayTask.CompletedAt,
      }
    };
  }

  /// <summary>
  /// Get a SubTask by ID
  /// </summary>
  /// <param name="id">Subtask Id</param>
  /// <returns>RelaySubTaskModel from the ID</returns>
  /// <exception cref="KeyNotFoundException">The RelaySubTask does not exist.</exception>
  public async Task<RelaySubTaskModel> GetSubTask(Guid id)
  {
    var entity = await db.RelaySubTasks.AsNoTracking().Include(relaySubTask => relaySubTask.Owner)
                   .ThenInclude(subNode => subNode.RelayUsers)
                   .Include(relaySubTask => relaySubTask.RelayTask)
                   .SingleOrDefaultAsync(t => t.Id == id)
                 ?? throw new KeyNotFoundException();

    return new RelaySubTaskModel()
    {
      Id = entity.Id,
      Owner = new SubNodeModel
      {
        Id = entity.Owner.Id,
        Owner = entity.Owner.RelayUsers.First().UserName ?? string.Empty
      },
      RelayTask = new RelayTaskModel()
      {
        Id = entity.RelayTask.Id
      },
      Result = entity.Result
    };
  }


  /// <summary>
  /// List RelaySubTasks for a given RelayTask
  /// </summary>
  /// <param name="relayTaskId">RelayTask ID</param>
  /// <param name="incompleteOnly">List incomplete SubTasks only.</param>
  /// <returns></returns>
  public async Task<IEnumerable<RelaySubTaskModel>> ListSubTasks(string relayTaskId, bool incompleteOnly)
  {
    var query = db.RelaySubTasks
      .AsNoTracking()
      .Where(x => x.RelayTask.Id == relayTaskId && x.RelayTask.CompletedAt == null);

    if (incompleteOnly) query = query.Where(x => x.Result == null);

    var relaySubTasks = await query.Include(relaySubTask => relaySubTask.Owner)
      .ThenInclude(subNode => subNode.RelayUsers)
      .Include(relaySubTask => relaySubTask.RelayTask).ToListAsync();

    return relaySubTasks.Select(x => new RelaySubTaskModel()
    {
      Id = x.Id,
      Owner = new SubNodeModel()
      {
        Id = x.Owner.Id,
        Owner = x.Owner.RelayUsers.First().UserName ?? string.Empty
      },
      RelayTask = new RelayTaskModel()
      {
        Id = x.RelayTask.Id
      },
      Result = x.Result
    });
  }
}
