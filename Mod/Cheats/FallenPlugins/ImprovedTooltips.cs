#nullable disable
#nullable enable
using System.Collections;
using HarmonyLib;
using Il2Cpp;
using Il2CppItemFiltering;
using Il2CppTMPro;
using MelonLoader;

namespace Mod.Cheats.FallenPlugins;

internal static class ImprovedTooltips
{
    private const string TooltipMarker = "\u200B\u200B\u200B";
    private static MelonPreferences_Category? s_category;
    public static MelonPreferences_Entry<bool>? ShowFullItemName { get; private set; }
    public static MelonPreferences_Entry<bool>? ShowLegendaryPotential { get; private set; }
    public static MelonPreferences_Entry<bool>? CompareStashItems { get; private set; }

    private static bool s_kgImprovementsLoaded;
    public static bool KgImprovementsLoaded => s_kgImprovementsLoaded;

    public static void Initialize()
    {
        if (s_category != null) return;

        s_kgImprovementsLoaded = MelonMod.RegisteredMelons.Any(m =>
            string.Equals(m.Info.Name, "kg_LastEpoch_Improvements", StringComparison.Ordinal));
        s_category = MelonPreferences.CreateCategory("ImprovedTooltips", "Improved Tooltips");
        s_category.SetFilePath("UserData/FallenImprovedTooltips.cfg");
        ShowFullItemName = s_category.CreateEntry("ShowFullItemName", true, "Use the complete item name on ground labels");
        ShowLegendaryPotential = s_category.CreateEntry("ShowLPOnGroundLabels", true, "Show LP or Weaver's Will on ground labels");
        CompareStashItems = s_category.CreateEntry("ShowLPComparison", true, "Compare unique items against stash copies");
    }

    public static void Save()
    {
        try { s_category?.SaveToFile(false); } catch { }
    }

