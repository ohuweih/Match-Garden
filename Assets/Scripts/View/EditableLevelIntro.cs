using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EditableLevelIntro : MonoBehaviour
{
    [Header("Root")]
    public GameObject introRoot;

    [Header("Content")]
    public TMP_Text levelTitleText;
    public RectTransform objectivesRoot;
    public GameObject objectiveCardTemplate;
    public TMP_Text movesText;
    public Button playButton;

    public bool IsConfigured =>
        introRoot != null &&
        levelTitleText != null &&
        objectivesRoot != null &&
        objectiveCardTemplate != null &&
        movesText != null &&
        playButton != null;
}
