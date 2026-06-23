using Altinn.AccessMgmt.FFB.Jobs.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Npgsql;

namespace Altinn.AccessMgmt.FFB.Jobs;

public static class AssignmentInstanceCleanupJob
{
    public const string JobName = "AssignmentInstanceCleanup";

    public static async Task RunAsync(DuoRepo repo, JobRun run, AssignmentInstanceCleanupOptions opts, CancellationToken ct)
    {
        run.AddLog($"Assignment Instance Cleanup");

        if (!RoleConstants.TryGetByAll(opts.OldRole, out var oldRole))
        {
            run.AddLog($"Old role '{opts.OldRole}' not found in RoleConstants", true);
            return;
        }

        if (!RoleConstants.TryGetByAll(opts.NewRole, out var newRole))
        {
            run.AddLog($"New role '{opts.NewRole}' not found in RoleConstants", true);
            return;
        }

        if (!Guid.TryParse(opts.ChangedBySystem, out var changedBySystemId))
        {
            run.AddLog($"ChangedBySystem '{opts.ChangedBySystem}' is not a valid GUID", true);
            return;
        }

        int count = 0;          // total processed toward Limit (moved + redundant removed)
        int moved = 0;
        int removedDuplicates = 0;

        while (count < opts.Limit)
        {
            var instances = (await repo.GetAccAssignmentInstance(oldRole.Id, changedBySystemId, limit: opts.LoopLimit, ct: ct)).ToList();
            if (instances.Count == 0)
            {
                break;
            }

            var progressedThisBatch = 0;

            foreach (var instance in instances)
            {
                if (count >= opts.Limit)
                {
                    break;
                }

                run.AddLog($"Moving AssignmentInstance: {instance.Id} from Assignment: {instance.AssignmentId}");

                // Upsert Assignment
                var newAssignmentId = await repo.UpsertAssignment(instance.AssignmentFromId, instance.AssignmentToId, newRole.Id, instance.ValidFrom, instance.ChangedBy, instance.ChangedBySystem, opts.OperationId, ct);
                if (newAssignmentId is null || !newAssignmentId.HasValue)
                {
                    run.AddLog($"Failed to upsert assignment for AssignmentInstance: {instance.Id}", true);
                    continue;
                }

                // Move AssignmentInstance onto the new assignment.
                try
                {
                    var updated = await repo.UpdateAssignmentInstance(instance.Id, newAssignmentId.Value, DateTimeOffset.UtcNow, SystemEntityConstants.DBA, SystemEntityConstants.DBA, opts.OperationId);
                    if (!updated)
                    {
                        run.AddLog($"Failed to update AssignmentInstance '{instance.Id}' AssignmentId from '{instance.AssignmentId}' to '{newAssignmentId.Value}'", true);
                        continue;
                    }

                    moved++;
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    // The target assignment already has an instance for this resource/instance — it was
                    // already migrated, so this old row is redundant. Delete it instead of moving.
                    run.AddLog($"AssignmentInstance '{instance.Id}' already exists on target assignment '{newAssignmentId.Value}' — deleting redundant instance.");
                    var removed = await repo.DeleteAssignmentInstance(instance.Id, SystemEntityConstants.DBA, SystemEntityConstants.DBA, opts.OperationId);
                    if (!removed)
                    {
                        run.AddLog($"Failed to delete redundant AssignmentInstance '{instance.Id}'", true);
                        continue;
                    }

                    removedDuplicates++;
                }

                // Remove Old Assignment if it is now empty (a normal side effect, not a failure)
                var deleted = await repo.RemoveAssignmentIfEmpty(instance.AssignmentId, SystemEntityConstants.DBA, SystemEntityConstants.DBA, opts.OperationId);
                if (deleted)
                {
                    run.AddLog($"Deleted empty assignment '{instance.AssignmentId}'");
                }

                count++;
                progressedThisBatch++;
            }

            // Guard against an endless loop: if a full batch was fetched but nothing could be processed
            // (every instance hit a genuine upsert/update/delete failure), the same rows would return again.
            if (progressedThisBatch == 0)
            {
                run.AddLog("No instances in this batch could be processed — stopping to avoid an endless loop.", true);
                break;
            }
        }

        run.AddLog($"Done. Moved {moved}, removed {removedDuplicates} redundant assignment instance(s).");
    }
}

public record AssignmentInstanceCleanupOptions(string ChangedBySystem, string OldRole, string NewRole, string OperationId, int Limit, int LoopLimit);
