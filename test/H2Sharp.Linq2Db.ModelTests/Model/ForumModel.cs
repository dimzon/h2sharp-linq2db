/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MIT License (see LICENSE).
 */
using LinqToDB.Mapping;

namespace H2Sharp.Linq2Db.ModelTests.Model;

/// <summary>
///     A forum: the forums, the topics in them, the answers to the topics, the people who
///     write them, the tags and the votes.
/// </summary>
/// <remarks>
///     <para>
///         The model is here to carry the capabilities of the provider: the types, the
///         nullability, the identity, the indexes and the composite key. Every column has a
///         type H2 2.5 has, and every name is the name H2 folds it to.
///     </para>
///     <para>
///         The tables are joined by their columns and not by the foreign keys, because the engine
///         does not read a foreign key inside CREATE TABLE yet (the reference takes it, the engine
///         refuses it: "expected "PRIMARY""). The queries join the tables the way the forum is
///         joined, and the keys of the relations are the columns the relations are built on.
///     </para>
///     <para>
///         The dates are not read from the clock: the seed puts the same dates in every run, so
///         that the answers of the queries are the same every time.
///     </para>
/// </remarks>
[Table("Forums")]
public class Forum
{
    [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
    public int Id { get; set; }

    [Column("Name", Length = 100)]
    public string Name { get; set; } = "";

    [Column("Description", Length = 500)]
    public string Description { get; set; } = "";

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("IsArchived")]
    public bool IsArchived { get; set; }
}

/// <summary>
///     A person who writes in the forum.
/// </summary>
[Table("Users")]
public class User
{
    [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
    public int Id { get; set; }

    [Column("UserName", Length = 50)]
    public string UserName { get; set; } = "";

    [Column("Email", Length = 200)]
    public string Email { get; set; } = "";

    [Column("JoinedAt")]
    public DateTime JoinedAt { get; set; }

    [Column("LastSeenAt")]
    public DateTime? LastSeenAt { get; set; }

    [Column("Role")]
    public UserRole Role { get; set; }

    [Column("IsBanned")]
    public bool IsBanned { get; set; }
}

/// <summary>
///     What a person knows about themselves: the day they were born, what they are worth and
///     what they look like.
/// </summary>
[Table("UserProfiles")]
public class UserProfile
{
    [Column("UserId", IsPrimaryKey = true)]
    public int UserId { get; set; }

    [Column("BirthDate")]
    public DateTime? BirthDate { get; set; }

    [Column("Reputation")]
    public int Reputation { get; set; }

    [Column("Balance", Precision = 18, Scale = 2)]
    public decimal Balance { get; set; }

    [Column("Avatar")]
    public byte[] Avatar { get; set; } = [];
}

/// <summary>
///     A topic of a forum.
/// </summary>
[Table("Topics")]
public class Topic
{
    [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
    public int Id { get; set; }

    [Column("ForumId")]
    public int ForumId { get; set; }

    [Column("AuthorId")]
    public int AuthorId { get; set; }

    [Column("Title", Length = 200)]
    public string Title { get; set; } = "";

    [Column("Body")]
    public string Body { get; set; } = "";

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("LastPostAt")]
    public DateTime LastPostAt { get; set; }

    [Column("ViewCount")]
    public int ViewCount { get; set; }
}

/// <summary>
///     What the moderator thinks of a topic: whether it is pinned, whether it is closed and what
///     was written about it.
/// </summary>
[Table("TopicStates")]
public class TopicState
{
    [Column("TopicId", IsPrimaryKey = true)]
    public int TopicId { get; set; }

    [Column("IsPinned")]
    public bool IsPinned { get; set; }

    [Column("IsClosed")]
    public bool IsClosed { get; set; }

    [Column("ModeratorNote", Length = 500)]
    public string? ModeratorNote { get; set; }
}

/// <summary>
///     An answer to a topic.
/// </summary>
[Table("Posts")]
public class Post
{
    [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
    public int Id { get; set; }

    [Column("TopicId")]
    public int TopicId { get; set; }

    [Column("AuthorId")]
    public int AuthorId { get; set; }

    [Column("Body")]
    public string Body { get; set; } = "";

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("IsHidden")]
    public bool IsHidden { get; set; }
}

/// <summary>
///     A word the topics are marked with.
/// </summary>
[Table("Tags")]
public class Tag
{
    [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
    public int Id { get; set; }

    [Column("Name", Length = 50)]
    public string Name { get; set; } = "";
}

/// <summary>
///     A topic marked with a tag.
/// </summary>
[Table("TopicTags")]
public class TopicTag
{
    [Column("TopicId", IsPrimaryKey = true)]
    public int TopicId { get; set; }

    [Column("TagId", IsPrimaryKey = true)]
    public int TagId { get; set; }
}

/// <summary>
///     A vote of a person for an answer.
/// </summary>
[Table("Votes")]
public class Vote
{
    [Column("Id", IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true)]
    public int Id { get; set; }

    [Column("PostId")]
    public int PostId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("Value")]
    public int Value { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     The role of a person in the forum.
/// </summary>
public enum UserRole
{
    Guest,
    Member,
    Moderator,
    Administrator,
}
