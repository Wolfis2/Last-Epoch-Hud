using System.Linq;
using UnityEngine;
using static UnityEngine.GUI;
using MelonLoader;
using Mod.Cheats;
using Mod.Cheats.ESP;
using Mod.Cheats.Inventory;
using TrackerRuntime = Mod.Cheats.CooldownTracker.CooldownTracker;
using CooldownTrackerTheme = Mod.Cheats.CooldownTracker.Theme;
using Mod.Game;
using Mod.Utils;

namespace Mod
{
	internal class Menu
	{
		private static bool guiVisible = false;
		private const float resizeGripSize = 20.0f;
		private static bool isResizing = false;
		public static bool npcDrawingsDropdown = false;
		public static bool npcClassificationsDropdown = false;
		public static bool itemDrawingsDropdown = false;
		public static bool antiIdleSubDropdown = false;
		public static bool dpsMeterSubDropdown = false;
		public static bool specialsSubDropdown = false; // Placeholder for future per-special options
		private static bool s_hasAppliedInputBlockState;
		private static bool s_lastAppliedInputBlockState;
		private static bool s_hasAppliedMouseBlockState;
		private static bool s_lastAppliedMouseBlockState;
		private static int s_lastMenuToggleFrame = -1;
		private static int s_selectedTab = 0;
		private static readonly GUIContent[] s_tabLabels = new GUIContent[]
		{
			new GUIContent("ESP"),
			new GUIContent("Automation"),
			new GUIContent("Inventory"),
			new GUIContent("Cooldowns"),
			new GUIContent("Gameplay"),
			new GUIContent("Risky / Debug")
		};
		private static readonly Vector2[] s_tabScrollPositions = new Vector2[s_tabLabels.Length];
#if DEBUG
		public static bool debugToolsDropdown = false;
#endif

		public static void DrawModWindow(int windowID)
		{
			GUILayout.BeginVertical();

			DrawTabBar();
			GUILayout.Space(4f);

			s_tabScrollPositions[s_selectedTab] = GUILayout.BeginScrollView(
				s_tabScrollPositions[s_selectedTab],
				GUILayout.ExpandHeight(true));

			switch (s_selectedTab)
			{
				case 0:
					DrawEspTab();
					break;
				case 1:
					DrawAutomationTab();
					break;
				case 2:
					DrawInventoryTab();
					break;
				case 3:
					DrawCooldownsTab();
					break;
				case 4:
					DrawGameplayTab();
					break;
				default:
					DrawRiskyAndDebugTab();
					break;
			}

			GUILayout.EndScrollView();

			GUILayout.EndVertical();

			Rect resizeGripRect = new Rect(
				windowRect.width - resizeGripSize, windowRect.height - resizeGripSize, resizeGripSize, resizeGripSize);
			Box(resizeGripRect, "");

			Rect closeRect = new Rect(windowRect.width - 30f, 1f, 24f, 22f);
			if (GUI.Button(closeRect, "X", CooldownTrackerTheme.Button(10, danger: true)))
				ToggleMenu("Close button");

			DragWindow(new Rect(0, 0, windowRect.width - 34f, 24f));

			ProcessResizing(resizeGripRect, windowID);

			Event currentEvent = Event.current;
			if (currentEvent != null && currentEvent.isMouse &&
				new Rect(0f, 0f, windowRect.width, windowRect.height).Contains(currentEvent.mousePosition) &&
				currentEvent.type != EventType.Used)
				currentEvent.Use();
		}

		private static void DrawTabBar()
		{
			GUILayout.BeginHorizontal();
			for (int i = 0; i < s_tabLabels.Length; i++)
			{
				bool isSelected = s_selectedTab == i;
				Color prevColor = GUI.color;
				GUI.color = Color.white;

				bool pressed = GUILayout.Toggle(isSelected, s_tabLabels[i],
					CooldownTrackerTheme.Button(10, selected: isSelected), GUILayout.Height(26f));
				GUI.color = prevColor;

				if (pressed && !isSelected)
				{
					s_selectedTab = i;
				}
			}

			GUILayout.EndHorizontal();
		}

