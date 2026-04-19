namespace TelegramMonitor;

public static class SqlSugarSetup
{
    public static void AddSqlSugarSetup(this IServiceCollection services)
    {
        var config = App.GetConfig<DbConnectionOptions>("DbConnection");

        if (!Enum.TryParse<DbType>(config.DbType, true, out var dbType))
            throw new InvalidOperationException($"无效的数据库类型: {config.DbType}");

        var sqlSugar = new SqlSugarScope(
            new ConnectionConfig
            {
                DbType = dbType,
                ConnectionString = config.ConnectionString,
                IsAutoCloseConnection = true
            },
            db => { });

        services.AddSingleton<ISqlSugarClient>(sqlSugar);
        services.AddScoped<SqlSugarScope>(s => sqlSugar);

        InitializeDatabase(sqlSugar);
        services.AddSingleton<IKeywordRepository, KeywordRepository>();
        services.AddSingleton<IKeywordService, KeywordService>();
    }

    private static void InitializeDatabase(ISqlSugarClient db)
    {
        db.DbMaintenance.CreateDatabase();

        InitializeTable<KeywordConfig>(db);
        InitializeTable<TelegramAccount>(db);
        InitializeTable<TelegramMessageRecord>(db);
        InitializeTable<BotNotifyTarget>(db);
    }

    private static void InitializeTable<T>(ISqlSugarClient db) where T : class, new()
    {
        var tableName = db.EntityMaintenance.GetEntityInfo<T>().DbTableName;
        var existed = db.DbMaintenance.IsAnyTable(tableName);

        db.CodeFirst.InitTables<T>();

        Log.Information(existed
            ? $"表 {tableName} 已同步"
            : $"表 {tableName} 已创建");
    }
}
