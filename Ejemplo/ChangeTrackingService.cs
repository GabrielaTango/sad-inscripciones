using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

public class ChangeTrackingService
{
    private readonly string _connectionString;

    public ChangeTrackingService(string connectionString)
    {
        _connectionString = connectionString;
    }

    public long GetCurrentVersion()
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var cmd = new SqlCommand(
            "SELECT CHANGE_TRACKING_CURRENT_VERSION()", conn);

        return (long)cmd.ExecuteScalar();
    }

    public long GetMinValidVersion(string tableName)
    {
        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        using var cmd = new SqlCommand(
            $"SELECT CHANGE_TRACKING_MIN_VALID_VERSION(OBJECT_ID('{tableName}'))",
            conn);

        return (long)cmd.ExecuteScalar();
    }

    public bool IsVersionValid(string tableName, long lastVersion)
    {
        var minVersion = GetMinValidVersion(tableName);
        return lastVersion >= minVersion;
    }

    public List<ChangeRecord> GetChanges(string tableName, long lastVersion)
    {
        var results = new List<ChangeRecord>();

        using var conn = new SqlConnection(_connectionString);
        conn.Open();

        var query = $@"
            SELECT 
                CT.SYS_CHANGE_VERSION,
                CT.SYS_CHANGE_OPERATION,
                CT.Id
            FROM CHANGETABLE(CHANGES {tableName}, @lastVersion) AS CT";

        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@lastVersion", lastVersion);

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            results.Add(new ChangeRecord
            {
                Version = (long)reader["SYS_CHANGE_VERSION"],
                Operation = reader["SYS_CHANGE_OPERATION"].ToString(),
                Id = (int)reader["Id"]
            });
        }

        return results;
    }
}

public class ChangeRecord
{
    public long Version { get; set; }
    public string Operation { get; set; } // I, U, D
    public int Id { get; set; }
}