using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class LevelIntroPresenter : MonoBehaviour
{
    private EditableLevelIntro hud;
    private Action onPlay;

    public void Show(string title, ObjectiveDefinition[] objectives, bool useMoveLimit, int moveLimit, Action playAction)
    {
        onPlay = playAction;
        EnsureEventSystem();

        if (!BindSceneIntro())
        {
            Debug.LogError("[Level Intro] EditableLevelIntro is missing or not fully configured.");
            return;
        }

        hud.introRoot.SetActive(true);
        hud.levelTitleText.text = title;
        hud.movesText.text = useMoveLimit ? $"{Mathf.Max(1, moveLimit)} MOVES" : "UNLIMITED MOVES";
        RebuildObjectives(objectives);

        hud.playButton.onClick.RemoveListener(Play);
        hud.playButton.onClick.AddListener(Play);
    }

    private bool BindSceneIntro()
    {
        if (hud != null) return hud.IsConfigured;
        hud = FindAnyObjectByType<EditableLevelIntro>(FindObjectsInactive.Include);
        return hud != null && hud.IsConfigured;
    }

    private void RebuildObjectives(ObjectiveDefinition[] objectives)
    {
        for (int i = hud.objectivesRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = hud.objectivesRoot.GetChild(i);
            if (child.gameObject != hud.objectiveCardTemplate)
                Destroy(child.gameObject);
        }

        if (objectives == null || objectives.Length == 0)
        {
            SetObjectiveCard(hud.objectiveCardTemplate, "Free Play");
            hud.objectiveCardTemplate.SetActive(true);
            return;
        }

        bool usedTemplate = false;
        foreach (ObjectiveDefinition objective in objectives)
        {
            if (objective == null) continue;

            GameObject card;
            if (!usedTemplate)
            {
                card = hud.objectiveCardTemplate;
                usedTemplate = true;
            }
            else
            {
                card = Instantiate(hud.objectiveCardTemplate, hud.objectivesRoot, false);
                card.name = "ObjectiveCard_Runtime";
            }

            SetObjectiveCard(card, FormatObjective(objective));
            card.SetActive(true);
        }

        hud.objectiveCardTemplate.SetActive(usedTemplate);
    }

    private static void SetObjectiveCard(GameObject card, string value)
    {
        TMP_Text text = card.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = value;
        else Debug.LogWarning($"[Level Intro] No TMP text found under '{card.name}'.", card);
    }

    private void Play()
    {
        Action callback = onPlay;
        onPlay = null;

        if (hud != null)
        {
            hud.playButton.onClick.RemoveListener(Play);
            hud.introRoot.SetActive(false);
        }

        callback?.Invoke();
    }

    private void OnDestroy()
    {
        onPlay = null;
        if (hud != null && hud.playButton != null)
            hud.playButton.onClick.RemoveListener(Play);
    }

    private static string FormatObjective(ObjectiveDefinition objective)
    {
        switch (objective.Type)
        {
            case ObjectiveType.Score: return $"Earn {objective.Target:N0} points";
            case ObjectiveType.ClearColor: return $"Clear {objective.Target} {objective.Color} pieces";
            case ObjectiveType.UnlockTiles: return $"Unlock {objective.Target} tiles";
            case ObjectiveType.RescueBirds: return $"Rescue {objective.Target} birds";
            case ObjectiveType.ClearFrost: return $"Clear {objective.Target} frost layers";
            case ObjectiveType.DestroyFrostMachines: return $"Destroy {objective.Target} Frost Machines";
            default: return $"{objective.Type}: {objective.Target}";
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }
}