		private static void DrawEspTab()
		{
			GUI.enabled = true;

			specialsSubDropdown = DrawSubmenuToggle(specialsSubDropdown, "Specials");
			if (specialsSubDropdown)
			{
					Settings.espShowLootLizards = DrawOptionToggle(Settings.espShowLootLizards, "Show Loot Lizards");
					Settings.espShowChampions = DrawOptionToggle(Settings.espShowChampions, "Show Champions");
					Settings.espShowOmens = DrawOptionToggle(Settings.espShowOmens, "Show Omens");
					Settings.espShowChests = DrawOptionToggle(Settings.espShowChests, "Show Chests");
					Settings.espShowShrines = DrawOptionToggle(Settings.espShowShrines, "Show Shrines");
					Settings.espShowRunePrisons = DrawOptionToggle(Settings.espShowRunePrisons, "Show Rune Prisons");

				GUILayout.Space(6f);
					Settings.showESPLines = DrawOptionToggle(Settings.showESPLines, "Show ESP Lines");
					Settings.showESPLabels = DrawOptionToggle(Settings.showESPLabels, "Show ESP Labels");

				GUILayout.Label("Chest ESP Vertical Cull (m): " + Settings.espVerticalCullMeters.ToString("F0"));
				Settings.espVerticalCullMeters = GUILayout.HorizontalSlider(Settings.espVerticalCullMeters, 0f, 200f);
			}

			npcDrawingsDropdown = DrawSubmenuToggle(npcDrawingsDropdown, "NPC Alignment");
			if (npcDrawingsDropdown)
			{
				foreach (KeyValuePair<string, bool> entry in Settings.npcDrawings)
				{
					bool result = DrawOptionToggle(entry.Value, entry.Key);
					if (result != entry.Value)
					{
						Settings.npcDrawings[entry.Key] = result;
					}
				}
			}

			npcClassificationsDropdown = DrawSubmenuToggle(npcClassificationsDropdown, "NPC Rarity");
			if (npcClassificationsDropdown)
			{
				foreach (KeyValuePair<string, bool> entry in Settings.npcClassifications)
				{
					bool result = DrawOptionToggle(entry.Value, entry.Key);
					if (result != entry.Value)
					{
						Settings.npcClassifications[entry.Key] = result;
					}
				}
			}

			itemDrawingsDropdown = DrawSubmenuToggle(itemDrawingsDropdown, "Item Filters");
			if (itemDrawingsDropdown)
			{
				bool lootFilterEnabled = Settings.useLootFilter;
				Settings.useLootFilter = DrawOptionToggle(Settings.useLootFilter, "Use Loot Filter");
				if (lootFilterEnabled)
				{
					GUI.enabled = false;
				}

				foreach (KeyValuePair<string, bool> entry in Settings.itemDrawings)
				{
					if (!lootFilterEnabled)
					{
						bool result = DrawOptionToggle(entry.Value, entry.Key);
						if (result != entry.Value)
						{
							Settings.itemDrawings[entry.Key] = result;
						}
					}
				}

				GUI.enabled = true;
			}

			GUILayout.Space(6f);
			GUILayout.Label("Draw Distance: " + Settings.drawDistance.ToString("F1"));
			Settings.drawDistance = GUILayout.HorizontalSlider(Settings.drawDistance, 0.0f, 300.0f);
		}

