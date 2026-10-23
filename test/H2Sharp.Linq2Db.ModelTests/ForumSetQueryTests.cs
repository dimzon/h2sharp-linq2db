/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;
using H2Sharp.Linq2Db.ModelTests.Seeding;

using LinqToDB;
using LinqToDB.DataProvider.H2Sharp;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The rows of the forum are gathered into groups and joined with one another.
/// </summary>
/// <remarks>
///     The questions are the ones a forum asks about itself: how many answers each topic has,
///     who wrote the most, which forum is the busiest.
/// </remarks>
public class ForumSetQueryTests
{
    [Fact]
    public void The_answers_of_every_topic_are_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_answers_of_every_topic_are_counted));

        List<int> counts = (from post in db.Posts()
                             group post by post.TopicId into answers
                             orderby answers.Key
                             select answers.Count()).ToList();

        Assert.Equal([4, 2, 1, 2, 2, 2, 1], counts);
    }

    [Fact]
    public void The_newest_topics_come_from_a_common_table()
    {
        using var db = ForumDatabase.Filled(nameof(The_newest_topics_come_from_a_common_table));

        var newest = db.Topics().OrderByDescending(t => t.Id).Take(3).AsCte("Newest");
        List<string> titles = (from t in newest orderby t.Id select t.Title).ToList();

        Assert.Equal(
        [
            "Indexes that are never used",
            "Transactions and the order of the writes",
            "The forum moves",
        ],
        titles);
    }

    [Fact]
    public void A_join_per_row_is_refused_without_lateral()
    {
        using var db = ForumDatabase.Filled(nameof(A_join_per_row_is_refused_without_lateral));

        // H2 2.5 has no LATERAL and no CROSS APPLY: the argument of a table function
        // cannot depend on the row of an outer query.
        var refused = Assert.Throws<NotSupportedException>(
            () => db.Topics()
                .SelectMany(t => db.SystemRange(1, t.ViewCount))
                .ToList());

        Assert.Contains("LATERAL", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_topics_of_every_forum_are_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_of_every_forum_are_counted));

        List<int> counts = (from topic in db.Topics()
                             group topic by topic.ForumId into topics
                             orderby topics.Key
                             select topics.Count()).ToList();

        Assert.Equal([3, 3, 1], counts);
    }

    [Fact]
    public void The_views_of_the_topics_of_every_forum_are_summed_up()
    {
        using var db = ForumDatabase.Filled(nameof(The_views_of_the_topics_of_every_forum_are_summed_up));

        List<int> sums = (from topic in db.Topics()
                           group topic by topic.ForumId into topics
                           orderby topics.Key
                           select topics.Sum(t => t.ViewCount)).ToList();

        Assert.Equal([2140, 1460, 80], sums);
    }

    [Fact]
    public void The_names_of_the_authors_are_taken_once()
    {
        using var db = ForumDatabase.Filled(nameof(The_names_of_the_authors_are_taken_once));

        List<string> names = (from post in db.Posts()
                              join user in db.Users() on post.AuthorId equals user.Id
                              group post by user.UserName into posts
                              orderby posts.Key
                              select posts.Key).ToList();

        Assert.Equal(["ada", "grace", "linus", "margaret"], names);
    }

    [Fact]
    public void The_number_of_the_answers_of_every_author_is_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_of_the_answers_of_every_author_is_counted));

        List<int> counts = (from post in db.Posts()
                             group post by post.AuthorId into posts
                             orderby posts.Key
                             select posts.Count()).ToList();

        Assert.Equal([4, 4, 4, 2], counts);
    }

    [Fact]
    public void The_most_watched_topic_of_every_forum_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_most_watched_topic_of_every_forum_is_asked_for));

        List<int> views = (from topic in db.Topics()
                           group topic by topic.ForumId into topics
                           orderby topics.Key
                           select topics.Max(t => t.ViewCount)).ToList();

        Assert.Equal([1200, 890, 80], views);
    }

    [Fact]
    public void The_answers_are_counted_with_the_name_of_the_author()
    {
        using var db = ForumDatabase.Filled(nameof(The_answers_are_counted_with_the_name_of_the_author));

        List<int> counts = (from post in db.Posts()
                             join user in db.Users() on post.AuthorId equals user.Id
                             group post by user.UserName into posts
                             orderby posts.Key
                             select posts.Count()).ToList();

        Assert.Equal([4, 4, 4, 2], counts);
    }

    [Fact]
    public void The_topics_are_asked_for_with_the_number_of_their_answers()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_asked_for_with_the_number_of_their_answers));

        List<int> counts = (from topic in db.Topics()
                             join post in db.Posts() on topic.Id equals post.TopicId
                             group topic by topic.Id into posts
                             orderby posts.Key
                             select posts.Count()).ToList();

        Assert.Equal([4, 2, 1, 2, 2, 2, 1], counts);
    }

    [Fact]
    public void The_tags_of_every_topic_are_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_tags_of_every_topic_are_counted));

        List<int> counts = (from topicTag in db.TopicTags()
                             group topicTag by topicTag.TopicId into tags
                             orderby tags.Key
                             select tags.Count()).ToList();

        Assert.Equal([2, 1, 1, 1, 1, 1], counts);
    }

    [Fact]
    public void The_answers_of_a_forum_are_counted_with_a_join()
    {
        using var db = ForumDatabase.Filled(nameof(The_answers_of_a_forum_are_counted_with_a_join));

        int forumId = db.Forums().Single(f => f.Name == "Programming").Id;

        int answers = (from post in db.Posts()
                       join topic in db.Topics() on post.TopicId equals topic.Id
                       where topic.ForumId == forumId
                       select post.Id).Count();

        Assert.Equal(7, answers);
    }

    [Fact]
    public void The_topics_of_a_forum_are_asked_for_with_a_left_join()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_of_a_forum_are_asked_for_with_a_left_join));

        int forumId = db.Forums().Single(f => f.Name == "Old announcements").Id;

        List<string?> titles = (from topic in db.Topics()
                                 join state in db.TopicStates() on topic.Id equals state.TopicId
                                 where topic.ForumId == forumId
                                 orderby topic.Id
                                 select state.ModeratorNote).ToList();

        Assert.Equal(["The announcement is kept for the history."], titles);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_authors_in_both_topics_are_met()
    {
        using var db = ForumDatabase.Filled(nameof(The_authors_in_both_topics_are_met));

        // INTERSECT is not read by the port yet (engine issue dimzon/h2sharp#33):
        // the reference answers [1, 2].
        List<int> met = db.Posts().Where(p => p.TopicId == 1).Select(p => p.AuthorId)
            .Intersect(db.Posts().Where(p => p.TopicId == 2).Select(p => p.AuthorId))
            .OrderBy(a => a)
            .ToList();

        Assert.Equal([1, 2], met);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_authors_in_only_one_topic_are_set_apart()
    {
        using var db = ForumDatabase.Filled(nameof(The_authors_in_only_one_topic_are_set_apart));

        // EXCEPT is not read by the port yet (engine issue dimzon/h2sharp#33):
        // the reference answers [3, 4].
        List<int> apart = db.Posts().Where(p => p.TopicId == 1).Select(p => p.AuthorId)
            .Except(db.Posts().Where(p => p.TopicId == 2).Select(p => p.AuthorId))
            .OrderBy(a => a)
            .ToList();

        Assert.Equal([3, 4], apart);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void The_answers_of_one_topic_are_gathered_in_order()
    {
        using var db = ForumDatabase.Filled(nameof(The_answers_of_one_topic_are_gathered_in_order));

        // WITHIN GROUP in LISTAGG is not read by the port yet (engine issue dimzon/h2sharp#35):
        // the reference answers the bodies in the requested order.
        string gathered = db.Posts()
            .Where(p => p.TopicId == 1)
            .Select(p => Sql.Ext.ListAgg(p.Body, ", ").WithinGroup.OrderBy(p.Id).ToValue())
            .First();

        Assert.Equal(
            string.Join(", ", ForumData.Posts().Where(p => p.TopicId == 1).OrderBy(p => p.Id).Select(p => p.Body)),
            gathered);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_group_and_an_order()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_group_and_an_order));

        List<(int ForumId, int Topics)> groups = (from topic in db.Topics()
                                                  group topic by topic.ForumId into topics
                                                  orderby topics.Count() descending
                                                  select new { ForumId = topics.Key, Topics = topics.Count() })
            .ToList()
            .Select(g => (g.ForumId, g.Topics))
            .ToList();

        Assert.Equal(3, groups.Count);
        Assert.Equal(3, groups[0].Topics);
        Assert.Equal(1, groups[2].Topics);
    }
}
