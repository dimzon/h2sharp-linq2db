/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Seeding;
using LinqToDB;
using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

public class ForumAggregateWindowTests
{
    [Sql.Extension("COUNT(*) FILTER (WHERE {filter})")]
    private static int CountWhere([ExprParameter("filter")] bool filter)
        => throw new ServerSideOnlyException(nameof(CountWhere));

    [Fact]
    public void The_hidden_answers_are_counted_with_a_filter()
    {
        using var db = ForumDatabase.Filled(nameof(The_hidden_answers_are_counted_with_a_filter));

        // FILTER (WHERE ...) is silently ignored by the port (engine issue dimzon/h2sharp#38):
        // COUNT(*) FILTER (...) answers all rows instead of the filtered ones.
        int hidden = db.Posts()
            .Select(p => CountWhere(p.IsHidden))
            .First();

        Assert.Equal(ForumData.Posts().Count(p => p.IsHidden), hidden);
    }
}
