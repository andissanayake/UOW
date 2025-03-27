# 🚀 Level Up Your Dapper Game: Write Better, Safer Data Code with Ease

Dapper is amazing for its speed and simplicity—but it's also barebones. If you've worked with Entity Framework, you might miss features like change tracking, auditing, and Unit of Work support.

This article shows you how to take Dapper to the next level by:
- Wrapping Dapper in a **clean Unit of Work abstraction**
- Automatically handling **Created/Modified timestamps and users**
- Creating a pluggable, testable, and production-grade data access layer

---

## ✅ The Pain Points of Raw Dapper

Dapper is super fast, but...
- No built-in transaction scope
- No auditing support (CreatedBy, ModifiedBy)
- No unified place to commit/rollback
- No easy way to plug in user context

---

## 🛠️ Introducing the Architecture

We'll use these key building blocks:

- `BaseEntity` — for audit fields
- `ICurrentUserService` — abstraction for current user context
- `IUnitOfWork` — transactional Dapper wrapper
- `UnitOfWork` — Dapper + audit logic + transaction
- `UnitOfWorkFactory` — for creating multiple UoW instances per method

---

## 📦 Step 1: The BaseEntity with Audit Fields

```csharp
public class BaseEntity
{
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}
```

---

## 👤 Step 2: The Current User Context

```csharp
public interface ICurrentUserService
{
    string UserId { get; }
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
    Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null);
    Task<T> QuerySingleAsync<T>(string sql, object? param = null);
    Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null);
    Task<int> ExecuteAsync(string sql, object? param = null);

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
    private IDbTransaction _transaction;
    private bool _isCompleted;
    private readonly ICurrentUserService _currentUserService;

    public UnitOfWork(IDbConnection connection, ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
        Connection = connection;
        Connection.Open();
        _transaction = Connection.BeginTransaction();
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

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null) =>
        await Connection.QueryAsync<T>(sql, param, _transaction);

    public async Task<T> QuerySingleAsync<T>(string sql, object? param = null) =>
        await Connection.QuerySingleAsync<T>(sql, param, _transaction);

    public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null) =>
        await Connection.QueryFirstOrDefaultAsync<T>(sql, param, _transaction);

    public async Task<int> ExecuteAsync(string sql, object? param = null) =>
        await Connection.ExecuteAsync(sql, param, _transaction);

    public void Commit()
    {
        if (_isCompleted) return;
        _transaction.Commit();
        _isCompleted = true;
    }

    public void Rollback()
    {
        if (_isCompleted) return;
        _transaction.Rollback();
        _isCompleted = true;
    }

    public void Dispose()
    {
        if (!_isCompleted)
        {
            try { _transaction.Rollback(); } catch { }
        }
        _transaction.Dispose();
        Connection.Dispose();
    }
}
```

---

## 🏭 Step 5: The Factory (Optional But Powerful)

```csharp
public interface IUnitOfWorkFactory
{
    IUnitOfWork CreateUOW();
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
- 🔧 Pluggable and testable

---

Happy coding! 💙

