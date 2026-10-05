using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace ExampleAddon;

/// <summary>
/// The Radio app: today's bulletin on a brown panel, in the same parchment look as AliveNpcs' own
/// screens (see docs/computer-apps.md, "Matching the AliveNpcs look").
/// </summary>
internal sealed class RadioMenu : IClickableMenu
{
    private static readonly Rectangle CreamFrame = new(0, 256, 60, 60);
    private static readonly Rectangle BrownFrame = new(403, 373, 9, 9);
    private static readonly Color BrownTint = new(185, 125, 80);
    private static readonly Color PanelText = new(255, 250, 240);
    private static readonly Color PanelShadow = new(86, 46, 22);
    private static readonly Color Secondary = new(235, 200, 150);

    private readonly RadioBulletin _bulletin;
    private readonly string _title;
    private readonly string _tuning;

    private Rectangle _banner;
    private Rectangle _panel;
    private string? _wrappedFor;
    private string _wrapped = "";

    public RadioMenu(RadioBulletin bulletin, ITranslationHelper translation)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        _bulletin = bulletin;
        _title = translation.Get("radio.title");
        _tuning = translation.Get("radio.tuning");
        Layout();
    }

    private void Layout()
    {
        width = Math.Min(900, Game1.uiViewport.Width - 48);
        height = Math.Min(420, Game1.uiViewport.Height - 140);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2 + 45;

        var bannerWidth = Math.Min(width - 80, SpriteText.getWidthOfString(_title) + 96);
        _banner = new Rectangle(xPositionOnScreen + (width - bannerWidth) / 2, yPositionOnScreen - 90, bannerWidth, 84);
        _panel = new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + 26, width - 60, height - 52);
        _wrappedFor = null;

        initializeUpperRightCloseButton();
        upperRightCloseButton.bounds = new Rectangle(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48);
        upperRightCloseButton.myID = 0;
        populateClickableComponentList();
    }

    public override void snapToDefaultClickableComponent()
    {
        currentlySnappedComponent = upperRightCloseButton;
        snapCursorToCurrentSnappedComponent();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Layout();
    }

    public override void receiveKeyPress(Keys key)
    {
        // The controller's B arrives as Escape too. exitThisMenu returns the player to the Apps folder.
        if (key == Keys.Escape)
        {
            exitThisMenu();
            return;
        }

        base.receiveKeyPress(key);
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);

        drawTextureBox(b, Game1.menuTexture, CreamFrame, _banner.X, _banner.Y, _banner.Width, _banner.Height, Color.White);
        SpriteText.drawString(b, _title, _banner.X + (_banner.Width - SpriteText.getWidthOfString(_title)) / 2, _banner.Y + (_banner.Height - 44) / 2);

        drawTextureBox(b, Game1.menuTexture, CreamFrame, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);
        drawTextureBox(b, Game1.mouseCursors, BrownFrame, _panel.X, _panel.Y, _panel.Width, _panel.Height, BrownTint, 4f, drawShadow: false);

        var at = new Vector2(_panel.X + 28, _panel.Y + 28);
        var bulletin = _bulletin.Today();
        if (bulletin == null)
        {
            b.DrawString(Game1.smallFont, _tuning, at, Secondary);
        }
        else
        {
            if (!ReferenceEquals(bulletin, _wrappedFor))
            {
                _wrapped = Game1.parseText(bulletin, Game1.smallFont, _panel.Width - 56);
                _wrappedFor = bulletin;
            }

            b.DrawString(Game1.smallFont, _wrapped, at + new Vector2(-2, 2), PanelShadow);
            b.DrawString(Game1.smallFont, _wrapped, at + new Vector2(0, 2), PanelShadow);
            b.DrawString(Game1.smallFont, _wrapped, at, PanelText);
        }

        base.draw(b); // the close button
        drawMouse(b);
    }
}
