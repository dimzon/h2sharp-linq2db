/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;
using LinqToDB.Data;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The rows of the forum are asked for with a statement of one's own.
/// </summary>
/// <remarks>
///     The statement is written the way H2 2.5 takes it, and the reference was asked about the
///     forms that are not obvious.
/// </remarks>
public class ForumSqlQueryTests
{
    [Fact]
    public void The_rows_are_asked_for_with_a_statement()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_statement));

        List<Topic> topics = db.Query<Topic>(
                """
                SELECT "Id", "ForumId", "AuthorId", "Title", "Body", "CreatedAt", "LastPostAt", "ViewCount"
                FROM "Topics"
                ORDER BY "Id"
                """)
            .ToList();

        Assert.Equal(7, topics.Count);
        Assert.Equal("What a compiler does with the source", topics[0].Title);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_statement_and_a_parameter()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_statement_and_a_parameter));

        int forumId = db.Forums().Single(f => f.Name == "Databases").Id;

        List<string> titles = db.Query<Topic>(
                """
                SELECT "Id", "ForumId", "AuthorId", "Title", "Body", "CreatedAt", "LastPostAt", "ViewCount"
                FROM "Topics"
                WHERE "ForumId" = @forumId
                ORDER BY "Id"
                """,
                new { forumId })
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Reading a query plan",
                "Indexes that are never used",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_number_of_the_rows_is_asked_for_with_a_statement()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_asked_for_with_a_statement));

        int count = db.Query<int>("SELECT COUNT(*) FROM \"Topics\"").First();

        Assert.Equal(7, count);
    }

    [Fact]
    public void The_rows_of_two_tables_are_asked_for_with_a_statement()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_of_two_tables_are_asked_for_with_a_statement));

        List<string> titles = db.Query<string>(
                """
                SELECT "t"."Title"
                FROM "Topics" "t"
                INNER JOIN "Forums" "f" ON "t"."ForumId" = "f"."Id"
                WHERE "f"."Name" = @name
                ORDER BY "t"."Id"
                """,
                new { name = "Programming" })
            .ToList();

        Assert.Equal(
            [
                "What a compiler does with the source",
                "Types without a runtime",
                "The cost of an abstraction",
            ],
            titles);
    }

    [Fact]
    public void The_rows_are_counted_with_a_statement_and_a_group()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_counted_with_a_statement_and_a_group));

        List<int> counts = db.Query<int>(
                """
                SELECT COUNT(*)
                FROM "Posts"
                GROUP BY "TopicId"
                ORDER BY "TopicId"
                """)
            .ToList();

        Assert.Equal([4, 2, 1, 2, 2, 2, 1], counts);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_statement_and_a_condition_over_a_text()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_statement_and_a_condition_over_a_text));

        List<string> titles = db.Query<string>(
                """
                SELECT "Title"
                FROM "Topics"
                WHERE "Title" LIKE @pattern
                ORDER BY "Id"
                """,
                new { pattern = "%compiler%" })
            .ToList();

        Assert.Equal(["What a compiler does with the source"], titles);
    }
}
