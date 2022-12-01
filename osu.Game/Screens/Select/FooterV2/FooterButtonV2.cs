// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Select.FooterV2
{
    public partial class FooterButtonV2 : OsuClickableContainer, IKeyBindingHandler<GlobalAction>
    {
        private const int button_height = 120;
        private const int button_width = 140;
        private const int corner_radius = 10;

        public const float SHEAR_WIDTH = 16;

        protected static readonly Vector2 SHEAR = new Vector2(SHEAR_WIDTH / button_height, 0);

        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private Colour4 buttonAccentColour;

        protected Colour4 AccentColour
        {
            set
            {
                buttonAccentColour = value;
                bar.Colour = buttonAccentColour;
                icon.Colour = buttonAccentColour;
            }
        }

        protected IconUsage Icon
        {
            set => icon.Icon = value;
        }

        protected LocalisableString Text
        {
            set => text.Text = value;
        }

        private SpriteText text = null!;
        private SpriteIcon icon = null!;
        protected Container TextContainer = null!;
        private Box bar = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Radius = 5,
                Roundness = 10,
                Colour = Colour4.Black.Opacity(0.25f)
            };
            Shear = SHEAR;
            Size = new Vector2(button_width, button_height);
            Masking = true;
            CornerRadius = corner_radius;
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = colourProvider.Background3,
                    RelativeSizeAxes = Axes.Both
                },

                //For elements that should not be sheared.
                new Container
                {
                    Shear = -SHEAR,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        TextContainer = new Container
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Position = new Vector2(-SHEAR_WIDTH * (52f / button_height), 42),
                            AutoSizeAxes = Axes.Both,
                            Child = text = new OsuSpriteText
                            {
                                Font = OsuFont.TorusAlternate.With(size: 16),
                                AlwaysPresent = true
                            }
                        },
                        icon = new SpriteIcon
                        {
                            //We want to offset this by the same amount as the text for aesthetic purposes
                            Position = new Vector2(-SHEAR_WIDTH * (52f / button_height), 12),
                            Size = new Vector2(20),
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre
                        },
                        new Container
                        {
                            //Offset the bar to centre it with consideration for the shearing
                            Position = new Vector2(-SHEAR_WIDTH * (80f / button_height), -40),
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(120, 6),
                            Masking = true,
                            CornerRadius = 3,
                            Child = bar = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                            }
                        }
                    }
                }
            };
        }

        public Action Hovered = null!;
        public Action HoverLost = null!;
        public GlobalAction? Hotkey;

        protected override void UpdateAfterChildren()
        {
        }

        protected override bool OnHover(HoverEvent e)
        {
            Hovered?.Invoke();

            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            HoverLost?.Invoke();
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (!Enabled.Value)
                return true;

            return base.OnClick(e);
        }

        public virtual bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action == Hotkey && !e.Repeat)
            {
                TriggerClick();
                return true;
            }

            return false;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<GlobalAction> e) { }
    }
}
