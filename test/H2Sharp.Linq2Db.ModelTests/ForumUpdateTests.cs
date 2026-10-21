/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The rows of the forum are changed and thrown away.
/// </summary>
/// <remarks>
///     The changes are read back from the database, so that a change that was only made in the
///     memory of the provider does not pass as a change that was made.
/// </remarks>
public class ForumUpdateTests
{
    [Fact]
    public void A_row_is_changed()
    {
        using var db = ForumDatabase.Filled(nameof(A_row_is_changed));

        Topic topic = db.GetTable<Topic>().OrderBy(t => t.Id).First();
        topic.ViewCount = 4242;

        db.Update(topic);

        int views = db.GetTable<Topic>().Where(t => t.Id == topic.Id).Select(t => t.ViewCount).First();
        Assert.Equal(4242, views);
    }

    [Fact]
    public void A_row_is_changed_by_its_key()
    {
        using var db = ForumDatabase.Filled(nameof(A_row_is_changed_by_its_key));

        Topic topic = db.GetTable<Topic>().Single(t => t.Title == "Types without a runtime");
        topic.ViewCount = 1;

        db.Update(topic);

        int views = db.GetTable<Topic>().Where(t => t.Id == topic.Id).Select(t => t.ViewCount).First();
        Assert.Equal(1, views);
    }

    [Fact]
    public void A_text_is_changed()
    {
        using var db = ForumDatabase.Filled(nameof(A_text_is_changed));

        Topic topic = db.GetTable<Topic>().OrderBy(t => t.Id).First();
        topic.Title = "What a compiler does with the source, revised";

        db.Update(topic);

        string title = db.GetTable<Topic>().Where(t => t.Id == topic.Id).Select(t => t.Title).First();
        Assert.Equal("What a compiler does with the source, revised", title);
    }

    [Fact]
    public void The_name_of_a_person_is_changed()
    {
        using var db = ForumDatabase.Filled(nameof(The_name_of_a_person_is_changed));

        User user = db.GetTable<User>().Single(u => u.UserName == "linus");
        user.UserName = "linus-t";

        db.Update(user);

        Assert.Equal("linus-t", db.GetTable<User>().Single(u => u.Id == user.Id).UserName);
    }

    [Fact]
    public void Many_rows_are_changed()
    {
        using var db = ForumDatabase.Filled(nameof(Many_rows_are_changed));

        List<Topic> topics = db.GetTable<Topic>().Where(t => t.ForumId == 1).ToList();

        foreach (Topic topic in topics)
        {
            topic.ViewCount = 0;
            db.Update(topic);
        }

        int changed = db.GetTable<Topic>().Count(t => t.ViewCount == 0);

        Assert.Equal(3, changed);
    }

    [Fact]
    public void A_row_is_thrown_away()
    {
        using var db = ForumDatabase.Filled(nameof(A_row_is_thrown_away));

        Topic topic = db.GetTable<Topic>().OrderBy(t => t.Id).First();

        db.Delete(topic);

        Assert.Equal(6, db.GetTable<Topic>().Count());
        Assert.Equal(0, db.GetTable<Topic>().Count(t => t.Id == topic.Id));
    }

    [Fact]
    public void Many_rows_are_thrown_away()
    {
        using var db = ForumDatabase.Filled(nameof(Many_rows_are_thrown_away));

        db.GetTable<Post>().Where(p => p.TopicId == 1).Delete();

        Assert.Equal(10, db.GetTable<Post>().Count());
    }

    [Fact]
    public void A_row_is_put_in()
    {
        using var db = ForumDatabase.Filled(nameof(A_row_is_put_in));

        var tag = new Tag { Name = "port" };

        int key = Convert.ToInt32(db.InsertWithIdentity(tag));

        Assert.Equal(5, db.GetTable<Tag>().Count());
        Assert.Equal("port", db.GetTable<Tag>().Single(t => t.Id == key).Name);
    }

    [Fact]
    public void A_row_with_a_key_of_its_own_is_put_in()
    {
        using var db = ForumDatabase.Filled(nameof(A_row_with_a_key_of_its_own_is_put_in));

        var profile = new UserProfile { UserId = 99, Reputation = 5, Balance = 1.25m };

        db.Insert(profile);

        Assert.Equal(99, db.GetTable<UserProfile>().Single(p => p.UserId == 99).UserId);
        Assert.Equal(6, db.GetTable<UserProfile>().Count());
    }

    [Fact]
    public void The_rows_are_put_in_one_by_one()
    {
        using var db = ForumDatabase.Empty(nameof(The_rows_are_put_in_one_by_one));

        db.Insert(new Tag { Name = "one" });
        db.Insert(new Tag { Name = "two" });
        db.Insert(new Tag { Name = "three" });

        Assert.Equal(3, db.GetTable<Tag>().Count());
        Assert.Equal(["one", "three", "two"], db.GetTable<Tag>().Select(t => t.Name).OrderBy(n => n).ToList());
    }
}