		private static void DrawAutomationTab()
		{
			GUI.enabled = true;

			Settings.useAutoPot = DrawOptionToggle(Settings.useAutoPot, "Auto HP Pot");
			if (Settings.useAutoPot)
			{
				GUILayout.Label("Auto HP Pot Threshold %: " + Settings.autoHealthPotion.ToString("F1"));
				Settings.autoHealthPotion = GUILayout.HorizontalSlider(Settings.autoHealthPotion, 0.0f, 100.0f);

				GUILayout.Label("Auto HP Pot Cooldown: " + Settings.autoPotionCooldown.ToString("F1") + "s");
				Settings.autoPotionCooldown = GUILayout.HorizontalSlider(Settings.autoPotionCooldown, 0.1f, 5.0f);
			}

			Settings.useAutoDisconnect = DrawOptionToggle(Settings.useAutoDisconnect, "Auto Disconnect on Low HP");
			if (Settings.useAutoDisconnect)
			{
				GUILayout.Label("Auto Disconnect Threshold %: " + Settings.autoDisconnectHealthPercent.ToString("F1"));
				Settings.autoDisconnectHealthPercent = GUILayout.HorizontalSlider(Settings.autoDisconnectHealthPercent, 0.0f, 100.0f);

				GUILayout.Label("Auto Disconnect Cooldown: " + Settings.autoDisconnectCooldownSeconds.ToString("F0") + "s");
				Settings.autoDisconnectCooldownSeconds = GUILayout.HorizontalSlider(Settings.autoDisconnectCooldownSeconds, 1f, 60f);

				Settings.autoDisconnectOnlyWhenNoPotions = DrawOptionToggle(Settings.autoDisconnectOnlyWhenNoPotions, "Only Disconnect When Out of Potions");
			}

			dpsMeterSubDropdown = DrawSubmenuToggle(dpsMeterSubDropdown, "DPS Meter");
			if (dpsMeterSubDropdown)
			{
				bool wasEnabled = Settings.enableDpsMeter;
				Settings.enableDpsMeter = DrawOptionToggle(Settings.enableDpsMeter, "Enable DPS Meter Overlay");
				Settings.enableDpsMeterOnlineRaw = DrawOptionToggle(
					Settings.enableDpsMeterOnlineRaw,
					"Allow Online Raw Source");
				Settings.dpsMeterPanelLocked = DrawOptionToggle(
					Settings.dpsMeterPanelLocked,
					"Lock DPS Panel Position/Size");
				Settings.enableDamageNumberDiagnostics = DrawOptionToggle(
					Settings.enableDamageNumberDiagnostics,
					"Enable DamageNumber Diagnostics (Verbose Logs)");
				if (wasEnabled && !Settings.enableDpsMeter)
				{
					DpsMeter.Reset();
				}

				if (Settings.enableDpsMeter)
				{
					GUILayout.Label("DPS Window (s): " + Settings.dpsMeterWindowSeconds.ToString("F1"));
					Settings.dpsMeterWindowSeconds = GUILayout.HorizontalSlider(Settings.dpsMeterWindowSeconds, 1f, 20f);

					Settings.dpsMeterAutoReset = DrawOptionToggle(Settings.dpsMeterAutoReset, "Auto Reset After Inactivity");
					if (Settings.dpsMeterAutoReset)
					{
						GUILayout.Label("Inactivity Reset (s): " + Settings.dpsMeterInactivityResetSeconds.ToString("F1"));
						Settings.dpsMeterInactivityResetSeconds = GUILayout.HorizontalSlider(Settings.dpsMeterInactivityResetSeconds, 2f, 60f);
					}

					if (GUILayout.Button("Reset DPS Stats"))
					{
						DpsMeter.Reset();
					}
					if (GUILayout.Button("Reset DPS Panel Layout"))
					{
						DpsMeter.ResetPanelLayout();
					}

					if (!ObjectManager.IsOfflineMode() && Settings.enableDpsMeterOnlineRaw)
					{
						GUILayout.Space(4f);
						GUILayout.Label("Online Ownership Filter");
						if (GUILayout.Button("Filter Mode: " + DescribeDpsFilterMode(Settings.dpsMeterOnlineFilterMode)))
						{
							Settings.dpsMeterOnlineFilterMode = (Settings.dpsMeterOnlineFilterMode + 1) % 3;
						}

						GUILayout.Label("Near Radius (incoming bias): " + Settings.dpsMeterNearPlayerMeters.ToString("F1") + "m");
						Settings.dpsMeterNearPlayerMeters = GUILayout.HorizontalSlider(Settings.dpsMeterNearPlayerMeters, 0.5f, 6f);

						float minFar = Mathf.Max(Settings.dpsMeterNearPlayerMeters + 0.2f, 0.7f);
						GUILayout.Label("Far Radius (outgoing bias): " + Settings.dpsMeterFarPlayerMeters.ToString("F1") + "m");
						Settings.dpsMeterFarPlayerMeters = GUILayout.HorizontalSlider(Settings.dpsMeterFarPlayerMeters, minFar, 12f);

						GUILayout.Label("HP Drop Correlation Window: " + Settings.dpsMeterHpDropCorrelationMs.ToString("F0") + "ms");
						Settings.dpsMeterHpDropCorrelationMs = GUILayout.HorizontalSlider(Settings.dpsMeterHpDropCorrelationMs, 50f, 1000f);
					}
				}

				if (!Settings.dpsMeterPanelLocked)
				{
					GUILayout.Label("DPS panel unlocked: drag title to move, bottom-right grip to resize.");
				}
				if (!ObjectManager.IsOfflineMode() && !Settings.enableDpsMeterOnlineRaw)
				{
					GUILayout.Label("Online meter disabled. Enable 'Online Raw Source' to collect from damage-number text.");
				}
				if (!ObjectManager.IsOfflineMode() && Settings.enableDpsMeterOnlineRaw)
				{
					GUILayout.Label("Online Raw can be filtered by proximity + local HP-drop correlation.");
				}
				if (Settings.enableDamageNumberDiagnostics)
				{
					GUILayout.Label("DamageNumber diagnostics are active. Check Melon logs for renderer summaries.");
				}
			}
		}

