﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Atlas.Client.Components.Shared
{
    public abstract class PagerComponent : SharedComponentBase
    {
        [Parameter] public int TotalPages { get; set; }
        [Parameter] public int CurrentPage { get; set; } = 1;
        [Parameter] public EventCallback<int> OnLoadDataCallback { get; set; }

        protected override void OnInitialized()
        {
            SetPager("forward");
        }

        private const int PagerSize = 10;

        protected int StartPage;
        protected int EndPage;

        protected async Task RefreshData(int page)
        {
            await OnLoadDataCallback.InvokeAsync(page);
            CurrentPage = page;
            StateHasChanged();
        }

        public void SetPager(string direction)
        {
            if (direction == "forward" && EndPage < TotalPages)
            {
                StartPage = EndPage + 1;
                if (EndPage + PagerSize < TotalPages)
                {
                    EndPage = StartPage + PagerSize - 1;
                }
                else
                {
                    EndPage = TotalPages;
                }
                StateHasChanged();
            }
            else if (direction == "back" && StartPage > 1)
            {
                EndPage = StartPage - 1;
                StartPage -= PagerSize;
            }
            else
            {
                StartPage = 1;
                EndPage = TotalPages;
            }
        }

        protected async Task NavigateToPage(string action)
        {
            if (action == "next")
            {
                if (CurrentPage < TotalPages)
                {
                    if (CurrentPage == EndPage)
                    {
                        SetPager("forward");
                    }
                    CurrentPage += 1;
                }
            }
            else if (action == "previous")
            {
                if (CurrentPage > 1)
                {
                    if (CurrentPage == StartPage)
                    {
                        SetPager("back");
                    }
                    CurrentPage -= 1;
                }
            }
            else if (action == "first")
            {
                StartPage = 0;
                EndPage = 0;
                SetPager("forward");
                CurrentPage = 1;
            }
            else if (action == "last")
            {
                var pagers = (int)Math.Ceiling(TotalPages / (decimal)PagerSize);

                if (pagers > 1)
                {
                    EndPage = (pagers - 1) * PagerSize;
                    SetPager("forward");
                }

                CurrentPage = TotalPages;
            }

            await RefreshData(CurrentPage);
        }
    }
}