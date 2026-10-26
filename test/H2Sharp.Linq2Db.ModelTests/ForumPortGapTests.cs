/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB;
using LinqToDB.Data;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The forms of H2 2.5 the port does not read yet.
/// </summary>
/// <remarks>
///     <para>
///         Every test here is a form the reference takes: the reference was asked about each of
///         them, and it answered. The port does not read them yet, so the tests fail and are
///         marked as the gaps of the port they are. They are not weakened and not thrown away:
///         they are the signal that the port has not caught up with the reference.
///     </para>
///     <para>
///         The gaps are listed in <c>docs/DIVERGENCES.md</c>.
///     </para>
/// </remarks>
public class ForumPortGapTests
{
    [Fact]
    public void The_texts_are_joined_with_the_pipes()
    {
        using var db = ForumDatabase.Filled(nameof(The_texts_are_joined_with_the_pipes));

        string joined = db.Query<string>("SELECT 'a' || 'b'").First();

        Assert.Equal("ab", joined);
    }

    [Fact]
    public void The_moment_of_the_database_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_moment_of_the_database_is_asked_for));

        DateTime now = db.Query<DateTime>("SELECT CURRENT_TIMESTAMP").First();

        Assert.True(now > new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
    }

    [Fact]
    public void The_rows_are_asked_for_through_a_table_of_their_own()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_through_a_table_of_their_own));

        int count = db.Query<int>(
                """
                SELECT COUNT(*) FROM (
                    SELECT "Id" FROM "Topics"
                ) "T"
                """)
            .First();

        Assert.Equal(7, count);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_list_of_parameters()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_list_of_parameters));

        // The array is handed over as a DataParameter: an anonymous object drops array
        // properties from the command (a provider-side gap, tracked in DIVERGENCES),
        // while a DataParameter arrives and the engine reads = ANY($ids).
        List<int> ids = db.Query<int>(
                """
                SELECT "Id" FROM "Topics" WHERE "Id" = ANY($ids)
                """,
                new DataParameter("ids", new[] { 1, 2, 3 }))
            .ToList();

        Assert.Equal([1, 2, 3], ids);
    }
}
