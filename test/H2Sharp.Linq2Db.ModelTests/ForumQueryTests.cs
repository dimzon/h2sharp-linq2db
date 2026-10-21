/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;
using H2Sharp.Linq2Db.ModelTests.Seeding;

using LinqToDB;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The rows of the forum are asked for.
/// </summary>
/// <remarks>
///     <para>
///         Every test here starts from the same filled database, so that the answer of the
///         query is the same every time and can be written down.
///     </para>
///     <para>
///         The tests ask the questions an application asks: which topics are the newest, who
///         wrote the most, which forums are quiet, and so on.
///     </para>
///     <para>
///         The forms the engine does not read yet are in <see cref="ForumPortGapTests" />, where
///         they are marked as the gaps of the engine they are.
///     </para>
/// </remarks>
public class ForumQueryTests
{
    [Fact]
    public void All_the_topics_come_back()
    {
        using var db = ForumDatabase.Filled(nameof(All_the_topics_come_back));

        List<string> titles = db.GetTable<Topic>()
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(7, titles.Count);
        Assert.Equal("What a compiler does with the source", titles[0]);
        Assert.Equal("The forum moves", titles[6]);
    }

    [Fact]
    public void A_row_is_asked_for_by_its_key()
    {
        using var db = ForumDatabase.Filled(nameof(A_row_is_asked_for_by_its_key));

        int id = db.GetTable<Topic>().OrderBy(t => t.Id).Select(t => t.Id).First();

        Topic topic = db.GetTable<Topic>().Single(t => t.Id == id);

        Assert.Equal("What a compiler does with the source", topic.Title);
    }

