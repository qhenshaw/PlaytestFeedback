# Playtest Feedback

In-game feedback tool for Unity 6+ playtests. Players press **F8** to pause the game and open a side panel, where they can:

- pick a category (**Bug**, **Feedback**, **Idea**, or your own),
- write their feedback,
- snip part of the screen and draw on it with a simple brush.

The report goes to a **Discord channel** through a webhook, so there's no server to host. Every report automatically includes the game version, date, platform, hardware, FPS, scene, recent logs, plus any game state you choose to add.

Requires Unity 6000.0+, the Input System package and UI Toolkit.

## What a report looks like in Discord

Each report becomes a **forum post** (or a message, in a normal text channel) containing:

| Part | Contents |
|---|---|
| Title | `🐞 [Bug] First line of the feedback` |
| Embed | Full feedback text, player name, category colour, and fields for version, scene, playtime, platform, GPU, resolution, FPS and error counts, plus your custom game context |
| `screenshot.jpg` | The annotated snip, shown inline in the embed |
| `log.txt` | The last few hundred log lines, with stack traces for errors |
| `context.json` | Everything above as JSON, including all the game context values |

## Setup

### 1. Discord

1. Create a **Forum channel** for feedback (recommended: each report gets its own thread that your team can discuss and tag). A normal text channel works too.
2. Channel settings → **Integrations** → **Webhooks** → **New Webhook** → **Copy Webhook URL**.
3. Optional: create forum tags such as *Bug*, *Feedback*, *Idea*, *Fixed*, *Won't Fix*. Leave **"Require people to select tags when posting"** off: webhook posts with no tag would be rejected.

### 2. Unity

1. Add the package: **Window → Package Manager → + → Add package from disk…** and pick `package.json` in this folder.
   Or add it to `Packages/manifest.json`:
   ```json
   "com.qhenshaw.playtestfeedback": "file:E:/VFS/Packages/PlaytestFeedback"
   ```
2. **Tools → Playtest Feedback → Create or Select Settings**. This creates `Assets/Resources/PlaytestFeedbackSettings.asset`.
3. Paste the webhook URL and click **Send Test Message**.
4. Enter Play mode and press **F8**.

That's it. The tool creates itself at startup, so you don't need to add anything to your scenes.

## Controls

| Where | Input | Action |
|---|---|---|
| Game | F8 / L3 + R3 (configurable) | Open or close the panel |
| Panel | Ctrl + Enter | Send |
| Panel | Esc | Close (the draft is kept) |
| Snip | Drag | Select an area |
| Snip | Enter | Use the full screen |
| Snip / Draw | Esc / gamepad B | Cancel |
| Draw | Enter | Done |
| Draw | Ctrl + Z | Undo |

### Changing the hotkey

The **Hotkey** section of the settings asset is a regular Input System action, with the usual binding editor:

- Select a binding, open the **Path** dropdown and click **Listen**, then press the key or button you want.
- Use **+** to add more bindings, including combos (*Add Binding With One Modifier*), e.g. Ctrl + F8 or Select + Start.
- If every binding is removed, the defaults (F8, L3 + R3) are used.

The action is copied at runtime, so the asset is never modified in Play mode.

## Adding game state

Implement `IFeedbackContextProvider` on any component:

```csharp
public class PlayerFeedbackContext : MonoBehaviour, IFeedbackContextProvider
{
    void OnEnable()  => FeedbackManager.RegisterProvider(this);
    void OnDisable() => FeedbackManager.UnregisterProvider(this);

    public void CollectFeedbackContext(IDictionary<string, object> context)
    {
        context["Player Position"] = transform.position;
        context["Health"] = GetComponent<Health>().Current;
    }
}
```

Or add single values with a lambda:

```csharp
FeedbackManager.AddContext("Seed", () => worldGenerator.Seed);
```

Values are read once when the panel opens, before the game is paused. See the **Game Context Example** sample in the Package Manager.

## Scripting API

```csharp
FeedbackManager.Open();                 // e.g. from a "Send Feedback" button in your pause menu
FeedbackManager.Close();
FeedbackManager.IsOpen;
FeedbackManager.Opened    += ...;       // pause systems that ignore Time.timeScale
FeedbackManager.Closed    += ...;
FeedbackManager.Submitted += report => ...;
FeedbackSettings.WebhookUrlOverride = "..."; // use a webhook from somewhere other than the asset
```

## While the panel is open

Each of these can be turned off in the settings:

- `Time.timeScale` is set to 0 and audio is paused (`AudioListener.pause`). Both are restored on close.
- The cursor is unlocked and shown, then restored.
- **Every enabled Input System action is disabled**, except action maps listed in *Input Maps To Keep Enabled* (`UI` by default, for `InputSystemUIInputModule`). This stops typing from moving the player. Actions are re-enabled one frame after closing, so the Esc that closed the panel doesn't also open your pause menu.

Code that reads devices directly (`Keyboard.current.spaceKey.wasPressedThisFrame`) bypasses actions, so it can't be blocked. Check `FeedbackManager.IsOpen` there.

## Reliability

If a report can't be sent (offline, Discord down, rate limited), it is saved under `Application.persistentDataPath/PlaytestFeedback/Outbox` and sent automatically on the next launch. **Tools → Playtest Feedback → Open Outbox Folder** opens it in the Editor. Reports Discord rejects outright (bad webhook, invalid tag) are renamed to `.failed` and not retried.

## Forum tags per category

Each category has an optional **Forum Tag Id**. When it's set, the post is tagged automatically. Discord doesn't show tag IDs in the app; you can read them from the channel's `available_tags` with a bot token:

```bash
curl -H "Authorization: Bot <token>" https://discord.com/api/v10/channels/<forum channel id>
```

You don't need tags: the category is always in the post title (`[Bug] …`), so Discord search works without them.

## Security and privacy

- The webhook URL is stored in the build and **can be extracted by anyone who has it**. That's acceptable for closed playtests. Use **one webhook per playtest round** and delete it in Discord when the round ends, or immediately if it leaks.
- If this project's repo is public, keep the URL out of it: don't commit the settings asset with the URL filled in, or set `FeedbackSettings.WebhookUrlOverride` at runtime instead.
- Player text can never ping `@everyone` or roles (`allowed_mentions` is empty).
- Screenshots may show things like usernames. The panel tells players what gets sent.
- Turn on **Development Builds Only**, or add the `PLAYTEST_FEEDBACK_DISABLED` scripting define, to keep the tool out of release builds.

## Limits

- Discord allows about 10 MB of attachments per message. Screenshots are JPG and scaled to at most 1920 px (configurable). If the total is still too big, the log is dropped first.
- An embed holds at most 6000 characters, so long text or many context values are trimmed in the post. The full data is always in `context.json`.

## Troubleshooting

| Problem | Fix |
|---|---|
| Nothing happens on F8 | Check the Console for a "No settings found" warning, and that *Enabled In Editor* is on. |
| Panel opens but can't be clicked or typed in | If you use an `EventSystem` with `InputSystemUIInputModule`, make sure its action map's name is in *Input Maps To Keep Enabled*. |
| Input stops working while paused | Input System *Update Mode* set to *Process Events In Fixed Update* doesn't process input when `timeScale` is 0. Use Dynamic Update, or turn off *Pause Time*. |
| `400` error mentioning `thread_name` | The webhook isn't in a Forum channel. Untick *Post To Forum Channel*. |
| `404 Unknown Webhook` | The webhook was deleted. Create a new one and update the settings. |
