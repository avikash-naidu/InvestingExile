using System.Data;
using InvestingExile.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InvestingExile.Pipeline;

/// <summary>
/// Holds the database write phase for one Pipeline run. While held, no other
/// Pipeline run can migrate or write. Acquire after every external fetch and
/// before MigrateAsync. Dispose after the write transaction commits or rolls back.
/// </summary>
internal sealed class PipelineWriteLock : IAsyncDisposable
{
    // Any bigint that no other advisory-lock user of this database picks.
    private const long Key = 0x496E_7645_7869_6C65; // "InvExile"

    private readonly AppDbContext _db;

    private PipelineWriteLock(AppDbContext db) => _db = db;

    /// <summary>
    /// Opens the context's connection and blocks on pg_advisory_lock until the
    /// key is free. Works on a database with no tables. The open connection is
    /// kept for the context's lifetime, so the lock, MigrateAsync, and the write
    /// transaction share one session.
    /// </summary>
    public static async Task<PipelineWriteLock> AcquireAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_lock({0})", [Key], cancellationToken);
        }
        catch
        {
            await db.Database.CloseConnectionAsync();
            throw;
        }

        return new PipelineWriteLock(db);
    }

    public async ValueTask DisposeAsync()
    {
        var connection = _db.Database.GetDbConnection();

        // A session lock dies with its session, so a broken or closed connection
        // no longer holds it. Unlocking there would only throw and hide the
        // exception that broke the run.
        if (connection.State is ConnectionState.Broken or ConnectionState.Closed)
        {
            await _db.Database.CloseConnectionAsync();
            return;
        }

        try
        {
            // No cancellation token: the lock must be released even when the run was cancelled.
            await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_unlock({0})", [Key]);
        }
        catch
        {
            // Closing returns the connection to the Npgsql pool with its session,
            // and the lock, still alive. Clearing the pool makes Npgsql discard
            // this connection on close instead, which ends the session.
            if (connection is NpgsqlConnection npgsql)
            {
                NpgsqlConnection.ClearPool(npgsql);
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }
}
