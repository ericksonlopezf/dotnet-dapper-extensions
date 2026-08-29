// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data.Common;
using AwesomeAssertions;
using EricksonLopez.DapperExtensions.Resilience;
using EricksonLopez.DapperExtensions.Testing.Common;
using Xunit;

namespace EricksonLopez.DapperExtensions.Resilience.UnitTests;

public class SqlServerTransientErrorDetectorTests
{
    private readonly SqlServerTransientErrorDetector _sut = SqlServerTransientErrorDetector.Default;

    [Fact]
    public void IsTransient_Null_ReturnsFalse()
    {
        _sut.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_EmptyOrWhitespaceMessage_ReturnsFalse()
    {
        _sut.IsTransient(new Exception("")).Should().BeFalse();
        _sut.IsTransient(new Exception("   ")).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestDbException("Generic DB error", isTransient: true);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData(1205)]
    [InlineData(1222)]
    [InlineData(233)]
    [InlineData(64)]
    [InlineData(4060)]
    [InlineData(40143)]
    [InlineData(40197)]
    [InlineData(40501)]
    [InlineData(40613)]
    [InlineData(49918)]
    [InlineData(10928)]
    [InlineData(10929)]
    [InlineData(10053)]
    [InlineData(10054)]
    [InlineData(10060)]
    public void IsTransient_DbException_WithTransientErrorCode_ReturnsTrue(int errorCode)
    {
        var ex = new TestDbException("Error", errorCode: errorCode);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataNumber_ReturnsTrue()
    {
        var ex = new TestDbException("Error", errorCode: 99999);
        ex.Data["Number"] = 1205;
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataNumber_NonTransient_ReturnsFalse()
    {
        var ex = new TestDbException("syntax error near SELECT", errorCode: 99999);
        ex.Data["Number"] = 88888;
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithDataNumber_NonIntType_ReturnsFalse()
    {
        var ex = new TestDbException("syntax error near SELECT", errorCode: 99999);
        ex.Data["Number"] = "not_an_int";
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_InnerDbException_TraversesChainAndReturnsTrue()
    {
        var inner = new TestDbException("Db error", errorCode: 1205);
        var outer = new Exception("Outer wrapper", inner);
        _sut.IsTransient(outer).Should().BeTrue();
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("connection")]
    [InlineData("deadlock")]
    [InlineData("transient")]
    [InlineData("deadlock detected")]
    [InlineData("operation timeout occurred")]
    [InlineData("connection reset by peer")]
    [InlineData("transient failure has occurred")]
    public void IsTransient_MessageContainsTransientKeyword_ReturnsTrue(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("constraint violation")]
    [InlineData("syntax error near SELECT")]
    [InlineData("duplicate key value violates unique constraint")]
    public void IsTransient_PermanentError_ReturnsFalse(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void Default_ReturnsSingleton()
    {
        SqlServerTransientErrorDetector.Default.Should().BeSameAs(SqlServerTransientErrorDetector.Default);
    }
}

public class PostgreSqlTransientErrorDetectorTests
{
    private readonly PostgreSqlTransientErrorDetector _sut = PostgreSqlTransientErrorDetector.Default;

    [Fact]
    public void IsTransient_Null_ReturnsFalse()
    {
        _sut.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_EmptyOrWhitespaceMessage_ReturnsFalse()
    {
        _sut.IsTransient(new Exception("")).Should().BeFalse();
        _sut.IsTransient(new Exception("   ")).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestDbException("Generic DB error", isTransient: true);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("40001")]
    [InlineData("40P01")]
    [InlineData("08006")]
    [InlineData("08001")]
    [InlineData("08004")]
    [InlineData("57P01")]
    [InlineData("57P02")]
    [InlineData("57P03")]
    [InlineData("53300")]
    [InlineData("53400")]
    public void IsTransient_DbException_WithSqlState_ReturnsTrue(string sqlState)
    {
        var ex = new TestDbException("Error", sqlState: sqlState);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithSqlState_NonTransient_ReturnsFalse()
    {
        var ex = new TestDbException("syntax error at or near SELECT", sqlState: "42601");
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithDataSqlState_ReturnsTrue()
    {
        var ex = new TestDbException("Error", sqlState: "UNKNOWN");
        ex.Data["SqlState"] = "40001";
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataSqlState_NonTransient_ReturnsFalse()
    {
        var ex = new TestDbException("syntax error at or near SELECT", sqlState: "UNKNOWN");
        ex.Data["SqlState"] = "42601";
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithDataSqlState_NonStringType_ReturnsFalse()
    {
        var ex = new TestDbException("syntax error at or near SELECT", sqlState: "UNKNOWN");
        ex.Data["SqlState"] = 12345;
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithNullSqlState_FallsThrough()
    {
        var ex = new TestDbException("syntax error at or near SELECT", sqlState: null!);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_InnerDbException_TraversesChainAndReturnsTrue()
    {
        var inner = new TestDbException("Db error", sqlState: "40P01");
        var outer = new Exception("Outer wrapper", inner);
        _sut.IsTransient(outer).Should().BeTrue();
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("connection")]
    [InlineData("deadlock")]
    [InlineData("serialization")]
    [InlineData("serialization failure on concurrent update")]
    [InlineData("deadlock found between transactions")]
    [InlineData("connection failure to server")]
    [InlineData("operation timeout occurred")]
    public void IsTransient_MessageContainsTransientKeyword_ReturnsTrue(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("duplicate key value violates unique constraint")]
    [InlineData("column does not exist")]
    [InlineData("null value in column violates not-null constraint")]
    public void IsTransient_PermanentError_ReturnsFalse(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void Default_ReturnsSingleton()
    {
        PostgreSqlTransientErrorDetector.Default.Should().BeSameAs(PostgreSqlTransientErrorDetector.Default);
    }
}

public class MySqlTransientErrorDetectorTests
{
    private readonly MySqlTransientErrorDetector _sut = MySqlTransientErrorDetector.Default;

    [Fact]
    public void IsTransient_Null_ReturnsFalse()
    {
        _sut.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_EmptyOrWhitespaceMessage_ReturnsFalse()
    {
        _sut.IsTransient(new Exception("")).Should().BeFalse();
        _sut.IsTransient(new Exception("   ")).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestDbException("Generic DB error", isTransient: true);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData(1213)]
    [InlineData(1205)]
    [InlineData(2006)]
    [InlineData(2013)]
    [InlineData(1158)]
    [InlineData(1159)]
    [InlineData(1160)]
    [InlineData(1161)]
    [InlineData(3024)]
    public void IsTransient_DbException_WithTransientErrorCode_ReturnsTrue(int errorCode)
    {
        var ex = new TestDbException("Error", errorCode: errorCode);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataServerErrorStatus_ReturnsTrue()
    {
        var ex = new TestDbException("Error", errorCode: 99999);
        ex.Data["ServerErrorStatus"] = 1213;
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataServerErrorStatus_NonTransient_ReturnsFalse()
    {
        var ex = new TestDbException("Table 'users' doesn't exist", errorCode: 99999);
        ex.Data["ServerErrorStatus"] = 88888;
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithDataServerErrorStatus_NonIntType_ReturnsFalse()
    {
        var ex = new TestDbException("Table 'users' doesn't exist", errorCode: 99999);
        ex.Data["ServerErrorStatus"] = "not_an_int";
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_InnerDbException_TraversesChainAndReturnsTrue()
    {
        var inner = new TestDbException("Db error", errorCode: 2006);
        var outer = new Exception("Outer wrapper", inner);
        _sut.IsTransient(outer).Should().BeTrue();
    }

    [Theory]
    [InlineData("deadlock")]
    [InlineData("server has gone away")]
    [InlineData("lost connection")]
    [InlineData("lock wait timeout")]
    [InlineData("deadlock found when trying to get lock")]
    [InlineData("MySQL server has gone away")]
    [InlineData("Lost connection to MySQL server")]
    [InlineData("lock wait timeout exceeded")]
    public void IsTransient_MessageContainsTransientKeyword_ReturnsTrue(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("Duplicate entry '123' for key 'PRIMARY'")]
    [InlineData("Column 'name' cannot be null")]
    [InlineData("Table 'users' doesn't exist")]
    public void IsTransient_PermanentError_ReturnsFalse(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void Default_ReturnsSingleton()
    {
        MySqlTransientErrorDetector.Default.Should().BeSameAs(MySqlTransientErrorDetector.Default);
    }
}

public class SqliteTransientErrorDetectorTests
{
    private readonly SqliteTransientErrorDetector _sut = SqliteTransientErrorDetector.Default;

    [Fact]
    public void IsTransient_Null_ReturnsFalse()
    {
        _sut.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_EmptyOrWhitespaceMessage_ReturnsFalse()
    {
        _sut.IsTransient(new Exception("")).Should().BeFalse();
        _sut.IsTransient(new Exception("   ")).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestDbException("Generic DB error", isTransient: true);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(261)]
    [InlineData(262)]
    public void IsTransient_DbException_WithTransientErrorCode_ReturnsTrue(int errorCode)
    {
        var ex = new TestDbException("Error", errorCode: errorCode);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataSqliteErrorCode_ReturnsTrue()
    {
        var ex = new TestDbException("Error", errorCode: 99999);
        ex.Data["SqliteErrorCode"] = 5;
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_WithDataSqliteErrorCode_NonTransient_ReturnsFalse()
    {
        var ex = new TestDbException("UNIQUE constraint failed: users.id", errorCode: 99999);
        ex.Data["SqliteErrorCode"] = 19;
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithDataSqliteErrorCode_NonIntType_ReturnsFalse()
    {
        var ex = new TestDbException("UNIQUE constraint failed: users.id", errorCode: 99999);
        ex.Data["SqliteErrorCode"] = "not_an_int";
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_InnerDbException_TraversesChainAndReturnsTrue()
    {
        var inner = new TestDbException("Db error", errorCode: 5);
        var outer = new Exception("Outer wrapper", inner);
        _sut.IsTransient(outer).Should().BeTrue();
    }

    [Theory]
    [InlineData("database is locked")]
    [InlineData("unable to open database")]
    [InlineData("disk I/O error")]
    [InlineData("database disk image is malformed")]
    [InlineData("SQLITE_BUSY error")]
    [InlineData("SQLITE_LOCKED error")]
    public void IsTransient_MessageContainsTransientKeyword_ReturnsTrue(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("syntax error near SELECT")]
    [InlineData("table users already exists")]
    [InlineData("UNIQUE constraint failed: users.id")]
    public void IsTransient_PermanentError_ReturnsFalse(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void Default_ReturnsSingleton()
    {
        SqliteTransientErrorDetector.Default.Should().BeSameAs(SqliteTransientErrorDetector.Default);
    }
}

public class OracleTransientErrorDetectorTests
{
    private readonly OracleTransientErrorDetector _sut = OracleTransientErrorDetector.Default;

    [Fact]
    public void IsTransient_Null_ReturnsFalse()
    {
        _sut.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_EmptyOrWhitespaceMessage_ReturnsFalse()
    {
        _sut.IsTransient(new Exception("")).Should().BeFalse();
        _sut.IsTransient(new Exception("   ")).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DbException_WithIsTransientTrue_ReturnsTrue()
    {
        var ex = new TestDbException("Generic DB error", isTransient: true);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData(60)]
    [InlineData(18)]
    [InlineData(54)]
    [InlineData(8177)]
    [InlineData(3113)]
    [InlineData(3114)]
    [InlineData(3135)]
    [InlineData(12170)]
    [InlineData(12541)]
    [InlineData(12560)]
    [InlineData(12571)]
    [InlineData(4031)]
    public void IsTransient_DbException_WithTransientErrorCode_ReturnsTrue(int errorCode)
    {
        var ex = new TestDbException("Error", errorCode: errorCode);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DbException_NonTransientErrorCode_ReturnsFalse()
    {
        var ex = new TestDbException("ORA-00942: table or view does not exist", errorCode: 942);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_InnerDbException_TraversesChainAndReturnsTrue()
    {
        var inner = new TestDbException("Db error", errorCode: 60);
        var outer = new Exception("Outer wrapper", inner);
        _sut.IsTransient(outer).Should().BeTrue();
    }

    [Theory]
    [InlineData("ORA-00060")]
    [InlineData("ORA-08177")]
    [InlineData("ORA-03113")]
    [InlineData("ORA-03114")]
    [InlineData("ORA-03135")]
    [InlineData("ORA-12170")]
    [InlineData("ORA-12541")]
    [InlineData("ORA-12560")]
    [InlineData("ORA-12571")]
    [InlineData("deadlock")]
    [InlineData("timeout")]
    [InlineData("connection lost")]
    [InlineData("end-of-file")]
    [InlineData("serialization failure")]
    [InlineData("ORA-00060 deadlock")]
    [InlineData("ORA-08177 serialization")]
    [InlineData("ORA-03113 end of file")]
    [InlineData("ORA-03114 not connected")]
    [InlineData("ORA-03135 connection lost")]
    [InlineData("ORA-12170 timeout")]
    [InlineData("ORA-12541 no listener")]
    [InlineData("ORA-12560 protocol error")]
    [InlineData("ORA-12571 packet writer failure")]
    [InlineData("deadlock detected")]
    [InlineData("connection lost contact")]
    [InlineData("end-of-file on channel")]
    [InlineData("connection timeout")]
    public void IsTransient_MessageContainsTransientKeyword_ReturnsTrue(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData("ORA-00001: unique constraint violated")]
    [InlineData("ORA-00942: table or view does not exist")]
    public void IsTransient_PermanentError_ReturnsFalse(string message)
    {
        var ex = new Exception(message);
        _sut.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void Default_ReturnsSingleton()
    {
        OracleTransientErrorDetector.Default.Should().BeSameAs(OracleTransientErrorDetector.Default);
    }
}