    [HarmonyPatch(typeof(TooltipItemManager), "CreateTooltipContent")]
    private static class TooltipContentPatch
    {
        [HarmonyPrefix]
        private static void Prefix(TooltipItemManager __instance)
        {
            try
            {
                var item = __instance?.ActiveItem;
                if (item != null) AppendTooltipDetails(item);
            }
            catch (Exception e)
            {
                MelonLogger.Warning($"[LEHud] Improved Tooltips skipped an item: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(GroundItemLabel), "SetGroundTooltipText")]
    private static class GroundLabelPatch
    {
        [HarmonyPostfix]
        private static void Postfix(GroundItemLabel __instance)
        {
            try { MelonCoroutines.Start(UpdateGroundLabel(__instance)); }
            catch (Exception e) { MelonLogger.Warning($"[LEHud] Ground-label enhancement failed to start: {e.Message}"); }
        }
    }

    private static void AppendTooltipDetails(ItemDataUnpacked item)
    {
        item.LoreText ??= string.Empty;
        int markerIndex = item.LoreText.IndexOf(TooltipMarker, StringComparison.Ordinal);
        if (markerIndex >= 0)
            item.LoreText = item.LoreText.Substring(0, markerIndex).TrimEnd('\n', '\r', ' ');

        string additions = string.Empty;
        bool hadLore = !string.IsNullOrEmpty(item.LoreText);
        void Append(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            additions += (additions.Length == 0 ? (hadLore ? "\n\n" : string.Empty) : "\n\n") + text;
        }

        Rule? matchedRule = FindMatchingRule(item);
        if (matchedRule != null && (ItemList.isEquipment(item.itemType) || ItemList.isIdol(item.itemType)))
        {
            string description = matchedRule.GetRuleDescription();
            if (!string.IsNullOrWhiteSpace(description))
                Append($"<color=#E0E0E0>Filter Rule:</color> {description}");
        }

        if (item.isUniqueSetOrLegendary())
            Append(BuildOwnershipText(item));

        if (additions.Length > 0)
            item.LoreText += TooltipMarker + additions;
    }

    private static Rule? FindMatchingRule(ItemDataUnpacked item)
    {
        try
        {
            var manager = ItemFilterManager.Instance;
            var rules = manager?.Filter?.rules;
            if (rules == null) return null;

            int level = 0;
            try { level = PlayerFinder.getExperienceTracker()?.CurrentLevel ?? 0; } catch { }
            for (int i = rules.Count - 1; i >= 0; i--)
            {
                Rule rule = rules[i];
                if (rule != null && rule.isEnabled && rule.type.ToString() != "HIDE" && rule.Match(item, level))
                    return rule;
            }
        }
        catch { }
        return null;
    }

    private static string BuildOwnershipText(ItemDataUnpacked item)
    {
        bool isSet = item.isSet();
        bool isWeaver = item.weaversWill > 0;
        ItemDataUnpacked? stashMatch = FindMatchingStashItem(item, isWeaver);
        if (stashMatch == null)
            return "<i><color=#FFD700>[NEW - NOT IN STASH]</color></i>";
        if (isSet)
            return "<color=#00FF00>[OWNED - IN STASH]</color>";

        int currentValue = isWeaver ? item.weaversWill : item.getLegendaryPotentialTier();
        int stashValue = isWeaver ? stashMatch.weaversWill : stashMatch.legendaryPotential;
        string stat = isWeaver ? "WW" : "LP";
        string tint = isWeaver ? "#8D6EE8" : "#FF5555";
        string comparison = stashValue > currentValue
            ? $"<color=#FF5555>↓</color> (Stash has <color={tint}>{stat}:{stashValue}</color>)"
            : stashValue < currentValue
                ? $"<color=#53D769>↑</color> (Stash has <color={tint}>{stat}:{stashValue}</color>)"
                : $"<color=#68A7FF>=</color> (Stash has <color={tint}>{stat}:{stashValue}</color>)";
        return $"<color=#53D769>[OWNED]</color> - <color={tint}>[{stat}:{currentValue}]</color> {comparison}";
    }

    private static ItemDataUnpacked? FindMatchingStashItem(ItemDataUnpacked item, bool preferWeaver)
    {
        try
        {
            var containers = ItemContainersManager.Instance?.stash?.CurrentContainer?.containers;
            if (containers == null) return null;

            ItemDataUnpacked? best = null;
            int bestValue = -1;
            foreach (ItemContainer container in containers)
            {
                if (container?.content == null) continue;
                foreach (ItemContainerEntry entry in container.content)
                {
                    var data = entry?.data;
                    if (data == null || !data.isUniqueSetOrLegendary() || data.uniqueID != item.uniqueID) continue;
                    int value = preferWeaver ? data.weaversWill : data.legendaryPotential;
                    if (best == null || value > bestValue)
                    {
                        best = data.getAsUnpacked();
                        bestValue = value;
                    }
                }
            }
            return best;
        }
        catch { return null; }
    }

    private static IEnumerator UpdateGroundLabel(GroundItemLabel label)
    {
        yield return null;
        if (label == null) yield break;

        ItemDataUnpacked? item;
        try
        {
            item = label.getItemData();
            if (item == null || !item.isUniqueSetOrLegendary()) yield break;
        }
        catch { yield break; }

        TextMeshProUGUI text;
        try { text = label.itemText; }
        catch { yield break; }
        if (text == null) yield break;

        const string labelMarker = "\u200B\u200B\u200B";
        string current = text.text ?? string.Empty;
        if (current.Contains(labelMarker, StringComparison.Ordinal)) yield break;

        string baseName = current;
        string stats = string.Empty;
        string suffix;
        bool isSet = item.isSet();
        if (!s_kgImprovementsLoaded)
        {
            if (ShowFullItemName?.Value == true) baseName = item.FullName;
            if (!isSet && ShowLegendaryPotential?.Value == true)
                stats = item.weaversWill > 0 ? $" <color=#8D6EE8>[WW:{item.weaversWill}]</color>"
                    : $" <color=#FF5555>[LP:{item.legendaryPotential}]</color>";
        }

        ItemDataUnpacked? stashMatch = FindMatchingStashItem(item, item.weaversWill > 0);
        if (stashMatch == null) suffix = " <i><color=#FFD700>NEW</color></i>";
        else if (isSet) suffix = " <color=#53D769>[OWNED]</color>";
        else if (CompareStashItems?.Value == true)
        {
            int currentValue = item.weaversWill > 0 ? item.weaversWill : item.legendaryPotential;
            int stashValue = item.weaversWill > 0 ? stashMatch.weaversWill : stashMatch.legendaryPotential;
            suffix = stashValue > currentValue ? " <color=#FF5555>↓</color>"
                : stashValue < currentValue ? " <color=#53D769>↑</color>" : " <color=#68A7FF>=</color>";
        }
        else suffix = string.Empty;

        text.text = $"{baseName}{stats}{suffix}{labelMarker}";
        try { if (label.emphasized) text.text = text.text.ToUpperInvariant(); } catch { }
    }
}
