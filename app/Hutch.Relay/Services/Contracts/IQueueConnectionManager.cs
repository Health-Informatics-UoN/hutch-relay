using Hutch.Rackit.TaskApi.Models;
using Hutch.Relay.Models;

namespace Hutch.Relay.Services.Contracts;

/// <summary>
/// An interface for the functional needs of a RelayTask Queue.
///
/// This is quite domain opinionated - not a general purpose queue service;
/// it cares about Relay SubNodes and Task Types etc.
/// </summary>
public interface IQueueConnectionManager
{
  /// <summary>
  /// Test if the configured queue backend is available to receive messages, optionally on a specific named queue
  /// </summary>
  /// <param name="queueName">Optionally specify a queue name to also ensure is ready</param>
  /// <returns>True if the queue service is ready to receive items (in the named queue if provided)</returns>
  public Task<bool> IsReady(string? queueName = null);
}