		private static void DrawInventoryTab()
		{
			GUI.enabled = true;
			GUILayout.Label("Inventory buttons are added to the game's inventory panel.");
			GUILayout.Label("Stash/vendor-from-anywhere features may conflict with online play; use at your own risk.");
			GUILayout.Space(6f);

			DrawInventoryToggle("Show STASH button", Prefs.ShowStash);
			DrawInventoryToggle("Show STASH ALL button", Prefs.ShowStashAll);
			DrawInventoryToggle("Show VENDOR button (off by default)", Prefs.ShowVendor);
			DrawInventoryToggle("Show Quick Teleport menu", Prefs.ShowTeleport);
			DrawInventoryToggle("Inventory debug logging", Prefs.DebugLog);
			if (Prefs.DebugLog.Value)
			{
				GUILayout.Space(6f);
				GUILayout.Label("Quick Teleport route (diagnostic)");
				int route = GUILayout.SelectionGrid(TravelService.RouteIndex, TravelService.RouteNames, 2,
					GUILayout.Height(54f));
				if (route != TravelService.RouteIndex)
				{
					TravelService.RouteIndex = route;
					MelonLogger.Msg($"[LEHud] Quick teleport diagnostic route: {TravelService.RouteNames[route]}");
				}
			}
		}

		private static void DrawInventoryToggle(string label, MelonPreferences_Entry<bool> entry)
		{
			bool value = DrawOptionToggle(entry.Value, label);
			if (value == entry.Value)
				return;

			entry.Value = value;
			InventoryUi.ApplyVisibility();
			Prefs.Save();
		}

		private static bool DrawSubmenuToggle(bool selected, string label)
		{
			return GUILayout.Toggle(selected, label,
				CooldownTrackerTheme.Button(10, selected: selected), GUILayout.Height(26f));
		}

		private static bool DrawOptionToggle(bool selected, string label)
		{
			GUIStyle rowStyle = CooldownTrackerTheme.OptionRow(selected);
			Rect row = GUILayoutUtility.GetRect(GUIContent.none, rowStyle,
				GUILayout.ExpandWidth(true), GUILayout.Height(25f));
			if (GUI.Button(row, GUIContent.none, rowStyle))
				selected = !selected;

			CooldownTrackerTheme.Text9(new Rect(row.x + 9f, row.y, row.width - 68f, row.height),
				label, selected ? CooldownTrackerTheme.TextHi : CooldownTrackerTheme.Text, 10);
			Rect badge = new Rect(row.xMax - 51f, row.y + 3f, 42f, row.height - 6f);
			GUI.Button(badge, selected ? "ON" : "OFF", CooldownTrackerTheme.Button(8, selected: selected));
			return selected;
		}

