using System;
using System.Collections.Generic;

[Serializable]
public sealed class ProgressData
{
    // Missing required JSON fields must fail validation. Version 1 is migrated
    // explicitly so existing completed-level records survive the bank upgrade.
    public int version;
    public List<string> completedLevelIds;
    public List<BankedSpecial> bankedSpecials;

    public static ProgressData Empty() => new ProgressData
    {
        version = 2, completedLevelIds = new List<string>(), bankedSpecials = new List<BankedSpecial>()
    };

    public ProgressData Copy() => new ProgressData
    {
        version = version,
        completedLevelIds = new List<string>(completedLevelIds),
        bankedSpecials = bankedSpecials.ConvertAll(piece => piece.Copy())
    };

    public bool Contains(string id) => completedLevelIds.Contains(id);
    public void Complete(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A level needs a permanent ID.");
        if (!Contains(id)) completedLevelIds.Add(id);
    }

    public void Validate()
    {
        if (version != 1 && version != 2) throw new InvalidOperationException($"Unsupported save version {version}.");
        if (completedLevelIds == null) throw new ArgumentException("Completed level list is missing.");
        foreach (var id in completedLevelIds)
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An empty level ID was found.");
        completedLevelIds = new List<string>(new HashSet<string>(completedLevelIds));
        if (version == 1)
        {
            bankedSpecials = new List<BankedSpecial>();
            version = 2;
        }
        if (bankedSpecials == null) throw new ArgumentException("Special bank is missing.");
        var bankIds = new HashSet<string>();
        foreach (var piece in bankedSpecials)
        {
            if (piece == null) throw new ArgumentException("A banked special is missing.");
            piece.Validate();
            if (!bankIds.Add(piece.id)) throw new ArgumentException("Duplicate banked special ID.");
        }
    }
}
