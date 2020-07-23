﻿using Atlas.Data;
using Atlas.Data.Caching;
using Atlas.Data.Services;
using Atlas.Domain;
using Atlas.Domain.Forums.Commands;
using AutoFixture;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Tests.Data.Services
{
    [TestFixture]
    public class ForumServiceTests : TestFixtureBase
    {
        [Test]
        public async Task Should_create_new_forum_and_add_event()
        {
            using (var dbContext = new AtlasDbContext(Shared.CreateContextOptions()))
            {
                var command = Fixture.Create<CreateForum>();

                var cacheManager = new Mock<ICacheManager>();

                var createValidator = new Mock<IValidator<CreateForum>>();
                createValidator
                    .Setup(x => x.ValidateAsync(command, new CancellationToken()))
                    .ReturnsAsync(new ValidationResult());

                var updateValidator = new Mock<IValidator<UpdateForum>>();

                var sut = new ForumService(dbContext,
                    cacheManager.Object, 
                    createValidator.Object, 
                    updateValidator.Object);

                await sut.CreateAsync(command);

                var forum = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == command.Id);
                var @event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == command.Id);

                createValidator.Verify(x => x.ValidateAsync(command, new CancellationToken()));
                Assert.NotNull(forum);
                Assert.AreEqual(1, forum.SortOrder);
                Assert.NotNull(@event);
            }
        }

        [Test]
        public async Task Should_update_forum_and_add_event()
        {
            var options = Shared.CreateContextOptions();
            var forum = Fixture.Create<Forum>();

            using (var dbContext = new AtlasDbContext(options))
            {
                dbContext.Forums.Add(forum);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlasDbContext(options))
            {
                var command = Fixture.Build<UpdateForum>()
                        .With(x => x.Id, forum.Id)
                        .With(x => x.ForumGroupId, forum.ForumGroupId)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();

                var createValidator = new Mock<IValidator<CreateForum>>();

                var updateValidator = new Mock<IValidator<UpdateForum>>();
                updateValidator
                    .Setup(x => x.ValidateAsync(command, new CancellationToken()))
                    .ReturnsAsync(new ValidationResult());

                var sut = new ForumService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.UpdateAsync(command);

                var updatedForum = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == command.Id);
                var @event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == command.Id);

                updateValidator.Verify(x => x.ValidateAsync(command, new CancellationToken()));
                Assert.AreEqual(command.Name, updatedForum.Name);
                Assert.NotNull(@event);
            }
        }

        [Test]
        public async Task Should_move_forum_up_and_add_events()
        {
            var options = Shared.CreateContextOptions();

            var forumGroupId = Guid.NewGuid();

            var forum1 = new Forum(forumGroupId, "Forum 1", 1);
            var forum2 = new Forum(forumGroupId, "Forum 2", 2);

            using (var dbContext = new AtlasDbContext(options))
            {
                dbContext.Forums.Add(forum1);
                dbContext.Forums.Add(forum2);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlasDbContext(options))
            {
                var command = new MoveForum
                {
                    Id = forum2.Id,
                    Direction = Direction.Up
                };

                var cacheManager = new Mock<ICacheManager>();
                var createValidator = new Mock<IValidator<CreateForum>>();
                var updateValidator = new Mock<IValidator<UpdateForum>>();

                var sut = new ForumService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.MoveAsync(command);

                var updatedForum1 = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum1.Id);
                var updatedForum2 = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum2.Id);

                var event1 = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum1.Id);
                var event2 = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum2.Id);

                Assert.AreEqual(forum2.SortOrder, updatedForum1.SortOrder);
                Assert.AreEqual(forum1.SortOrder, updatedForum2.SortOrder);
                Assert.NotNull(event1);
                Assert.NotNull(event2);
            }
        }

        [Test]
        public async Task Should_move_forum_down_and_add_events()
        {
            var options = Shared.CreateContextOptions();

            var forumGroupId = Guid.NewGuid();

            var forum1 = new Forum(forumGroupId, "Forum 1", 1);
            var forum2 = new Forum(forumGroupId, "Forum 2", 2);

            using (var dbContext = new AtlasDbContext(options))
            {
                dbContext.Forums.Add(forum1);
                dbContext.Forums.Add(forum2);
                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlasDbContext(options))
            {
                var command = new MoveForum
                {
                    Id = forum1.Id,
                    Direction = Direction.Down
                };

                var cacheManager = new Mock<ICacheManager>();
                var createValidator = new Mock<IValidator<CreateForum>>();
                var updateValidator = new Mock<IValidator<UpdateForum>>();

                var sut = new ForumService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.MoveAsync(command);

                var updatedForum1 = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum1.Id);
                var updatedForum2 = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum2.Id);

                var event1 = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum1.Id);
                var event2 = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum2.Id);

                Assert.AreEqual(forum2.SortOrder, updatedForum1.SortOrder);
                Assert.AreEqual(forum1.SortOrder, updatedForum2.SortOrder);
                Assert.NotNull(event1);
                Assert.NotNull(event2);
            }
        }

        [Test]
        public async Task Should_delete_forum_and_reorder_other_forums_and_add_event()
        {
            var options = Shared.CreateContextOptions();

            var forumGroupId = Guid.NewGuid();

            var forum1 = new Forum(forumGroupId, "Forum 1", 1);
            var forum2 = new Forum(forumGroupId, "Forum 2", 2);
            var forum3 = new Forum(forumGroupId, "Forum 3", 3);
            var forum4 = new Forum(forumGroupId, "Forum 4", 4);

            using (var dbContext = new AtlasDbContext(options))
            {
                dbContext.Forums.Add(forum1);
                dbContext.Forums.Add(forum2);
                dbContext.Forums.Add(forum3);
                dbContext.Forums.Add(forum4);

                await dbContext.SaveChangesAsync();
            }

            using (var dbContext = new AtlasDbContext(options))
            {
                var command = Fixture.Build<DeleteForum>()
                        .With(x => x.Id, forum2.Id)
                    .Create();

                var cacheManager = new Mock<ICacheManager>();
                var createValidator = new Mock<IValidator<CreateForum>>();
                var updateValidator = new Mock<IValidator<UpdateForum>>();

                var sut = new ForumService(dbContext,
                    cacheManager.Object,
                    createValidator.Object,
                    updateValidator.Object);

                await sut.DeleteAsync(command);

                var forum1Reordered = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum1.Id);
                var forum2Deleted = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum2.Id);
                var forum3Reordered = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum3.Id);
                var forum4Reordered = await dbContext.Forums.FirstOrDefaultAsync(x => x.Id == forum4.Id);

                var forum1Event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum1.Id);
                var forum2Event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum2.Id);
                var forum3Event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum3.Id);
                var forum4Event = await dbContext.Events.FirstOrDefaultAsync(x => x.TargetId == forum4.Id);

                Assert.AreEqual(forum1.SortOrder, forum1Reordered.SortOrder);
                Assert.AreEqual(StatusType.Deleted, forum2Deleted.Status);
                Assert.AreEqual(forum2.SortOrder, forum3Reordered.SortOrder);
                Assert.AreEqual(forum3.SortOrder, forum4Reordered.SortOrder);
                Assert.NotNull(forum1Event);
                Assert.NotNull(forum2Event);
                Assert.NotNull(forum3Event);
                Assert.NotNull(forum4Event);
            }
        }
    }
}