		private static void DrawCooldownsTab()
		{
			GUI.enabled = true;
			float height = TrackerRuntime.SettingsContentHeight;
			Rect hostRect = GUILayoutUtility.GetRect(
				Mathf.Max(320f, windowRect.width - 28f), height, GUILayout.ExpandWidth(true));
			TrackerRuntime.DrawSettingsTab(hostRect);
		}

		private static void DrawGameplayTab()
		{
			GUI.enabled = true;

			bool previousRemoveFog = Settings.removeFog;
			Settings.removeFog = DrawOptionToggle(Settings.removeFog, "Remove Fog");
			if (Settings.removeFog != previousRemoveFog)
			{
				GameMods.FogRemover();
			}

			Settings.cameraZoomUnlock = DrawOptionToggle(Settings.cameraZoomUnlock, "Camera Zoom Unlock");
			Settings.minimapZoomUnlock = DrawOptionToggle(Settings.minimapZoomUnlock, "Minimap Zoom Unlock");
			Settings.mapHack = DrawOptionToggle(Settings.mapHack, "Map Hack (Boost RevealRadius 14,000%)");

			bool previousPlayerLantern = Settings.playerLantern;
			Settings.playerLantern = DrawOptionToggle(Settings.playerLantern, "Player Lantern");
			if (Settings.playerLantern != previousPlayerLantern)
			{
				GameMods.playerLantern();
			}

			Settings.blockMenuInputWhenOpen = DrawOptionToggle(
				Settings.blockMenuInputWhenOpen,
				"Block Game Input While Menu Open (Keyboard + Mouse)");

			GUILayout.Space(10f);

			Color prevColor = GUI.color;
			GUI.color = Color.green;
			GUILayout.Label("Radar Monster Type Filters:");
			GUI.color = prevColor;
			Settings.showWhiteMonsters = DrawOptionToggle(Settings.showWhiteMonsters, "Show White Monsters");
			Settings.showMagicMonsters = DrawOptionToggle(Settings.showMagicMonsters, "Show Magic Monsters");
			Settings.showRareMonsters = DrawOptionToggle(Settings.showRareMonsters, "Show Rare Monsters");
			Settings.showUniqueMonsters = DrawOptionToggle(Settings.showUniqueMonsters, "Show Unique Monsters");
			Settings.showBossMonsters = DrawOptionToggle(Settings.showBossMonsters, "Show Boss Monsters");

			GUILayout.Space(6f);
			GUILayout.Label("Marker size and color (#RRGGBB)");
			DrawRarityMarkerStyle("Normal", ref Settings.minimapNormalCircleSize, ref Settings.minimapNormalCircleColor);
			DrawRarityMarkerStyle("Magic", ref Settings.minimapMagicCircleSize, ref Settings.minimapMagicCircleColor);
			DrawRarityMarkerStyle("Rare", ref Settings.minimapRareCircleSize, ref Settings.minimapRareCircleColor);
			DrawRarityMarkerStyle("Unique", ref Settings.minimapUniqueCircleSize, ref Settings.minimapUniqueCircleColor);
			DrawRarityMarkerStyle("Boss", ref Settings.minimapBossCircleSize, ref Settings.minimapBossCircleColor);
			GUILayout.Label("Marker opacity: " + Settings.minimapCircleOpacity.ToString("F2"));
			Settings.minimapCircleOpacity = GUILayout.HorizontalSlider(Settings.minimapCircleOpacity, 0.1f, 1f);

			GUILayout.Space(6f);
			GUILayout.Label("Radar status: " + MinimapEnemyCircles.lastDebugInfo);
			GUILayout.Label("Fullscreen scale correction: " + Settings.minimapFullscreenScaleCorrection.ToString("F3"));
			Settings.minimapFullscreenScaleCorrection = GUILayout.HorizontalSlider(
				Settings.minimapFullscreenScaleCorrection, 0.01f, 1f);
			GUILayout.Label("Fullscreen radar offset X: " + Settings.minimapFullscreenOffsetX.ToString("F0"));
			Settings.minimapFullscreenOffsetX = GUILayout.HorizontalSlider(
				Settings.minimapFullscreenOffsetX, -500f, 500f);
			GUILayout.Label("Fullscreen radar offset Y: " + Settings.minimapFullscreenOffsetY.ToString("F0"));
			Settings.minimapFullscreenOffsetY = GUILayout.HorizontalSlider(
				Settings.minimapFullscreenOffsetY, -500f, 500f);
		}

