using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Data;
using Atlas.Domain;
using Atlas.Framework;
using Atlas.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atlas.Areas.Admin.Pages.ForumGroups
{
    public class IndexModel : BasePageModel
    {
        private readonly AtlasDbContext _dbContext;
        private readonly IContextService _contextService;

        public IndexModel(AtlasDbContext dbContext, IContextService contextService)
        {
            _dbContext = dbContext;
            _contextService = contextService;
        }

        public IList<ForumGroupModel> ForumGroups { get; } = new List<ForumGroupModel>();

        public async Task OnGetAsync()
        {
            var forumGroups = await _dbContext.ForumGroups
                .Include(x => x.PermissionSet)
                .Where(x => x.SiteId == _contextService.CurrentSite().Id && x.Status != StatusType.Deleted)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            foreach (var forumGroup in forumGroups)
            {
                ForumGroups.Add(new ForumGroupModel
                {
                    Id = forumGroup.Id,
                    Name = forumGroup.Name,
                    SortOrder = forumGroup.SortOrder,
                    TotalTopics = forumGroup.TopicsCount,
                    TotalReplies = forumGroup.RepliesCount
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var forumGroup = await _dbContext.ForumGroups.FirstOrDefaultAsync(x => x.Id == id && x.Status != StatusType.Deleted);

            if (forumGroup == null)
            {
                return NotFound();
            }

            forumGroup.Delete();

            var forums = await _dbContext.Forums
                .Where(x => x.ForumGroupId == forumGroup.Id)
                .ToListAsync();

            foreach (var forum in forums)
            {
                forum.Delete();
            }

            await _dbContext.SaveChangesAsync();

            return RedirectToPage();
        }

        public class ForumGroupModel
        {
            public Guid Id { get; set; }
            public string Name { get; set; }
            public int SortOrder { get; set; }
            public int TotalTopics { get; set; }
            public int TotalReplies { get; set; }
            public string PermissionSetName { get; set; }
        }
    }
}
