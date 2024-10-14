using BotSharp.Plugin.SqlDriver.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace BotSharp.Plugin.SqlDriver.Functions;

public class GetTableDefinitionFn : IFunctionCallback
{
    public string Name => "sql_table_definition";
    public string Indication => "Obtain the relevant data structure definitions.";
    private readonly IServiceProvider _services;
    private readonly ILogger<GetTableDefinitionFn> _logger;

    public GetTableDefinitionFn(
        IServiceProvider services,
        ILogger<GetTableDefinitionFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<SqlStatement>(message.FunctionArgs);
        var tables = args.Tables;
        var agentService = _services.GetRequiredService<IAgentService>();
        var settings = _services.GetRequiredService<SqlDriverSetting>();

        // Get table DDL from database
        var tableDdls = settings.DatabaseType switch
        {
            "MySql" => GetDdlFromMySql(tables),
            "SqlServer" => GetDdlFromSqlServer(tables),
            _ => throw new NotImplementedException($"Database type {settings.DatabaseType} is not supported.")
        };
    
        message.Content = string.Join("\r\n\r\n", tableDdls);
        return true;
    }

    private List<string> GetDdlFromMySql(string[] tables)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var tableDdls = new List<string>();
        using var connection = new MySqlConnection(settings.MySqlMetaConnectionString ?? settings.MySqlConnectionString);
        connection.Open();

        foreach (var table in tables)
        {
            try
            {
                var escapedTableName = MySqlHelper.EscapeString(table);
                var sql = $"SHOW CREATE TABLE `{escapedTableName}`";

                using var command = new MySqlCommand(sql, connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var result = reader.GetString(1);
                    tableDdls.Add(result);
                }

                reader.Close();
                command.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when getting ddl statement of table {table}. {ex.Message}\r\n{ex.InnerException}");
            }
        }

        connection.Close();
        return tableDdls;
    }

    private List<string> GetDdlFromSqlServer(string[] tables)
    {
        var settings = _services.GetRequiredService<SqlDriverSetting>();
        var tableDdls = new List<string>();
        using var connection = new SqlConnection(settings.SqlServerExecutionConnectionString ?? settings.SqlServerConnectionString);
        connection.Open();

        foreach (var table in tables)
        {
            try
            {
                var sql = @$"DECLARE @TableName NVARCHAR(128) = '{table}';
                            DECLARE @SQL NVARCHAR(MAX) = 'CREATE TABLE ' + @TableName + ' (';

                            SELECT @SQL = @SQL + '
                                ' + COLUMN_NAME + ' ' + 
                                DATA_TYPE + 
                                CASE 
                                    WHEN CHARACTER_MAXIMUM_LENGTH IS NOT NULL AND DATA_TYPE LIKE '%char%' 
                                        THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR(10)) + ')'
                                    WHEN DATA_TYPE IN ('decimal', 'numeric')
                                        THEN '(' + CAST(NUMERIC_PRECISION AS VARCHAR(10)) + ',' + CAST(NUMERIC_SCALE AS VARCHAR(10)) + ')'
                                    ELSE ''
                                END + ' ' + 
                                CASE WHEN IS_NULLABLE = 'NO' THEN 'NOT NULL' ELSE 'NULL' END + ',' 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = @TableName
                            ORDER BY ORDINAL_POSITION;

                            -- Remove the last comma and add closing parenthesis
                            SET @SQL = LEFT(@SQL, LEN(@SQL) - 1) + ');';

                            SELECT @SQL;";

                using var command = new SqlCommand(sql, connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var result = reader.GetString(0);
                    tableDdls.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when getting ddl statement of table {table}. {ex.Message}\r\n{ex.InnerException}");
            }
        }

        connection.Close();
        return tableDdls;
    }
}
