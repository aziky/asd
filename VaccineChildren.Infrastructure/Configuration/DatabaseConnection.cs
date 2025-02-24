﻿
using Microsoft.Extensions.Logging;

namespace VaccineChildren.Infrastructure.Configuration;

public class DatabaseConnection
{
    private readonly ILogger<DatabaseConnection> _logger;

    public DatabaseConnection()
    {
        
    }

    public DatabaseConnection(ILogger<DatabaseConnection> logger)
    {
        _logger = logger;
    }
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string SSLMode { get; set; }
    public string TrustServerCertificate { get; set; }
    public string Pooling { get; set; }
    public int MaxPoolSize { get; set; }
   
    public string ToConnectionString()
    {
        string connectionString =
            $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};SSL Mode={SSLMode};Trust Server Certificate={TrustServerCertificate};Pooling={Pooling};MaxPoolSize={MaxPoolSize};";
        _logger?.LogInformation("{ClassName} - Generated connection string: {ConnectionString}", nameof(DatabaseConnection), connectionString); 
        return connectionString;
    }
}