# 🚀 Level Up Your Dapper Game: Write Better, Safer Data Code with Ease

Dapper is amazing for its speed and simplicity—but it's also barebones. If you've worked with Entity Framework, you might miss features like change tracking, auditing, and Unit of Work support.

This article shows you how to take Dapper to the next level by:
- Wrapping Dapper in a **clean Unit of Work abstraction**
- Automatically handling **Created/Modified timestamps and users**
- Creating a pluggable, testable, and production-grade data access layer

---

## ✅ The Pain Points of Raw Dapper

Dapper is super fast, but...
- ❌ No built-in transaction scope
- ❌ No auditing support (CreatedBy, ModifiedBy)
- ❌ No unified place to commit/rollback
- ❌ No easy way to plug in user context
- ❌ No bulk insert support

---

## 🤝 Comparing: Raw Dapper vs Unit of Work

| Feature                       | Raw Dapper | With UoW Wrapper ✅ |
|------------------------------|------------|--------------------|
| Connection & Transaction Mgmt| ❌ Manual   | ✅ Built-in        |
| Insert/Update/Delete Helpers | ❌ No       | ✅ Yes             |
| Auto Audit Fields            | ❌ No       | ✅ Yes             |
| Testability                  | ❌ Hard     | ✅ Easy            |
| Bulk Insert                  | ❌ Manual or 3rd-party | ✅ Built-in with SqlBulkCopy |

---

## 💼 Usage Comparison

### ❌ Raw Dapper (Insert Example)
```csharp
using var connection = new SqlConnection("...connectionString...");
await connection.OpenAsync();

var user = new User { Id = Guid.NewGuid(), Name = "Alex" };

await connection.ExecuteAsync(
    "INSERT INTO Users (Id, Name) VALUES (@Id, @Name)",
    user);
```

### ✅ UoW Wrapper (Insert Example)
```csharp
using var uow = _unitOfWorkFactory.CreateUOW();

var user = new User { Name = "Alex" };
await uow.InsertAsync(user);

uow.Commit();
```

### ✅ UoW Wrapper (Get By Id)
```csharp
using var uow = _unitOfWorkFactory.CreateUOW();
var user = await uow.GetAsync<User>(userId);
uow.Commit();
```

### ✅ UoW Wrapper (Update Example)
```csharp
using var uow = _unitOfWorkFactory.CreateUOW();

var user = await uow.GetAsync<User>(userId);
user.Name = "Updated Name";
await uow.UpdateAsync(user);

uow.Commit();
```

### ✅ UoW Wrapper (Bulk Copy Example)
```csharp
using var uow = _unitOfWorkFactory.CreateUOW();

var users = Enumerable.Range(1, 1000).Select(i => new User
{
    Id = Guid.NewGuid(),
    Name = $"User {i}"
});

await uow.BulkCopyAsync(users);
uow.Commit();
```

- 🚀 Cleaner and consistent syntax
- ✅ Audit fields are handled automatically
- ✅ Shared transaction for safety

## 🛠️ Introducing the Architecture

We'll use these key building blocks:

- `BaseEntity` — for audit fields
- `ICurrentUserService` — abstraction for current user context
- `IUnitOfWork` — transactional Dapper wrapper
- `UnitOfWork` — Dapper + audit logic + transaction + bulk copy
- `UnitOfWorkFactory` — for creating multiple UoW instances per method

---

## 📦 Step 1: The BaseEntity with Audit Fields

```csharp
using Dapper.Contrib.Extensions;

public class BaseEntity<TID> : AuditableEntity
{
    [Key]
    public TID Id { get; set; } = default!;
}

public class AuditableEntity
{
    public DateTimeOffset Created { get; set; }
    public string CreatedBy { get; set; } = default!;
    public DateTimeOffset? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
```

---

## 👤 Step 2: The Current User Context

```csharp
public interface ICurrentUserService
{
    public string UserId { get; }
}

public class CurrentUserService : ICurrentUserService
{
    public string UserId => "SYSTEM"; // Replace with real claims logic
}
```

---

## 🔄 Step 3: The Unit of Work Interface

```csharp
public interface IUnitOfWork : IDisposable
{
    IDbConnection Connection { get; }

    Task InsertAsync<T>(T entity) where T : class;
    Task UpdateAsync<T>(T entity) where T : class;
    Task DeleteAsync<T>(T entity) where T : class;
    Task<T?> GetAsync<T>(object id) where T : class;
    Task<IEnumerable<T>> GetAllAsync<T>() where T : class;
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);
    Task<T> QuerySingleAsync<T>(string sql, object? param = null);
    Task<int> ExecuteAsync(string sql, object? param = null);
    Task BulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null);

    void Commit();
    void Rollback();
}
```

---

## 💡 Step 4: The Unit of Work Implementation

