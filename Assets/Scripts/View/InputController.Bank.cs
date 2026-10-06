using System.Collections;
using UnityEngine;

public partial class InputController
{
    private CampaignProgress bankCampaign;
    private LevelDefinition bankLevel;
    private string pendingBankId;
    private string bankMessage = "";
    private bool bankOpen;
    private int bankPage;


    // Canvas HUD read-only bank state / commands. Game rules remain in this controller.
    public bool BankAvailable => bankCampaign != null && bankLevel != null && !levelEnded;
    public bool BankOpen => bankOpen;
    public bool BankPlacementPending => pendingBankId != null;
    public string BankMessage => bankMessage;
    public int BankCount => bankCampaign != null ? bankCampaign.BankedSpecials.Count : 0;
    public bool CanSaveSelectedSpecial => PlayerActionsReady && selectedPiece != null && IsSpecialPiece(selectedPiece) && pendingBankId == null;

    public BankedSpecial CurrentBankedSpecial
    {
        get
        {
            if (bankCampaign == null) return null;
            var entries = bankCampaign.BankedSpecials;
            if (entries.Count == 0) return null;
            if (bankPage < 0 || bankPage >= entries.Count) bankPage = 0;
            return entries[bankPage];
        }
    }

    public int CurrentBankPage => BankCount == 0 ? 0 : bankPage + 1;
    public bool CurrentBankedSpecialUsable
    {
        get
        {
            var entry = CurrentBankedSpecial;
            return entry != null && bankCampaign.CanUseBankedSpecial(bankLevel, entry.id);
        }
    }

    public int GetBankOwnedCount(SpecialType specialType)
    {
        if (bankCampaign == null) return 0;

        int count = 0;
        foreach (var entry in bankCampaign.BankedSpecials)
        {
            if (entry.special == specialType) count++;
        }
        return count;
    }

    public int GetBankUsableCount(SpecialType specialType)
    {
        if (bankCampaign == null || bankLevel == null) return 0;

        int count = 0;
        foreach (var entry in bankCampaign.BankedSpecials)
        {
            if (entry.special == specialType &&
                bankCampaign.CanUseBankedSpecial(bankLevel, entry.id))
            {
                count++;
            }
        }
        return count;
    }

    public bool SelectBankedSpecialType(SpecialType specialType)
    {
        if (!PlayerActionsReady || bankCampaign == null || bankLevel == null)
            return false;

        foreach (var entry in bankCampaign.BankedSpecials)
        {
            if (entry.special != specialType) continue;
            if (!bankCampaign.CanUseBankedSpecial(bankLevel, entry.id)) continue;
            return SelectBankedSpecial(entry.id);
        }

        bankMessage = GetBankOwnedCount(specialType) > 0
            ? "That saved special is locked in this level."
            : "You do not have that special saved yet.";
        return false;
    }

    public void ToggleBankPanel()
    {
        if (!PlayerActionsReady || !BankAvailable) return;
        ClearHint();
        lastClickedPiece = null;
        if (pendingBankId != null)
        {
            pendingBankId = null;
            bankMessage = "";
            bankOpen = false;
            return;
        }
        bankOpen = !bankOpen;
        bankMessage = "";
    }

    public void CloseBankPanel()
    {
        bankOpen = false;
        bankMessage = "";
    }

    public void NextBankedSpecial()
    {
        if (!PlayerActionsReady || bankCampaign == null) return;
        int count = bankCampaign.BankedSpecials.Count;
        if (count > 1) bankPage = (bankPage + 1) % count;
    }

    public bool UseCurrentBankedSpecial()
    {
        var entry = CurrentBankedSpecial;
        return entry != null && SelectBankedSpecial(entry.id);
    }

    private void ResetBankInteraction()
    {
        pendingBankId = null;
        bankMessage = "";
        bankOpen = false;
        bankPage = 0;
    }

    private bool PointerOverBank() => false;

    public bool SaveSelectedSpecial()
    {
        if (inputLocked || levelEnded || board == null || bankCampaign == null || selectedPiece == null) return false;
        var cell = board.GetCell(selectedPiece.X, selectedPiece.Y);
        if (!SpecialBankActions.TryBank(bankCampaign, bankLevel, cell, out var error))
        {
            bankMessage = "Could not save. Your special is still on the board. Try again.";
            Debug.LogWarning("[Special Bank] " + error, this);
            return false;
        }
        bankMessage = "Special saved! Locked until another level.";
        pendingBankId = null;
        bankOpen = false;
        Debug.Log($"[Special Bank] Saved a special from {bankLevel.name}. Locked in this level, including retries.", this);
        BeginBankResolution();
        return true;
    }

    public bool SelectBankedSpecial(string bankId)
    {
        if (inputLocked || levelEnded || bankCampaign == null || !bankCampaign.CanUseBankedSpecial(bankLevel, bankId)) return false;
        ClearHint();
        DeselectPiece();
        lastClickedPiece = null;
        pendingBankId = bankId;
        bankOpen = false;
        bankMessage = "Click a normal tile to place your special.";
        return true;
    }

    public bool PlaceBankedSpecial(PieceView target)
    {
        if (inputLocked || levelEnded || board == null || target == null || pendingBankId == null) return false;
        var cell = board.GetCell(target.X, target.Y);
        if (!cell.IsPlayable || cell.IsEmpty || cell.Piece.IsSpecial || !cell.Piece.IsColored)
        {
            bankMessage = "Choose a normal colored tile.";
            return false;
        }
        if (!SpecialBankActions.TryPlace(bankCampaign, bankLevel, pendingBankId, cell, out var error))
        {
            bankMessage = "Could not place. Your special is still in the bank. Try again.";
            Debug.LogWarning("[Special Bank] " + error, this);
            return false;
        }
        pendingBankId = null;
        bankMessage = "Special placed. It has been used from the bank.";
        Debug.Log($"[Special Bank] Placed a saved special in {bankLevel.name} at ({cell.X}, {cell.Y}).", this);
        BeginBankResolution();
        return true;
    }

    private void BeginBankResolution()
    {
        inputLocked = true;
        ClearHint();
        DeselectPiece();
        lastClickedPiece = null;
        // Show the placed color/special before any matching or clearing animation.
        boardView.RefreshBoard(board);
        StartCoroutine(ResolveBankAction());
    }

    private IEnumerator ResolveBankAction()
    {
        // Placement may create a match. Resolve it normally, with score/objective
        // events, but without spending a move or triggering a Hot Streak.
        var sequence = boardResolver.ResolveWithSequence(board, null, MoveSource.SpecialBank);
        yield return StartCoroutine(boardView.PlayResolutionSequence(sequence, popDuration, fallDuration, refillDuration));
        boardView.RefreshBoard(board);
        yield return StartCoroutine(FinishResolutionChain(sequence, "After special bank action"));
    }



    // Returns the next free row so hint text and the bank never overlap.

}