		private static void DrawRarityMarkerStyle(string label, ref float size, ref string colorHex)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, GUILayout.Width(54f));
			GUILayout.Label(size.ToString("F0"), GUILayout.Width(22f));
			size = GUILayout.HorizontalSlider(size, 2f, 20f, GUILayout.Width(96f));

			Color swatch = Color.white;
			if (ColorUtility.TryParseHtmlString("#" + colorHex, out Color parsed)) swatch = parsed;
			Color previous = GUI.color;
			GUI.color = swatch;
			GUILayout.Box(GUIContent.none, GUILayout.Width(18f), GUILayout.Height(18f));
			GUI.color = previous;
			GUILayout.Label("#", GUILayout.Width(10f));
			string edited = GUILayout.TextField(colorHex, 6, GUILayout.Width(58f));
			colorHex = FilterHexColor(edited);
			GUILayout.EndHorizontal();
		}

		private static string FilterHexColor(string value)
		{
			var result = new System.Text.StringBuilder(6);
			foreach (char c in value)
			{
				if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
					result.Append(char.ToUpperInvariant(c));
				if (result.Length == 6) break;
			}
			return result.ToString();
		}

		private static void DrawRiskyAndDebugTab()
		{
			GUI.enabled = true;

			GUILayout.Label("These options are provided at your own risk.");
			GUILayout.Space(6f);

			GUILayout.Label("TimeScale: " + Settings.timeScale.ToString("F1"));
			Settings.timeScale = GUILayout.HorizontalSlider(Settings.timeScale, 0.1f, 6.0f);

			GUILayout.Space(6f);
			if (!ObjectManager.IsOfflineMode())
			{
				GUILayout.Label("Allow Any Waypoint: unavailable in online mode");
			}
			else
			{
				Settings.useAnyWaypoint = DrawOptionToggle(Settings.useAnyWaypoint, "Allow Any Waypoint");
			}

			GUILayout.Space(10f);
			antiIdleSubDropdown = DrawSubmenuToggle(antiIdleSubDropdown, "Anti-Idle");
			if (antiIdleSubDropdown)
			{
				Settings.useSimpleAntiIdle = DrawOptionToggle(Settings.useSimpleAntiIdle, "Enable Anti-Idle");
				if (Settings.useSimpleAntiIdle)
				{
					GUILayout.Label("Pulse Interval (s): " + Settings.simpleAntiIdleInterval.ToString("F0"));
					Settings.simpleAntiIdleInterval = GUILayout.HorizontalSlider(Settings.simpleAntiIdleInterval, 60f, 900f);
					Settings.forceIsIdleFalseFallback = DrawOptionToggle(
						Settings.forceIsIdleFalseFallback,
						"Force IsIdle FALSE Fallback (high risk)");

					Settings.suppressKeepAliveOnActivity = DrawOptionToggle(Settings.suppressKeepAliveOnActivity, "Suppress When Actively Playing");
					if (Settings.suppressKeepAliveOnActivity)
					{
						GUILayout.Label("Activity Suppression (s): " + Settings.activitySuppressionSeconds.ToString("F0"));
						Settings.activitySuppressionSeconds = GUILayout.HorizontalSlider(Settings.activitySuppressionSeconds, 5f, 300f);

						GUILayout.Label("Scene Change Suppression (s): " + (Settings.sceneChangeSuppressionSeconds <= 0f ? "Disabled" : Settings.sceneChangeSuppressionSeconds.ToString("F0")));
						Settings.sceneChangeSuppressionSeconds = GUILayout.HorizontalSlider(Settings.sceneChangeSuppressionSeconds, 0f, 300f);
					}
				}
			}

#if DEBUG
			GUILayout.Space(10f);
			DrawDebugToolsSection();
#endif
		}

