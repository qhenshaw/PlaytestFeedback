using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PlaytestFeedback
{
    /// <summary>
    /// A feedback category the player can pick (Bug, Feedback, Idea...).
    /// </summary>
    [Serializable]
    public sealed class FeedbackCategory
    {
        [Tooltip("Name shown on the category button and in the Discord post title.")]
        public string name = "Feedback";

        [Tooltip("Button highlight and Discord embed colour.")]
        public Color color = new Color(0.35f, 0.4f, 0.95f);

        [Tooltip("Optional emoji put in front of the Discord forum post title.")]
        public string discordEmoji = "";

        [Tooltip("Optional Discord forum tag ID (a long number) applied to the post. Leave empty to skip. See the README for how to find tag IDs.")]
        public string forumTagId = "";

        public FeedbackCategory() { }

        public FeedbackCategory(string name, Color color, string discordEmoji)
        {
            this.name = name;
            this.color = color;
            this.discordEmoji = discordEmoji;
        }
    }

    /// <summary>
    /// Project settings for the playtest feedback tool. Create it with
    /// Tools > Playtest Feedback > Create or Select Settings. It must live in a Resources folder
    /// and be named "PlaytestFeedbackSettings" to be picked up automatically.
    /// </summary>
    [CreateAssetMenu(menuName = "Playtest Feedback/Settings", fileName = ResourcesPath)]
    public sealed class FeedbackSettings : ScriptableObject
    {
        public const string ResourcesPath = "PlaytestFeedbackSettings";

        [Header("Discord")]
        [Tooltip("Discord webhook URL (Channel settings > Integrations > Webhooks). Anyone with the build can extract this, so use one webhook per playtest round and delete it afterwards.")]
        public string webhookUrl = "";

        [Tooltip("Enable if the webhook points at a Forum channel. Each report then becomes its own forum post (thread), and category tags can be applied.")]
        public bool postToForumChannel = true;

        [Tooltip("Name the webhook posts as. Discord rejects names containing 'discord' or 'clyde'.")]
        public string botUsername = "Playtest Feedback";

        [Tooltip("Optional avatar image URL for the webhook posts.")]
        public string botAvatarUrl = "";

        [Header("Availability")]
        [Tooltip("Automatically create the feedback tool when the game starts. Turn off to add a FeedbackManager to a scene yourself.")]
        public bool autoCreate = true;

        [Tooltip("Allow the tool to run in the Unity Editor.")]
        public bool enabledInEditor = true;

        [Tooltip("Only enable the tool in Development Builds (and the Editor).")]
        public bool developmentBuildsOnly = false;

        [Header("Hotkey")]
        [Tooltip("Opens and closes the feedback panel. Add, remove or edit bindings here; use 'Listen' in the binding path picker to press the key you want. Defaults to F8, or L3 + R3 on a gamepad.")]
        public InputAction toggleAction = CreateDefaultToggleAction();

        [Header("While the panel is open")]
        [Tooltip("Set Time.timeScale to 0 while the panel is open.")]
        public bool pauseTime = true;

        [Tooltip("Pause all audio (AudioListener.pause) while the panel is open.")]
        public bool pauseAudio = true;

        [Tooltip("Disable every enabled Input System action while the panel is open so typing doesn't move the player. Re-enabled on close.")]
        public bool disableGameInput = true;

        [Tooltip("Action maps that stay enabled when game input is disabled (the UI map is needed by an InputSystemUIInputModule).")]
        public string[] inputMapsToKeepEnabled = { "UI" };

        [Header("Form")]
        [Tooltip("Show a 'Your name' field. The name is remembered between sessions.")]
        public bool askForPlayerName = true;

        [Tooltip("Require the player to pick a category before sending.")]
        public bool requireCategory = true;

        [Tooltip("Maximum feedback text length. Discord embeds allow up to 4096 characters.")]
        [Range(200, 4000)]
        public int maxTextLength = 3000;

        public List<FeedbackCategory> categories = new List<FeedbackCategory>
        {
            new FeedbackCategory("Bug", new Color(0.93f, 0.26f, 0.27f), "\U0001F41E"),
            new FeedbackCategory("Feedback", new Color(0.35f, 0.4f, 0.95f), "\U0001F4AC"),
            new FeedbackCategory("Idea", new Color(0.98f, 0.75f, 0.18f), "\U0001F4A1"),
        };

        [Header("Attachments")]
        [Range(50, 100)]
        public int jpgQuality = 85;

        [Tooltip("Screenshots larger than this (in pixels, longest side) are scaled down. 0 = never scale.")]
        public int maxImageSize = 1920;

        [Tooltip("Attach the most recent log messages as log.txt.")]
        public bool includeLogs = true;

        [Range(20, 1000)]
        public int logLines = 300;

        [Tooltip("Include stack traces for errors, exceptions and asserts in log.txt.")]
        public bool includeStackTraces = true;

        [Tooltip("Show game context values (from IFeedbackContextProvider / FeedbackManager.AddContext) as fields in the Discord post. They are always included in context.json.")]
        public bool showGameContextInEmbed = true;

        [Header("Reliability")]
        [Tooltip("If a report can't be sent (offline, Discord down), save it to disk and retry on the next launch.")]
        public bool queueFailedSubmissions = true;

        [Header("Advanced")]
        [Tooltip("Optional PanelSettings to use instead of the built-in one (for example to share your game's theme).")]
        public PanelSettings panelSettingsOverride;

        [Tooltip("Optional extra style sheet applied on top of the default panel styles.")]
        public StyleSheet extraStyleSheet;

        /// <summary>
        /// Set at runtime to use a different webhook than the one stored in the asset
        /// (for example one read from a command-line argument or a file next to the build).
        /// </summary>
        public static string WebhookUrlOverride { get; set; }

        public string EffectiveWebhookUrl =>
            string.IsNullOrWhiteSpace(WebhookUrlOverride) ? webhookUrl?.Trim() : WebhookUrlOverride.Trim();

        public bool IsEnabledForThisBuild
        {
            get
            {
                if (Application.isEditor) return enabledInEditor;
                return !developmentBuildsOnly || Debug.isDebugBuild;
            }
        }

        public static FeedbackSettings Load() => Resources.Load<FeedbackSettings>(ResourcesPath);

        /// <summary>F8 on keyboard, L3 + R3 (in either order) on gamepad.</summary>
        public static InputAction CreateDefaultToggleAction()
        {
            var action = new InputAction("Toggle Feedback Panel", InputActionType.Button);
            action.AddBinding("<Keyboard>/f8");
            // modifiersOrder=2 (Unordered): the two buttons can be pressed in any order.
            action.AddCompositeBinding("OneModifier(modifiersOrder=2)")
                .With("Modifier", "<Gamepad>/leftStickPress")
                .With("Binding", "<Gamepad>/rightStickPress");
            return action;
        }

        /// <summary>A copy of the configured hotkey for runtime use, or the default if none is set up.</summary>
        internal InputAction CreateToggleActionInstance()
        {
            if (toggleAction == null || toggleAction.bindings.Count == 0) return CreateDefaultToggleAction();

            // Clone so enabling it at runtime never touches the asset.
            var clone = toggleAction.Clone();
            clone.Rename("Toggle Feedback Panel");
            return clone;
        }

        public static bool IsValidWebhookUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            url = url.Trim();
            return url.StartsWith("https://discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://discordapp.com/api/webhooks/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://canary.discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://ptb.discord.com/api/webhooks/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
