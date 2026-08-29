# Level 08: Customization & Extensibility

## 1. Goal
Implement custom extensions for domain value objects (`TypeHandler`), custom transient error detectors (`ISqlTransientErrorDetector`), and manual Native AOT mappers (`IDataReaderMapper<T>`).

---

## 2. Custom Value Object Type Handler (`Money`)

```csharp
using System.Data;
using Dapper;

public readonly record struct Money(decimal Amount, string Currency)
{
    public override string ToString() => $"{Amount} {Currency}";
}

public sealed class MoneyTypeHandler : SqlMapper.TypeHandler<Money>
{
    public static readonly MoneyTypeHandler Default = new();

    public override void SetValue(IDbDataParameter parameter, Money value)
    {
        parameter.DbType = DbType.Decimal;
        parameter.Value = value.Amount;
    }

    public override Money Parse(object value)
    {
        var amount = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        return new Money(amount, "USD");
    }
}

// Registration
SqlMapper.AddTypeHandler(MoneyTypeHandler.Default);

var price = await connection.QuerySingleAsync<Money>("SELECT price FROM products WHERE id = 1;");
```

---

## 3. Custom Transient Error Detector
Implement `ISqlTransientErrorDetector` to handle bespoke cluster errors or cloud managed database messages:

```csharp
using EricksonLopez.DapperExtensions.Resilience;

public sealed class CustomClusterTransientErrorDetector : ISqlTransientErrorDetector
{
    public bool IsTransient(Exception exception)
    {
        if (exception == null) return false;

        var message = exception.Message;
        return message.Contains("node restart in progress", StringComparison.OrdinalIgnoreCase)
            || message.Contains("read replica lag exceeded", StringComparison.OrdinalIgnoreCase)
            || message.Contains("cluster topology changing", StringComparison.OrdinalIgnoreCase);
    }
}

// Construct Polly pipeline with the custom detector
var customPipeline = SqlResilienceDefaults.Standard(new CustomClusterTransientErrorDetector());
```

---

## 4. Custom Manual AOT Mapper (`IDataReaderMapper<T>`)
For scenarios where source generation is not used, implement `IDataReaderMapper<T>` manually:

```csharp
using System.Data;
using EricksonLopez.DapperExtensions.MultiMap;

public sealed class CustomCustomerMapper : IDataReaderMapper<Customer>
{
    public Customer Map(IDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        return new Customer
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            Tier = Enum.Parse<CustomerTier>(reader.GetString(reader.GetOrdinal("tier")), ignoreCase: true),
            RegisteredDate = DateOnly.Parse(reader.GetString(reader.GetOrdinal("registered_date")), CultureInfo.InvariantCulture)
        };
    }
}
```

---

## 5. Source Code Reference
- Executable Showcase: [`samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level08_Customization/CustomDetectorAndHandlerDemo.cs`](../../samples/EricksonLopez.DapperExtensions.Showcase/Levels/Level08_Customization/CustomDetectorAndHandlerDemo.cs)