#if DEBUG
		private static void DrawDebugToolsSection()
		{
			debugToolsDropdown = DrawSubmenuToggle(debugToolsDropdown, "DEBUG Tools");
			if (!debugToolsDropdown)
			{
				return;
			}

			Settings.enableNetworkDiagnostics = DrawOptionToggle(Settings.enableNetworkDiagnostics, "Enable Network Diagnostics (Verbose)");
			if (Settings.enableNetworkDiagnostics)
			{
				GUILayout.Label("Captures deep ClientNetworkService breadcrumbs during connect/load troubleshooting.");
			}

			Settings.debugEnableDiagnostics = DrawOptionToggle(Settings.debugEnableDiagnostics, "Enable Diagnostics");
			if (!Settings.debugEnableDiagnostics)
			{
				return;
			}

			Settings.debugShowLocalPlayerPanel = DrawOptionToggle(Settings.debugShowLocalPlayerPanel, "Show Local Player Panel");
			Settings.debugShowLocalPlayerWorldLabel = DrawOptionToggle(Settings.debugShowLocalPlayerWorldLabel, "Show Local Player World Label");
			Settings.debugDrawAllManagerActors = DrawOptionToggle(Settings.debugDrawAllManagerActors, "Draw All ActorManager Actors (No Sorting)");
			Settings.debugDrawAllGroundItems = DrawOptionToggle(Settings.debugDrawAllGroundItems, "Draw All GroundItemVisuals");
			Settings.debugDrawAllGroundGold = DrawOptionToggle(Settings.debugDrawAllGroundGold, "Draw All GroundGoldVisuals");
			Settings.debugDrawManagerLines = DrawOptionToggle(Settings.debugDrawManagerLines, "Draw Debug Lines To Targets");
			Settings.debugIgnoreDistanceCulling = DrawOptionToggle(Settings.debugIgnoreDistanceCulling, "Ignore Draw Distance Culling");

			GUILayout.Label("Debug Max Entries/System: " + Settings.debugMaxEntriesPerSystem.ToString());
			var debugMax = GUILayout.HorizontalSlider(Settings.debugMaxEntriesPerSystem, 10f, 500f);
			Settings.debugMaxEntriesPerSystem = Mathf.RoundToInt(debugMax);
		}