```csharp
public class UnitOfWork : IUnitOfWork
{
    public IDbConnection Connection { get; }
    private IDbTransaction? _transaction;
    private bool _isCompleted;
    private readonly ICurrentUserService _currentUserService;

    public UnitOfWork(IDbConnection connection, ICurrentUserService currentUserService, bool transactional = true)
    {
        SqlMapperExtensions.TableNameMapper = (type) => type.Name;
        _currentUserService = currentUserService;
        Connection = connection;
        Connection.Open();
        if (transactional)
        {
            _transaction = Connection.BeginTransaction();
        }
        else
        {
            _isCompleted = true;
        }
    }

    public async Task InsertAsync<T>(T entity) where T : class
    {
        if (entity is BaseEntity auditable)
        {
            var now = DateTimeOffset.UtcNow;
            auditable.Created = now;
            auditable.CreatedBy = _currentUserService.UserId;
        }
        await Connection.InsertAsync(entity, _transaction);
    }

    public async Task UpdateAsync<T>(T entity) where T : class
    {
        if (entity is BaseEntity auditable)
        {
            auditable.LastModified = DateTimeOffset.UtcNow;
            auditable.LastModifiedBy = _currentUserService.UserId;
        }
        await Connection.UpdateAsync(entity, _transaction);
    }

    public async Task DeleteAsync<T>(T entity) where T : class =>
        await Connection.DeleteAsync(entity, _transaction);

    public async Task<T?> GetAsync<T>(object id) where T : class =>
        await Connection.GetAsync<T>(id, _transaction);

    public async Task<IEnumerable<T>> GetAllAsync<T>() where T : class =>
        await Connection.GetAllAsync<T>(_transaction);

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null) =>
        await Connection.QueryAsync<T>(sql, param, _transaction);

    public async Task<T> QuerySingleAsync<T>(string sql, object? param = null) =>
        await Connection.QuerySingleAsync<T>(sql, param, _transaction);

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null) =>
        await Connection.QueryFirstOrDefaultAsync<T>(sql, param, _transaction);

    public async Task<int> ExecuteAsync(string sql, object? param = null) =>
        await Connection.ExecuteAsync(sql, param, _transaction);

    public async Task BulkCopyAsync<T>(IEnumerable<T> items, string? tableName = null)
    {
        if (Connection is not SqlConnection sqlConnection)
            throw new NotSupportedException("Bulk insert is only supported with SqlConnection.");

        var actualTableName = tableName ?? typeof(T).Name;

        var props = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        var dataTable = new DataTable();
        foreach (var prop in props)
        {
            var type = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            if (type == typeof(DateTimeOffset))
                type = typeof(DateTime);
            dataTable.Columns.Add(prop.Name, type);
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserService.UserId ?? "system";

        foreach (var item in items)
        {
            var idProp = typeof(T).GetProperty("Id");
            if (idProp != null && idProp.PropertyType == typeof(Guid))
            {
                var idValue = (Guid?)idProp.GetValue(item);
                if (idValue == null || idValue == Guid.Empty)
                {
                    idProp.SetValue(item, Guid.NewGuid());
                }
            }

            if (item is BaseEntity auditable)
            {
                auditable.Created = now;
                auditable.CreatedBy = userId;
            }

            var values = props.Select(p =>
            {
                var value = p.GetValue(item);
                if (value is DateTimeOffset dto)
                    return dto.UtcDateTime;
                return value ?? DBNull.Value;
            }).ToArray();

            dataTable.Rows.Add(values);
        }

        using var bulkCopy = new SqlBulkCopy(sqlConnection, SqlBulkCopyOptions.Default, (SqlTransaction?)_transaction)
        {
            DestinationTableName = actualTableName
        };

        foreach (DataColumn column in dataTable.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(dataTable);
    }

    public void Commit()
    {
        if (_isCompleted) return;
        _transaction?.Commit();
        _isCompleted = true;
    }

    public void Rollback()
    {
        if (_isCompleted) return;
        _transaction?.Rollback();
        _isCompleted = true;
    }

    public void Dispose()
    {
        if (!_isCompleted)
        {
            try { _transaction?.Rollback(); } catch { }
        }
        _transaction?.Dispose();
        Connection.Dispose();
    }
}
```

---

## 🏭 Step 5: The Factory

```csharp
public interface IUnitOfWorkFactory
{
    public IUnitOfWork CreateUOW();
}

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly string _connectionString;
    private readonly ICurrentUserService _currentUserService;

    public UnitOfWorkFactory(string connectionString, ICurrentUserService currentUserService)
    {
        _connectionString = connectionString;
        _currentUserService = currentUserService;
    }

    public IUnitOfWork CreateUOW()
    {
        var connection = new SqlConnection(_connectionString);
        return new UnitOfWork(connection, _currentUserService);
    }
}
```

---

## 🔌 Dependency Injection Setup (ASP.NET Core)

```csharp
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUnitOfWorkFactory>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var user = provider.GetRequiredService<ICurrentUserService>();
    return new UnitOfWorkFactory(config.GetConnectionString("DefaultConnection"), user);
});
```

---

## ✅ Benefits

- 🧼 Clean separation of concerns
- 🔁 Transaction-safe Dapper access
- 🔒 Auditing baked in automatically
- ⚡ Fast `SqlBulkCopy` support for batch inserts
- 🔧 Pluggable and testable

---

Happy coding! 💙

