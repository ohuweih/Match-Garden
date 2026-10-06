using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Match 3/Level Catalog")]
public sealed class LevelCatalog : ScriptableObject
{
    [Tooltip("Levels in play order. Each needs objectives; a move limit is optional.")]
    [SerializeField] private LevelDefinition[] levels = new LevelDefinition[0];
    public LevelDefinition[] CreateLevelList() => (LevelDefinition[])levels.Clone();

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (levels == null || levels.Length == 0) { errors.Add("Add at least one level to the catalog."); return errors; }
        var ids = new HashSet<string>();
        for (int i = 0; i < levels.Length; i++)
        {
            var level = levels[i];
            if (level == null) { errors.Add($"Catalog entry {i + 1} is empty."); continue; }
            if (string.IsNullOrWhiteSpace(level.LevelId)) errors.Add($"{level.name}: missing permanent level ID. Reimport the asset.");
            else if (!ids.Add(level.LevelId)) errors.Add($"{level.name}: duplicate level in the catalog.");
            var settings = level.CreateSettings();
            foreach (var error in LevelBuilder.Validate(settings)) errors.Add($"{level.name}: {error}");
            if (settings.Objectives == null || settings.Objectives.Length == 0) errors.Add($"{level.name}: campaign levels need an objective.");
        }
        return errors;
    }
}

public sealed class CampaignProgress
{
    private readonly LevelDefinition[] levels;
    private readonly ProgressFileStore store;
    private ProgressData data;
    public string LoadWarning => store.Warning;
    public bool CanSave => store.CanSave;
    public LevelDefinition LastLevel => levels[levels.Length - 1];
    public LevelDefinition NextUnfinished
    {
        get
        {
            foreach (var level in levels) if (!data.Contains(level.LevelId)) return level;
            return null;
        }
    }
    public bool AllComplete => NextUnfinished == null;
    public bool IsCompleted(LevelDefinition level) => level != null && Array.IndexOf(levels, level) >= 0 && data.Contains(level.LevelId);
    public int CompletedCount
    {
        get
        {
            int count = 0;
            foreach (var level in levels) if (data.Contains(level.LevelId)) count++;
            return count;
        }
    }

    public CampaignProgress(LevelCatalog catalog, ProgressFileStore store)
    {
        if (catalog == null) throw new ArgumentNullException(nameof(catalog));
        var errors = catalog.Validate();
        if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        levels = catalog.CreateLevelList();
        data = store.Load();
    }

    public IReadOnlyList<BankedSpecial> BankedSpecials => data.bankedSpecials.ConvertAll(piece => piece.Copy()).AsReadOnly();

    public bool CanUseBankedSpecial(LevelDefinition level, string bankId)
    {
        var piece = data.bankedSpecials.Find(entry => entry.id == bankId);
        return level != null && Array.IndexOf(levels, level) >= 0 && piece != null &&
            piece.sourceLevelId != level.LevelId && data.Contains(piece.sourceLevelId);
    }

    public bool TryBankSpecial(LevelDefinition level, PieceData piece, out string error)
    {
        error = null;
        if (level == null || Array.IndexOf(levels, level) < 0)
        { error = "This level is not in the campaign."; return false; }
        if (piece == null || !piece.IsSpecial)
        { error = "Only special pieces can be banked."; return false; }
        var candidate = data.Copy();
        candidate.bankedSpecials.Add(new BankedSpecial
        {
            id = Guid.NewGuid().ToString("N"), sourceLevelId = level.LevelId,
            color = piece.Color, special = piece.Special
        });
        if (!store.TrySave(candidate, out error)) return false;
        data = candidate;
        return true;
    }

    public bool TryUseBankedSpecial(LevelDefinition level, string bankId, out PieceData piece, out string error)
    {
        piece = null;
        error = null;
        if (!CanUseBankedSpecial(level, bankId))
        { error = "This special is locked or is no longer in the bank."; return false; }
        var candidate = data.Copy();
        var saved = candidate.bankedSpecials.Find(entry => entry.id == bankId);
        candidate.bankedSpecials.Remove(saved);
        if (!store.TrySave(candidate, out error)) return false;
        data = candidate;
        piece = new PieceData(saved.color, saved.special);
        return true;
    }


    public bool TryComplete(LevelDefinition level, out string error)
    {
        error = null;
        if (Array.IndexOf(levels, level) < 0) { error = "Level is not in this campaign."; return false; }
        if (data.Contains(level.LevelId)) return true;
        var candidate = data.Copy();
        candidate.Complete(level.LevelId);
        if (!store.TrySave(candidate, out error)) return false;
        data = candidate;
        return true;
    }
}
