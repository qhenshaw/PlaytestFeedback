# Changelog

[0.1.0] - 2026-11-14
- First release of Playtest Feedback.
- F8 / gamepad hotkey opens a UI Toolkit side panel and pauses the game (time, audio, cursor, Input System actions).
- Hotkey is an Input System action on the settings asset, edited with the standard binding editor (Listen, multiple bindings, modifier combos).
- Category picker (Bug / Feedback / Idea by default, configurable, with optional Discord forum tags).
- Screenshot snip from a frozen frame, with brush annotation (colours, sizes, undo, clear).
- Automatic metadata: version, platform, hardware, FPS, scenes, playtime, log counts, recent logs.
- Custom game state through `IFeedbackContextProvider` and `FeedbackManager.AddContext`.
- Sends to a Discord webhook (forum post or message) with screenshot, log.txt and context.json.
- Offline outbox: failed reports are saved and retried on the next launch.
- Settings inspector with a "Send Test Message" button.