    [Fact]
    public void The_rows_of_one_forum_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_of_one_forum_are_asked_for));

        int forumId = db.GetTable<Forum>().Single(f => f.Name == "Databases").Id;

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.ForumId == forumId)
            .OrderBy(t => t.Id)
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
    public void The_topics_are_asked_for_by_their_name()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_their_name));

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.Title.Contains("compiler"))
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(["What a compiler does with the source"], titles);
    }

    [Fact]
    public void The_topics_are_asked_for_by_the_beginning_of_their_name()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_the_beginning_of_their_name));

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.Title.StartsWith("The"))
            .OrderBy(t => t.Title)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "The cost of an abstraction",
                "The forum moves",
            ],
            titles);
    }

    [Fact]
    public void The_topics_are_asked_for_by_the_end_of_their_name()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_the_end_of_their_name));

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.Title.EndsWith("plan"))
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(["Reading a query plan"], titles);
    }

    [Fact]
    public void The_topics_are_ordered_by_the_number_of_the_views()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_ordered_by_the_number_of_the_views));

        List<(string Title, int Views)> topics = db.GetTable<Topic>()
            .OrderByDescending(t => t.ViewCount)
            .Select(t => new { t.Title, t.ViewCount })
            .ToList()
            .Select(t => (t.Title, t.ViewCount))
            .ToList();

        Assert.Equal("What a compiler does with the source", topics[0].Title);
        Assert.Equal(1200, topics[0].Views);
        Assert.Equal("The forum moves", topics[^1].Title);
        Assert.Equal(80, topics[^1].Views);
    }

    [Fact]
    public void The_topics_are_ordered_by_two_columns()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_ordered_by_two_columns));

        List<string> titles = db.GetTable<Topic>()
            .OrderBy(t => t.ForumId)
            .ThenByDescending(t => t.ViewCount)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "What a compiler does with the source",
                "Types without a runtime",
                "The cost of an abstraction",
                "Reading a query plan",
                "Transactions and the order of the writes",
                "Indexes that are never used",
                "The forum moves",
            ],
            titles);
    }

    [Fact]
    public void The_number_of_the_rows_is_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_of_the_rows_is_counted));

        Assert.Equal(7, db.GetTable<Topic>().Count());
        Assert.Equal(3, db.GetTable<Topic>().Count(t => t.ForumId == 1));
        Assert.Equal(14, db.GetTable<Post>().Count());
    }

    [Fact]
    public void The_sum_of_the_views_is_taken()
    {
        using var db = ForumDatabase.Filled(nameof(The_sum_of_the_views_is_taken));

        int sum = db.GetTable<Topic>().Sum(t => t.ViewCount);

        Assert.Equal(3680, sum);
    }

    [Fact]
    public void The_smallest_and_the_largest_of_the_views_are_taken()
    {
        using var db = ForumDatabase.Filled(nameof(The_smallest_and_the_largest_of_the_views_are_taken));

        Assert.Equal(80, db.GetTable<Topic>().Min(t => t.ViewCount));
        Assert.Equal(1200, db.GetTable<Topic>().Max(t => t.ViewCount));
    }

    [Fact]
    public void The_average_of_the_views_is_taken()
    {
        using var db = ForumDatabase.Filled(nameof(The_average_of_the_views_is_taken));

        double average = db.GetTable<Topic>().Average(t => t.ViewCount);

        Assert.Equal(3680d / 7d, average, 6);
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void It_is_asked_whether_there_is_a_row()
    {
        using var db = ForumDatabase.Filled(nameof(It_is_asked_whether_there_is_a_row));

        Assert.True(db.GetTable<Topic>().Any(t => t.ViewCount > 1000));
        Assert.False(db.GetTable<Topic>().Any(t => t.ViewCount > 5000));
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public void It_is_asked_whether_every_row_says_yes()
    {
        using var db = ForumDatabase.Filled(nameof(It_is_asked_whether_every_row_says_yes));

        Assert.True(db.GetTable<Topic>().All(t => t.ViewCount > 0));
        Assert.False(db.GetTable<Topic>().All(t => t.ViewCount > 100));
    }

    [Fact]
    public void The_rows_are_taken_one_by_one()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_taken_one_by_one));

        Topic first = db.GetTable<Topic>().OrderBy(t => t.Id).First();
        Topic last = db.GetTable<Topic>().OrderByDescending(t => t.Id).First();

        Assert.Equal("What a compiler does with the source", first.Title);
        Assert.Equal("The forum moves", last.Title);
    }

    [Fact]
    public void A_page_of_the_topics_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(A_page_of_the_topics_is_asked_for));

        List<string> titles = db.GetTable<Topic>()
            .OrderBy(t => t.Id)
            .Skip(2)
            .Take(3)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "The cost of an abstraction",
                "Reading a query plan",
                "Indexes that are never used",
            ],
            titles);
    }

    [Fact]
    public void The_rows_are_asked_for_by_a_list_of_keys()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_by_a_list_of_keys));

        List<int> ids = db.GetTable<Topic>()
            .Where(t => t.Title.Contains("The") || t.Title.Contains("Types"))
            .Select(t => t.Id)
            .ToList();

        List<string> titles = db.GetTable<Topic>()
            .Where(t => ids.Contains(t.Id))
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Types without a runtime",
                "The cost of an abstraction",
                "The forum moves",
            ],
            titles);
    }

    [Fact]
    public void The_rows_are_asked_for_by_a_number_between_two()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_by_a_number_between_two));

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.ViewCount >= 150 && t.ViewCount <= 890)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Types without a runtime",
                "The cost of an abstraction",
                "Reading a query plan",
                "Indexes that are never used",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_rows_with_a_null_in_them_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_with_a_null_in_them_are_asked_for));

        List<int> users = db.GetTable<User>()
            .Where(u => u.LastSeenAt == null)
            .Select(u => u.Id)
            .ToList();

        Assert.Empty(users);

        List<int> withoutABirthday = db.GetTable<UserProfile>()
            .Where(p => p.BirthDate == null)
            .Select(p => p.UserId)
            .ToList();

        Assert.Equal(2, withoutABirthday.Count);
    }

    [Fact]
    public void The_repeated_names_are_taken_once()
    {
        using var db = ForumDatabase.Filled(nameof(The_repeated_names_are_taken_once));

        List<int> authors = db.GetTable<Post>()
            .Select(p => p.AuthorId)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        Assert.Equal([1, 2, 3, 4], authors);
    }

    [Fact]
    public void The_topics_of_a_forum_are_asked_for_with_a_join()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_of_a_forum_are_asked_for_with_a_join));

        List<string> titles = (from topic in db.GetTable<Topic>()
                               join forum in db.GetTable<Forum>() on topic.ForumId equals forum.Id
                               where forum.Name == "Databases"
                               orderby topic.Id
                               select topic.Title).ToList();

        Assert.Equal(
            [
                "Reading a query plan",
                "Indexes that are never used",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_name_of_the_author_is_asked_for_with_a_join()
    {
        using var db = ForumDatabase.Filled(nameof(The_name_of_the_author_is_asked_for_with_a_join));

        List<(string Title, string Author)> topics = (from topic in db.GetTable<Topic>()
                                                     join user in db.GetTable<User>() on topic.AuthorId equals user.Id
                                                     orderby topic.Id
                                                     select new { topic.Title, Author = user.UserName })
            .ToList()
            .Select(t => (t.Title, t.Author))
            .ToList();

        Assert.Equal("ada", topics[0].Author);
        Assert.Equal("grace", topics[1].Author);
        Assert.Equal("linus", topics[2].Author);
    }

    [Fact]
    public void The_topics_are_asked_for_by_the_name_of_the_author()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_the_name_of_the_author));

        List<string> titles = (from topic in db.GetTable<Topic>()
                               join user in db.GetTable<User>() on topic.AuthorId equals user.Id
                               where user.UserName == "ada"
                               orderby topic.Id
                               select topic.Title).ToList();

        Assert.Equal(
            [
                "What a compiler does with the source",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_answers_of_a_topic_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_answers_of_a_topic_are_asked_for));

        int topicId = db.GetTable<Topic>().Single(t => t.Title == "Reading a query plan").Id;

        List<string> bodies = db.GetTable<Post>()
            .Where(p => p.TopicId == topicId)
            .OrderBy(p => p.Id)
            .Select(p => p.Body)
            .ToList();

        Assert.Equal(
            [
                "The plan is read from the leaves.",
                "The order of the nodes is the order of the work.",
            ],
            bodies);
    }

    [Fact]
    public void The_tags_of_a_topic_are_asked_for_with_two_joins()
    {
        using var db = ForumDatabase.Filled(nameof(The_tags_of_a_topic_are_asked_for_with_two_joins));

        int topicId = db.GetTable<Topic>().Single(t => t.Title == "What a compiler does with the source").Id;

        List<string> names = (from topicTag in db.GetTable<TopicTag>()
                              join tag in db.GetTable<Tag>() on topicTag.TagId equals tag.Id
                              where topicTag.TopicId == topicId
                              orderby tag.Name
                              select tag.Name).ToList();

        Assert.Equal(["compilers", "types"], names);
    }

    [Fact]
    public void The_number_of_the_answers_of_every_topic_is_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_of_the_answers_of_every_topic_is_counted));

        List<(int TopicId, int Answers)> counts = (from post in db.GetTable<Post>()
                                                   group post by post.TopicId into answers
                                                   orderby answers.Key
                                                   select new { TopicId = answers.Key, Answers = answers.Count() })
            .ToList()
            .Select(c => (c.TopicId, c.Answers))
            .ToList();

        Assert.Equal(7, counts.Count);
        Assert.Equal(4, counts[0].Answers);
        Assert.Equal(2, counts[1].Answers);
        Assert.Equal(1, counts[2].Answers);
    }

    [Fact]
    public void The_topics_of_a_forum_are_asked_for_with_a_subquery()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_of_a_forum_are_asked_for_with_a_subquery));

        int forumId = db.GetTable<Forum>().Single(f => f.Name == "Programming").Id;

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.ForumId == forumId)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
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
    [Trait("Category", "BlockedByPort")]
    public void The_topics_that_have_answers_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_that_have_answers_are_asked_for));

        List<string> titles = db.GetTable<Topic>()
            .Where(t => db.GetTable<Post>().Any(p => p.TopicId == t.Id))
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(7, titles.Count);
    }

    [Fact]
    public void The_state_of_a_topic_is_asked_for_with_a_join()
    {
        using var db = ForumDatabase.Filled(nameof(The_state_of_a_topic_is_asked_for_with_a_join));

        List<string> notes = (from topic in db.GetTable<Topic>()
                              join state in db.GetTable<TopicState>() on topic.Id equals state.TopicId
                              where state.IsClosed
                              orderby topic.Id
                              select state.ModeratorNote!).ToList();

        Assert.Equal(
            [
                "Locked: the thread turned into a flame war.",
                "The announcement is kept for the history.",
            ],
            notes);
    }

    [Fact]
    public void The_pinned_topics_are_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_pinned_topics_are_asked_for));

        List<string> titles = (from topic in db.GetTable<Topic>()
                               join state in db.GetTable<TopicState>() on topic.Id equals state.TopicId
                               where state.IsPinned
                               select topic.Title).ToList();

        Assert.Equal(["What a compiler does with the source"], titles);
    }

    [Fact]
    public void The_people_of_the_forum_are_asked_for_with_their_profile()
    {
        using var db = ForumDatabase.Filled(nameof(The_people_of_the_forum_are_asked_for_with_their_profile));

        List<(string Name, decimal Balance)> people = (from user in db.GetTable<User>()
                                                       join profile in db.GetTable<UserProfile>() on user.Id equals profile.UserId
                                                       where user.Role == UserRole.Administrator
                                                       select new { user.UserName, profile.Balance })
            .ToList()
            .Select(p => (p.UserName, p.Balance))
            .ToList();

        Assert.Single(people);
        Assert.Equal("ada", people[0].Name);
        Assert.Equal(12.50m, people[0].Balance);
    }

    [Fact]
    public void The_votes_of_an_answer_are_counted()
    {
        using var db = ForumDatabase.Filled(nameof(The_votes_of_an_answer_are_counted));

        int postId = db.GetTable<Post>().Single(p => p.Body == "The tree is walked and the code is written.").Id;

        int votes = db.GetTable<Vote>().Count(v => v.PostId == postId);

        Assert.Equal(2, votes);
    }

    [Fact]
    public void The_rows_are_asked_for_with_a_condition_over_a_date()
    {
        using var db = ForumDatabase.Filled(nameof(The_rows_are_asked_for_with_a_condition_over_a_date));

        DateTime begin = ForumData.Start.AddDays(15);
        DateTime end = ForumData.Start.AddDays(45);

        List<string> titles = db.GetTable<Topic>()
            .Where(t => t.CreatedAt >= begin && t.CreatedAt < end)
            .OrderBy(t => t.Id)
            .Select(t => t.Title)
            .ToList();

        Assert.Equal(
            [
                "Types without a runtime",
                "The cost of an abstraction",
                "Reading a query plan",
                "Transactions and the order of the writes",
            ],
            titles);
    }

    [Fact]
    public void The_topics_are_asked_for_by_the_year_of_their_creation()
    {
        using var db = ForumDatabase.Filled(nameof(The_topics_are_asked_for_by_the_year_of_their_creation));

        int topics = db.GetTable<Topic>()
            .Count(t => t.CreatedAt.Year == 2024);

        Assert.Equal(7, topics);
    }

    [Fact]
    public void The_text_of_a_topic_is_asked_for()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_of_a_topic_is_asked_for));

        List<string> bodies = db.GetTable<Topic>()
            .Where(t => t.Body.StartsWith("A walk"))
            .Select(t => t.Body)
            .ToList();

        Assert.Equal(["A walk through the phases, from the characters to the code."], bodies);
    }
}