#endif

		private static void ProcessResizing(Rect resizeGripRect, int windowID)
		{
			Event currentEvent = Event.current;
			switch (currentEvent.type)
			{
				case EventType.MouseDown:
					// Check if the mouse is within the resize grip area
					if (resizeGripRect.Contains(currentEvent.mousePosition))
					{
						currentEvent.Use(); // Mark the event as used
						isResizing = true; // Set a flag indicating that we're resizing
					}
					break;

				case EventType.MouseUp:
					isResizing = false; // Clear the resizing flag on mouse up
					break;

				case EventType.MouseDrag:
					if (isResizing)
					{
						// Directly adjust windowRect for resizing
						windowRect.width += currentEvent.delta.x;
						windowRect.height += currentEvent.delta.y;
						// Enforce minimum size constraints
						windowRect.width = Mathf.Max(windowRect.width, 320);
						windowRect.height = Mathf.Max(windowRect.height, 200);
						currentEvent.Use();
					}
					break;
			}
		}

		private static string DescribeDpsFilterMode(int mode)
		{
			return mode switch
			{
				1 => "Likely Outgoing",
				2 => "Likely Incoming",
				_ => "All Visible"
			};
		}

		public static Rect windowRect = new Rect(20, 20, 500, 700);
		public static bool IsCooldownsTabActive => guiVisible && s_selectedTab == 3;
		public static float CooldownTabScrollOffset => s_tabScrollPositions[3].y;

		public static void OnGUI()
		{
			var currentEvent = Event.current;
			if (currentEvent.type == EventType.KeyDown
				&& (currentEvent.keyCode == KeyCode.Insert || currentEvent.keyCode == KeyCode.F10))
			{
				var key = currentEvent.keyCode;
				currentEvent.Use();
				ToggleMenu(key.ToString());
			}

			if (guiVisible)
			{
				GUISkin previousSkin = GUI.skin;
				try
				{
					CooldownTrackerTheme.ApplyHudSkin();
					windowRect = GUI.Window(0, windowRect, (WindowFunction)DrawModWindow, "LEHUD");
				}
				finally
				{
					GUI.skin = previousSkin;
				}
			}
		}

		public static void OnUpdate()
		{
			if (Input.GetKeyDown(KeyCode.Insert))
			{
				ToggleMenu("Insert");
			}
		else if (Input.GetKeyDown(KeyCode.F10))
			{
				ToggleMenu("F10");
			}

			// Optional input-blocking: blocks gameplay keyboard + mouse while menu is visible.
			bool shouldBlockGameInput = (Settings.blockMenuInputWhenOpen && guiVisible) ||
				TrackerRuntime.ShouldBlockGameInput;
			if (!s_hasAppliedInputBlockState || s_lastAppliedInputBlockState != shouldBlockGameInput)
			{
				EpochInputManagerBridge.TrySetMenuInputBlocked(shouldBlockGameInput);
				s_lastAppliedInputBlockState = shouldBlockGameInput;
				s_hasAppliedInputBlockState = true;
			}

			bool pointerOverHud = IsPointerOverHud();
			bool shouldBlockMouse = shouldBlockGameInput || pointerOverHud;
			if (pointerOverHud || !s_hasAppliedMouseBlockState || s_lastAppliedMouseBlockState != shouldBlockMouse)
			{
				EpochInputManagerBridge.TrySetMouseInputBlocked(shouldBlockMouse, force: pointerOverHud);
				s_lastAppliedMouseBlockState = shouldBlockMouse;
				s_hasAppliedMouseBlockState = true;
			}

			// Debug key for auto-potion system (F12)
			if (Input.GetKeyDown(KeyCode.F12))
			{
				AutoPotion.LogDebugInfo();
			}

#if DEBUG
			// Debug key for actor/local-player correlation diagnostics (F11)
			if (Input.GetKeyDown(KeyCode.F11))
			{
				DebugDiagnostics.LogCorrelationSnapshot();
			}
#endif
		}

		private static bool IsPointerOverHud()
		{
			if (!guiVisible) return false;
			Vector3 mouse = Input.mousePosition;
			Vector2 guiMouse = new Vector2(mouse.x, Screen.height - mouse.y);
			return windowRect.Contains(guiMouse);
		}

		private static void ToggleMenu(string inputSource)
		{
			if (s_lastMenuToggleFrame == Time.frameCount)
				return;

			s_lastMenuToggleFrame = Time.frameCount;
			guiVisible = !guiVisible;
			MelonLogger.Msg($"[LEHud] Menu {(guiVisible ? "opened" : "closed")} via {inputSource}.");
			if (guiVisible)
			{
				AntiIdleSystem.OnMenuOpened();
				return;
			}

			SettingsConfig.ApplyToPreferencesFromSettings();
			SettingsConfig.Save();
			TrackerRuntime.Save();
			MelonLogger.Msg("[LEHud] Preferences Saved!");
			AntiIdleSystem.OnMenuClosed();
		}
	}
}
