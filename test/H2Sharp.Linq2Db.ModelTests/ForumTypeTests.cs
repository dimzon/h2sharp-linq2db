/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using H2Sharp.Linq2Db.ModelTests.Model;

using LinqToDB;

using Xunit;

namespace H2Sharp.Linq2Db.ModelTests;

/// <summary>
///     The types of the provider: what goes into the database and what comes back.
/// </summary>
/// <remarks>
///     Every type the model carries is checked in both directions: the value that goes in comes
///     back unchanged.
/// </remarks>
public class ForumTypeTests
{
    [Fact]
    public void The_identity_key_is_given_out_by_the_database()
    {
        using var db = ForumDatabase.Empty(nameof(The_identity_key_is_given_out_by_the_database));

        var first = new Tag { Name = "one" };
        var second = new Tag { Name = "two" };

        int firstKey = Convert.ToInt32(db.InsertWithIdentity(first));
        int secondKey = Convert.ToInt32(db.InsertWithIdentity(second));

        Assert.Equal(1, firstKey);
        Assert.Equal(2, secondKey);
    }

    [Fact]
    public void The_enum_is_stored_as_a_number()
    {
        using var db = ForumDatabase.Filled(nameof(The_enum_is_stored_as_a_number));

        User user = db.Users().Single(u => u.UserName == "ada");

        Assert.Equal(UserRole.Administrator, user.Role);
    }

    [Fact]
    public void The_enum_is_stored_as_a_number_and_not_as_a_string()
    {
        using var db = ForumDatabase.Filled(nameof(The_enum_is_stored_as_a_number_and_not_as_a_string));

        List<UserRole> roles = db.Users()
            .OrderBy(u => u.Id)
            .Select(u => u.Role)
            .ToList();

        Assert.Equal(
            [
                UserRole.Administrator,
                UserRole.Moderator,
                UserRole.Member,
                UserRole.Member,
                UserRole.Guest,
            ],
            roles);
    }

    [Fact]
    public void The_enum_is_asked_for_by_its_name()
    {
        using var db = ForumDatabase.Filled(nameof(The_enum_is_asked_for_by_its_name));

        int moderators = db.Users().Count(u => u.Role == UserRole.Moderator);

        Assert.Equal(1, moderators);
    }

    [Fact]
    public void The_users_are_asked_for_by_their_roles()
    {
        using var db = ForumDatabase.Filled(nameof(The_users_are_asked_for_by_their_roles));

        List<string> names = db.Users()
            .Where(u => new[] { UserRole.Administrator, UserRole.Moderator }.Contains(u.Role))
            .OrderBy(u => u.Id)
            .Select(u => u.UserName)
            .ToList();

        Assert.Equal(["ada", "grace"], names);
    }

    [Fact]
    public void The_users_are_ordered_by_their_role()
    {
        using var db = ForumDatabase.Filled(nameof(The_users_are_ordered_by_their_role));

        List<UserRole> roles = db.Users()
            .OrderBy(u => u.Role)
            .Select(u => u.Role)
            .ToList();

        // The column holds the numeric value, and the engine orders numbers:
        // the same order .NET gives the enum itself.
        Assert.Equal(
            [UserRole.Guest, UserRole.Member, UserRole.Member, UserRole.Moderator, UserRole.Administrator],
            roles);
    }

    [Fact]
    public void The_decimal_keeps_its_places()
    {
        using var db = ForumDatabase.Filled(nameof(The_decimal_keeps_its_places));

        UserProfile profile = db.UserProfiles().Single(p => p.Balance == 12.50m);

        Assert.Equal(12.50m, profile.Balance);
    }

    [Fact]
    public void The_sum_of_the_decimals_keeps_its_places()
    {
        using var db = ForumDatabase.Filled(nameof(The_sum_of_the_decimals_keeps_its_places));

        decimal sum = db.UserProfiles().Sum(p => p.Balance);

        Assert.Equal(23.50m, sum);
    }

    [Fact]
    public void The_date_comes_back_as_it_went_in()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_comes_back_as_it_went_in));

        Topic topic = db.Topics().OrderBy(t => t.Id).First();

        Assert.Equal(new DateTime(2024, 1, 11, 9, 0, 0), topic.CreatedAt);
    }

    [Fact]
    public void The_date_with_an_hour_comes_back_as_it_went_in()
    {
        using var db = ForumDatabase.Filled(nameof(The_date_with_an_hour_comes_back_as_it_went_in));

        Post post = db.Posts().OrderBy(p => p.Id).First();

        Assert.Equal(new DateTime(2024, 1, 11, 10, 0, 0), post.CreatedAt);
    }

    [Fact]
    public void The_empty_date_comes_back_as_nothing()
    {
        using var db = ForumDatabase.Filled(nameof(The_empty_date_comes_back_as_nothing));

        UserProfile profile = db.UserProfiles().Single(p => p.Reputation == 250);

        Assert.Null(profile.BirthDate);
        Assert.Equal(2, db.UserProfiles().Count(p => p.BirthDate == null));
    }

    [Fact]
    public void The_boolean_comes_back_as_it_went_in()
    {
        using var db = ForumDatabase.Filled(nameof(The_boolean_comes_back_as_it_went_in));

        Topic topic = db.Topics().OrderBy(t => t.Id).First();

        Assert.True(db.TopicStates().Single(s => s.TopicId == topic.Id).IsPinned);
    }

    [Fact]
    public void The_bytes_come_back_as_they_went_in()
    {
        using var db = ForumDatabase.Filled(nameof(The_bytes_come_back_as_they_went_in));

        UserProfile profile = db.UserProfiles().Single(p => p.Reputation == 420);

        Assert.Equal<byte[]>([1, 2, 3], profile.Avatar);
    }

    [Fact]
    public void The_empty_bytes_come_back_as_they_went_in()
    {
        using var db = ForumDatabase.Filled(nameof(The_empty_bytes_come_back_as_they_went_in));

        UserProfile profile = db.UserProfiles().Single(p => p.Reputation == 380);

        Assert.Empty(profile.Avatar);
    }

    [Fact]
    public void The_text_keeps_its_length()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_keeps_its_length));

        User user = db.Users().Single(u => u.UserName == "margaret");

        Assert.Equal("margaret", user.UserName);
        Assert.Equal("margaret@example.org", user.Email);
    }

    [Fact]
    public void The_text_is_asked_for_by_its_length()
    {
        using var db = ForumDatabase.Filled(nameof(The_text_is_asked_for_by_its_length));

        int names = db.Users().Count(u => u.UserName.Length == 8);

        Assert.Equal(1, names);
    }

    [Fact]
    public void The_number_comes_back_as_it_went_in()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_comes_back_as_it_went_in));

        Topic topic = db.Topics().OrderBy(t => t.Id).First();

        Assert.Equal(1200, topic.ViewCount);
    }

    [Fact]
    public void The_number_is_asked_for_by_its_size()
    {
        using var db = ForumDatabase.Filled(nameof(The_number_is_asked_for_by_its_size));

        int topics = db.Topics().Count(t => t.ViewCount > 1000);

        Assert.Equal(1, topics);
    }
}
