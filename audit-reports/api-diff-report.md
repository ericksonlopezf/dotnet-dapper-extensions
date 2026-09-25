# API DIFF: a2fd2cd vs bf2b566

## ❌ REMOVED / RENAMED TYPES (Breaking)
- `class` `EricksonLopez.DapperExtensions.MySql.MySqlDapperExtensions` (defined in `src/EricksonLopez.DapperExtensions.MySql/MySqlMarker.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.OracleDapperExtensions` (defined in `src/EricksonLopez.DapperExtensions.Oracle/OracleMarker.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.Bulk.BulkParameters<T>` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Bulk/BulkParameters.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.Bulk.BulkParameters` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Bulk/BulkParameters.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.TypeHandlers.JsonbTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/TypeHandlers/JsonbTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSQL.TypeHandlers.NpgsqlTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/TypeHandlers/JsonbTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.SqlServerDapperExtensions` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/SqlServerMarker.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.SqliteDapperExtensions` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/SqliteMarker.cs`)

## ✨ ADDED TYPES
- `class` `EricksonLopez.DapperExtensions.DependencyInjection.DapperExtensionsOptions` (defined in `src/EricksonLopez.DapperExtensions.DependencyInjection/DapperExtensionsServiceCollectionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.DependencyInjection.DapperExtensionsServiceCollectionExtensions` (defined in `src/EricksonLopez.DapperExtensions.DependencyInjection/DapperExtensionsServiceCollectionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.HealthChecks.DapperHealthCheck` (defined in `src/EricksonLopez.DapperExtensions.HealthChecks/DapperHealthCheck.cs`)
- `class` `EricksonLopez.DapperExtensions.HealthChecks.DapperHealthCheckOptions` (defined in `src/EricksonLopez.DapperExtensions.HealthChecks/DapperHealthCheckOptions.cs`)
- `class` `EricksonLopez.DapperExtensions.HealthChecks.DapperHealthChecksBuilderExtensions` (defined in `src/EricksonLopez.DapperExtensions.HealthChecks/DapperHealthChecksBuilderExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.Bulk.BulkBuilder<T>` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.Bulk.BulkBuilder` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.TypeHandlers.JsonTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.MariaDb.TypeHandlers.MariaDbTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.MariaDB/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.Bulk.BulkBuilder<T>` (defined in `src/EricksonLopez.DapperExtensions.MySql/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.Bulk.BulkBuilder` (defined in `src/EricksonLopez.DapperExtensions.MySql/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.MySql/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.MySql/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.MySql/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.TypeHandlers.JsonTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.MySql/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.MySql.TypeHandlers.MySqlTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.MySql/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.OpenTelemetry.DapperDiagnostics` (defined in `src/EricksonLopez.DapperExtensions.OpenTelemetry/DapperDiagnostics.cs`)
- `class` `EricksonLopez.DapperExtensions.OpenTelemetry.DapperOpenTelemetryOptions` (defined in `src/EricksonLopez.DapperExtensions.OpenTelemetry/DapperOpenTelemetryOptions.cs`)
- `class` `EricksonLopez.DapperExtensions.OpenTelemetry.DapperOpenTelemetryServiceCollectionExtensions` (defined in `src/EricksonLopez.DapperExtensions.OpenTelemetry/DapperOpenTelemetryServiceCollectionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.OpenTelemetry.OpenTelemetryDbConnectionExtensions` (defined in `src/EricksonLopez.DapperExtensions.OpenTelemetry/OpenTelemetryDbConnectionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.Bulk.BulkBuilder<T>` (defined in `src/EricksonLopez.DapperExtensions.Oracle/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.Bulk.BulkBuilder` (defined in `src/EricksonLopez.DapperExtensions.Oracle/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.Oracle/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.Oracle/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.Oracle/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.TypeHandlers.JsonTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.Oracle/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.Oracle.TypeHandlers.OracleTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.Oracle/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.Bulk.BulkParameters<T>` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Bulk/BulkParameters.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.Bulk.BulkParameters` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Bulk/BulkParameters.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.TypeHandlers.JsonbTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/TypeHandlers/JsonbTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.PostgreSql.TypeHandlers.NpgsqlTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.PostgreSQL/TypeHandlers/JsonbTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.SourceGenerators.SqlEntityGenerator` (defined in `src/EricksonLopez.DapperExtensions.SourceGenerators/SqlEntityGenerator.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.Bulk.BulkDataTableBuilder<T>` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/Bulk/BulkDataTableBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.Bulk.BulkDataTableBuilder` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/Bulk/BulkDataTableBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.TypeHandlers.JsonTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlServer.TypeHandlers.SqlServerTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.SqlServer/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.Bulk.BulkBuilder<T>` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.Bulk.BulkBuilder` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/Bulk/BulkBuilder.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.Bulk.BulkExtensions` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/Bulk/BulkExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.Pagination.PagedQueryExtensions` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/Pagination/PagedQueryExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.Transactions.TransactionExtensions` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/Transactions/TransactionExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.TypeHandlers.JsonTypeHandler<T>` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/TypeHandlers/JsonTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.Sqlite.TypeHandlers.SqliteTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions.Sqlite/TypeHandlers/JsonTypeHandler.cs`)
- `interface` `EricksonLopez.DapperExtensions.MultiMap.IDataReaderMapper<T>` (defined in `src/EricksonLopez.DapperExtensions/MultiMap/IDataReaderMapper.cs`)
- `class` `EricksonLopez.DapperExtensions.MultiMap.MultiMapDescriptor` (defined in `src/EricksonLopez.DapperExtensions/MultiMap/IDataReaderMapper.cs`)
- `class` `EricksonLopez.DapperExtensions.MultiMap.MultiMapBuilder<TReturn>` (defined in `src/EricksonLopez.DapperExtensions/MultiMap/MultiMapBuilder.cs`)
- `interface` `EricksonLopez.DapperExtensions.Resilience.ISqlTransientErrorDetector` (defined in `src/EricksonLopez.DapperExtensions/Resilience/ISqlTransientErrorDetector.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.MySqlTransientErrorDetector` (defined in `src/EricksonLopez.DapperExtensions/Resilience/MySqlTransientErrorDetector.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.OracleTransientErrorDetector` (defined in `src/EricksonLopez.DapperExtensions/Resilience/OracleTransientErrorDetector.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.PostgreSqlTransientErrorDetector` (defined in `src/EricksonLopez.DapperExtensions/Resilience/PostgreSqlTransientErrorDetector.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.SavepointResilienceExtensions` (defined in `src/EricksonLopez.DapperExtensions/Resilience/SavepointResilienceExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.SqlResilienceDefaults` (defined in `src/EricksonLopez.DapperExtensions/Resilience/SqlResilienceDefaults.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.SqlResilienceExtensions` (defined in `src/EricksonLopez.DapperExtensions/Resilience/SqlResilienceExtensions.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.SqlServerTransientErrorDetector` (defined in `src/EricksonLopez.DapperExtensions/Resilience/SqlServerTransientErrorDetector.cs`)
- `class` `EricksonLopez.DapperExtensions.Resilience.SqliteTransientErrorDetector` (defined in `src/EricksonLopez.DapperExtensions/Resilience/SqliteTransientErrorDetector.cs`)
- `class` `EricksonLopez.DapperExtensions.SqlEntityAttribute` (defined in `src/EricksonLopez.DapperExtensions/SqlEntityAttribute.cs`)
- `class` `EricksonLopez.DapperExtensions.TypeHandlers.DapperTypeHandlerRegistrar` (defined in `src/EricksonLopez.DapperExtensions/TypeHandlers/DapperTypeHandlerRegistrar.cs`)
- `class` `EricksonLopez.DapperExtensions.TypeHandlers.DateOnlyTypeHandler` (defined in `src/EricksonLopez.DapperExtensions/TypeHandlers/DateOnlyTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.TypeHandlers.StringEnumTypeHandler<TEnum>` (defined in `src/EricksonLopez.DapperExtensions/TypeHandlers/StringEnumTypeHandler.cs`)
- `class` `EricksonLopez.DapperExtensions.TypeHandlers.TimeOnlyTypeHandler` (defined in `src/EricksonLopez.DapperExtensions/TypeHandlers/TimeOnlyTypeHandler.cs`)
- `interface` `EricksonLopez.DapperExtensions.UnitOfWork.ISavepoint` (defined in `src/EricksonLopez.DapperExtensions/UnitOfWork/IUnitOfWork.cs`)
- `interface` `EricksonLopez.DapperExtensions.UnitOfWork.IUnitOfWork` (defined in `src/EricksonLopez.DapperExtensions/UnitOfWork/IUnitOfWork.cs`)
- `class` `EricksonLopez.DapperExtensions.UnitOfWork.UnitOfWorkExtensions` (defined in `src/EricksonLopez.DapperExtensions/UnitOfWork/UnitOfWorkExtensions.cs`)

## 🔍 TYPE & MEMBER MODIFICATIONS
No modified types found.


# API DIFF: bf2b566 vs e54cf0e

## 🔍 TYPE & MEMBER MODIFICATIONS
### `EricksonLopez.DapperExtensions.HealthChecks.DapperHealthCheck` (class)
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public void DapperHealthCheck(Func<CancellationToken, Task<IDbConnection>> connectionFactory, DapperHealthCheckOptions? options = null)` -> `public void DapperHealthCheck(Func<CancellationToken, Task<IDbConnection>> connectionFactory, DapperHealthCheckOptions? options = null, TimeProvider? timeProvider = null)`

### `EricksonLopez.DapperExtensions.SourceGenerators.SqlEntityGenerator` (class)
  - ✨ ADDED MEMBER: `public static bool IsSyntaxTargetForGeneration(SyntaxNode node)`

### `EricksonLopez.DapperExtensions.Resilience.SqlResilienceDefaults` (class)
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline Standard(ISqlTransientErrorDetector detector)` -> `public static ResiliencePipeline Standard(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline StandardWithCircuitBreaker(ISqlTransientErrorDetector detector, double failureRatio = 0.5, TimeSpan? samplingDuration = null, int minimumThroughput = 10, TimeSpan? breakDuration = null)` -> `public static ResiliencePipeline StandardWithCircuitBreaker(ISqlTransientErrorDetector detector, double failureRatio = 0.5, TimeSpan? samplingDuration = null, int minimumThroughput = 10, TimeSpan? breakDuration = null, TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline Aggressive(ISqlTransientErrorDetector detector)` -> `public static ResiliencePipeline Aggressive(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline Conservative(ISqlTransientErrorDetector detector)` -> `public static ResiliencePipeline Conservative(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline<T> Standard<T>(ISqlTransientErrorDetector detector)` -> `public static ResiliencePipeline<T> Standard<T>(ISqlTransientErrorDetector detector, TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline<T> StandardWithCircuitBreaker<T>(ISqlTransientErrorDetector detector, double failureRatio = 0.5, TimeSpan? samplingDuration = null, int minimumThroughput = 10, TimeSpan? breakDuration = null)` -> `public static ResiliencePipeline<T> StandardWithCircuitBreaker<T>(ISqlTransientErrorDetector detector, double failureRatio = 0.5, TimeSpan? samplingDuration = null, int minimumThroughput = 10, TimeSpan? breakDuration = null, TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForSqlServer()` -> `public static ResiliencePipeline ForSqlServer(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForSqlServerWithCircuitBreaker()` -> `public static ResiliencePipeline ForSqlServerWithCircuitBreaker(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForPostgreSql()` -> `public static ResiliencePipeline ForPostgreSql(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForPostgreSqlWithCircuitBreaker()` -> `public static ResiliencePipeline ForPostgreSqlWithCircuitBreaker(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForMySql()` -> `public static ResiliencePipeline ForMySql(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForMySqlWithCircuitBreaker()` -> `public static ResiliencePipeline ForMySqlWithCircuitBreaker(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForSqlite()` -> `public static ResiliencePipeline ForSqlite(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForSqliteWithCircuitBreaker()` -> `public static ResiliencePipeline ForSqliteWithCircuitBreaker(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForOracle()` -> `public static ResiliencePipeline ForOracle(TimeProvider? timeProvider = null)`
  - ⚠️ MODIFIED MEMBER (Potential Breaking): `public static ResiliencePipeline ForOracleWithCircuitBreaker()` -> `public static ResiliencePipeline ForOracleWithCircuitBreaker(TimeProvider? timeProvider = null)`

### `EricksonLopez.DapperExtensions.UnitOfWork.IUnitOfWork` (interface)
  - Base / Interface changed: `: System.IAsyncDisposable` -> `: IAsyncDisposable`



# API DIFF: e54cf0e vs 9cb79a6

## 🔍 TYPE & MEMBER MODIFICATIONS
### `EricksonLopez.DapperExtensions.Resilience.SqlResilienceDefaults` (class)
  - ✨ ADDED MEMBER: `public static IResiliencePipeline StandardPipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-standard", TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline StandardWithCircuitBreakerPipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-standard-cb", double failureRatio = 0.5, TimeSpan? samplingDuration = null, int minimumThroughput = 10, TimeSpan? breakDuration = null, TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline AggressivePipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-aggressive", TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ConservativePipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-conservative", TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqlServerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqlServerWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForPostgreSqlPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForPostgreSqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForMySqlPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForMySqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqlitePipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqliteWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForOraclePipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForOracleWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`



# API DIFF: e54cf0e vs WorkingTree

## 🔍 TYPE & MEMBER MODIFICATIONS
### `EricksonLopez.DapperExtensions.Resilience.SqlResilienceDefaults` (class)
  - ✨ ADDED MEMBER: `public static IResiliencePipeline StandardPipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-standard", TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline StandardWithCircuitBreakerPipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-standard-cb", double failureRatio = 0.5, TimeSpan? samplingDuration = null, int minimumThroughput = 10, TimeSpan? breakDuration = null, TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline AggressivePipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-aggressive", TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ConservativePipeline(ISqlTransientErrorDetector detector, string pipelineName = "sql-conservative", TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqlServerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqlServerWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForPostgreSqlPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForPostgreSqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForMySqlPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForMySqlWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqlitePipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForSqliteWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForOraclePipeline(TimeProvider? timeProvider = null)`
  - ✨ ADDED MEMBER: `public static IResiliencePipeline ForOracleWithCircuitBreakerPipeline(TimeProvider? timeProvider = null)`

