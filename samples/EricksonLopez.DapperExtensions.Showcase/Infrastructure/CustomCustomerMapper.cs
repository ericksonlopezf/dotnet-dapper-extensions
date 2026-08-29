// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Globalization;
using EricksonLopez.DapperExtensions.MultiMap;
using EricksonLopez.DapperExtensions.Showcase.Models;

namespace EricksonLopez.DapperExtensions.Showcase.Infrastructure;

/// <summary>
/// Custom manual IDataReaderMapper implementation for AOT hydration.
/// </summary>
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
