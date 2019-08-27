﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Users;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace osu.Game.Overlays.Profile.Sections
{
    public abstract class PaginatedContainer<T> : FillFlowContainer
    {
        private readonly ShowMoreButton moreButton;
        private readonly OsuSpriteText missingText;
        private APIRequest<List<T>> retrievalRequest;
        private CancellationTokenSource loadCancellation;

        [Resolved]
        private IAPIProvider api { get; set; }

        protected int VisiblePages;
        protected int ItemsPerPage;

        protected readonly Bindable<User> User = new Bindable<User>();
        protected readonly FillFlowContainer ItemsContainer;
        protected RulesetStore Rulesets;

        protected PaginatedContainer(Bindable<User> user, string header, string missing)
        {
            User.BindTo(user);

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;

            Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = header,
                    Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold),
                    Margin = new MarginPadding { Top = 10, Bottom = 10 },
                },
                ItemsContainer = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Spacing = new Vector2(0, 2),
                },
                moreButton = new ShowMoreButton
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Alpha = 0,
                    Margin = new MarginPadding { Top = 10 },
                    Action = showMore,
                },
                missingText = new OsuSpriteText
                {
                    Font = OsuFont.GetFont(size: 15),
                    Text = missing,
                    Alpha = 0,
                },
            };
        }

        [BackgroundDependencyLoader]
        private void load(RulesetStore rulesets)
        {
            Rulesets = rulesets;

            User.ValueChanged += onUserChanged;
            User.TriggerChange();
        }

        private void onUserChanged(ValueChangedEvent<User> e)
        {
            loadCancellation?.Cancel();
            retrievalRequest?.Cancel();

            VisiblePages = 0;
            ItemsContainer.Clear();

            if (e.NewValue != null)
                showMore();
        }

        private void showMore()
        {
            loadCancellation = new CancellationTokenSource();

            retrievalRequest = CreateRequest();
            retrievalRequest.Success += UpdateItems;

            api.Queue(retrievalRequest);
        }

        protected virtual void UpdateItems(List<T> items)
        {
            Schedule(() =>
            {
                if (!items.Any() && VisiblePages == 1)
                {
                    moreButton.Hide();
                    moreButton.IsLoading = false;
                    missingText.Show();
                    return;
                }

                LoadComponentsAsync(items.Where(item => AllowCreate(item)).Select(CreateDrawableItem), drawables =>
                {
                    missingText.Hide();
                    moreButton.FadeTo(items.Count == ItemsPerPage ? 1 : 0);
                    moreButton.IsLoading = false;

                    ItemsContainer.AddRange(drawables);
                }, loadCancellation.Token);
            });
        }

        /// <summary>
        /// Used to check whether the item is suitable for drawable creation.
        /// </summary>
        /// <param name="item">An item to check</param>
        /// <returns></returns>
        protected virtual bool AllowCreate(T item) => true;

        protected abstract APIRequest<List<T>> CreateRequest();

        protected abstract Drawable CreateDrawableItem(T item);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            retrievalRequest?.Cancel();
        }
    }
}
