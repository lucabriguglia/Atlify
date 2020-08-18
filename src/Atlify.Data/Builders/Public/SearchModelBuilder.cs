﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlify.Models;
using Atlify.Models.Public.Search;
using Atlify.Data.Caching;
using Atlify.Domain;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace Atlify.Data.Builders.Public
{
    public class SearchModelBuilder : ISearchModelBuilder
    {
        private readonly AtlifyDbContext _dbContext;
        private readonly ICacheManager _cacheManager;
        private readonly IGravatarService _gravatarService;

        public SearchModelBuilder(AtlifyDbContext dbContext,
            ICacheManager cacheManager,
            IGravatarService gravatarService)
        {
            _dbContext = dbContext;
            _cacheManager = cacheManager;
            _gravatarService = gravatarService;
        }

        public async Task<SearchPageModel> BuildSearchPageModelAsync(Guid siteId, IList<Guid> forumIds, QueryOptions options)
        {
            var result = new SearchPageModel
            {
                Posts = await SearchPostModels(forumIds, options)
            };

            return result;
        }

        public async Task<PaginatedData<SearchPostModel>> SearchPostModels(IList<Guid> forumIds, QueryOptions options, Guid? memberId = null)
        {
            var postsQuery = _dbContext.Posts
                .Where(x =>
                    forumIds.Contains(x.ForumId) &&
                    x.Status == StatusType.Published &&
                    (x.Topic == null || x.Topic.Status == StatusType.Published));

            if (!string.IsNullOrWhiteSpace(options.Search))
            {
                postsQuery = postsQuery
                    .Where(x => x.Title.Contains(options.Search) || x.Content.Contains(options.Search));
            }

            if (memberId != null)
            {
                postsQuery = postsQuery.Where(x => x.MemberId == memberId);
            }

            var posts = await postsQuery
                .OrderByDescending(x => x.TimeStamp)
                .Skip(options.Skip)
                .Take(options.PageSize)
                .Select(p => new
                {
                    p.Id,
                    TopicId = p.TopicId ?? p.Id,
                    IsTopic = p.TopicId == null,
                    Title = p.Title ?? p.Topic.Title,
                    Slug = p.Slug ?? p.Topic.Slug,
                    p.Content,
                    p.TimeStamp,
                    p.MemberId,
                    MemberDisplayName = p.Member.DisplayName,
                    p.ForumId,
                    ForumName = p.Forum.Name,
                    ForumSlug = p.Forum.Slug
                })
                .ToListAsync();

            var items = posts.Select(post => new SearchPostModel
            {
                Id = post.Id,
                TopicId = post.TopicId,
                IsTopic = post.IsTopic,
                Title = post.Title,
                Slug = post.Slug,
                Content = Markdown.ToHtml(post.Content),
                TimeStamp = post.TimeStamp,
                MemberId = post.MemberId,
                MemberDisplayName = post.MemberDisplayName,
                ForumId = post.ForumId,
                ForumName = post.ForumName,
                ForumSlug = post.ForumSlug
            }).ToList();

            var totalRecords = await postsQuery.CountAsync();

            return new PaginatedData<SearchPostModel>(items, totalRecords, options.PageSize);
        }
    }
}