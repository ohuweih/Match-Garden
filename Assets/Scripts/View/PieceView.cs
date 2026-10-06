using UnityEngine;
using System.Collections;

public class PieceView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [Header("Normal Piece Art")]
    [SerializeField] private Sprite redSprite;
    [SerializeField] private Sprite orangeSprite;
    [SerializeField] private Sprite yellowSprite;
    [SerializeField] private Sprite greenSprite;
    [SerializeField] private Sprite blueSprite;
    [SerializeField] private Sprite purpleSprite;

    [Header("Special Piece Art")]
    [SerializeField] private Sprite horizontalLineSprite;
    [SerializeField] private Sprite verticalLineSprite;
    [SerializeField] private Sprite bombSprite;
    [SerializeField] private Sprite targetSprite;
    [SerializeField] private Sprite colorClearSprite;

    [Header("Piece Sizing")]
    [SerializeField, Range(0.75f, 1.05f)] private float pieceArtScale = 0.92f;

    [Header("Special Creation Animation")]
    [SerializeField, Min(0.05f)] private float specialTransitionDuration = 0.30f;

    [Header("Movable Board Object Art")]
    [SerializeField] private Sprite birdSprite;
    [SerializeField] private Sprite packageSprite;

    [SerializeField]
    private GameObject specialMarker;

    private int x;
    private int y;

    private bool isSelected;
    private bool isMovableObjective;
    private bool isPackage;
    private PackageView packageView;
    private bool hasVisualState;
    private SpecialType displayedSpecial = SpecialType.None;
    private Coroutine specialTransition;

    private void UpdatePackageView()
    {
        if (isPackage && packageView == null)
        {
            var root = new GameObject("Mystery package"); root.transform.SetParent(transform, false);
            packageView = root.AddComponent<PackageView>();
            packageView.Setup(spriteRenderer.sortingLayerID, spriteRenderer.sortingOrder + 1, packageSprite);
        }
        if (packageView != null) packageView.gameObject.SetActive(isPackage);
    }

    public void Show(PieceSnapshot snapshot)
    {
        Refresh(new BoardCell(x, y, snapshot.CreatePiece()));
    }
    private long displayedDistance;
    private BirdRescueGoal rescueGoal;
    private int birdNumber;
    private TextMesh objectiveLabel;
    private BirdFlightView birdFlight;

    private void UpdateObjectiveLabel()
    {
        if (isMovableObjective && objectiveLabel == null)
        {
            var label = new GameObject("Bird label");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0, -.36f, -0.1f);
            objectiveLabel = label.AddComponent<TextMesh>();
            objectiveLabel.anchor = TextAnchor.MiddleCenter;
            objectiveLabel.alignment = TextAlignment.Center;
            objectiveLabel.characterSize = 0.045f;
            objectiveLabel.fontStyle = FontStyle.Bold;
            objectiveLabel.fontSize = 48;
            objectiveLabel.color = Color.white;
            objectiveLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var renderer = label.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = objectiveLabel.font.material;
            renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            renderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        }
        if (objectiveLabel != null)
        {
            objectiveLabel.gameObject.SetActive(isMovableObjective);
            string progress = rescueGoal == null ? displayedDistance.ToString()
                : rescueGoal.Condition == BirdRescueCondition.TravelDistance
                    ? $"{displayedDistance}/{rescueGoal.RequiredDistance}"
                    : rescueGoal.Condition == BirdRescueCondition.ReachBottom
                        ? "To bottom" : $"Nest {birdNumber}";
            objectiveLabel.text = rescueGoal != null && rescueGoal.Condition != BirdRescueCondition.TravelDistance
                ? progress : $"#{birdNumber}  {progress}";
        }
    }

    public int X => x;
    public int Y => y;

    public void Setup(BoardCell cell)
    {
        x = cell.X;
        y = cell.Y;

        Refresh(cell);
    }

    public IEnumerator MoveTo(
    Vector3 targetPosition,
    float duration)
    {
        Vector3 startPosition =
            transform.position;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                elapsed / duration;

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    t
                );

            yield return null;
        }

        transform.position =
            targetPosition;
    }

    public void SetCoordinates(
    int newX,
    int newY)
    {
        if (isMovableObjective)
            displayedDistance += System.Math.Abs(newX - x) + System.Math.Abs(newY - y);
        UpdateObjectiveLabel();
        x = newX;
        y = newY;
    }

    public IEnumerator Pop(float duration)
    {
        Vector3 startScale =
            transform.localScale;

        Vector3 largeScale =
            Vector3.one * pieceArtScale * 1.15f;

        float growDuration =
            duration * 0.3f;

        float shrinkDuration =
            duration * 0.7f;

        float elapsed = 0f;

        // Small punch outward first.
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                elapsed / growDuration;

            transform.localScale =
                Vector3.Lerp(
                    startScale,
                    largeScale,
                    t
                );

            yield return null;
        }

        elapsed = 0f;

        // Then shrink down to nothing.
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                elapsed / shrinkDuration;

            transform.localScale =
                Vector3.Lerp(
                    largeScale,
                    Vector3.zero,
                    t
                );

            yield return null;
        }

        transform.localScale =
            Vector3.zero;
    }

    public IEnumerator PlaySpecialActivationPulse()
    {
        const float duration = 0.14f;
        float halfDuration = duration * 0.5f;

        Vector3 baseScale = Vector3.one * pieceArtScale;
        Vector3 punchScale = baseScale * 1.22f;
        Color baseColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(baseScale, punchScale, smoothT);
            float alpha = Mathf.Lerp(1f, 0.55f, smoothT);
            spriteRenderer.color =
                new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.localScale = Vector3.Lerp(punchScale, baseScale, smoothT);
            float alpha = Mathf.Lerp(0.55f, 1f, smoothT);
            spriteRenderer.color =
                new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        transform.localScale = baseScale;
        spriteRenderer.color =
            new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
    }

    public void Hide()
    {
        if (birdFlight != null) birdFlight.SetBird(spriteRenderer, false);
        if (specialTransition != null)
        {
            StopCoroutine(specialTransition);
            specialTransition = null;
        }
        hasVisualState = false;
        displayedSpecial = SpecialType.None;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f);
        }
        if (objectiveLabel != null) objectiveLabel.gameObject.SetActive(false);
        if (packageView != null) packageView.gameObject.SetActive(false);
        spriteRenderer.enabled = false;

        specialMarker.SetActive(false);
    }

    public void Show(
    PieceType color,
    SpecialType special)
    {
        isMovableObjective = false;
        isPackage = false;
        UpdatePackageView();
        rescueGoal = null;
        birdNumber = 0;
        displayedDistance = 0;
        UpdateObjectiveLabel();
        spriteRenderer.enabled = true;

        transform.localScale =
            Vector3.one * pieceArtScale;

        ApplyNormalPieceArt(color);
        ApplySpecialPieceArt(special);
        displayedSpecial = special;
        hasVisualState = true;
    }

    public void Refresh(BoardCell cell)
    {
        isMovableObjective = !cell.IsEmpty && cell.Piece.IsMovableObjective;
        isPackage = !cell.IsEmpty && cell.Piece.IsPackage;
        if (isMovableObjective && birdFlight == null) birdFlight = gameObject.AddComponent<BirdFlightView>();
        if (birdFlight != null) birdFlight.SetBird(spriteRenderer, isMovableObjective);
        UpdatePackageView();
        displayedDistance = isMovableObjective ? cell.Piece.DistanceTraveled : 0;
        rescueGoal = isMovableObjective ? cell.Piece.RescueGoal : null;
        birdNumber = isMovableObjective ? cell.Piece.BirdNumber : 0;
        UpdateObjectiveLabel();
        if (cell.IsEmpty)
        {
            spriteRenderer.enabled = false;
            specialMarker.SetActive(false);
            return;
        }

        spriteRenderer.enabled = true;

        if (isPackage)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.color = Color.clear;
            specialMarker.SetActive(false);
        }
        else if (isMovableObjective && birdSprite != null)
        {
            spriteRenderer.sprite = birdSprite;
            spriteRenderer.color = Color.white;
            specialMarker.SetActive(false);
        }
        else
        {
            SpecialType newSpecial = cell.Piece.Special;
            bool shouldAnimateSpecialCreation =
                hasVisualState &&
                displayedSpecial == SpecialType.None &&
                newSpecial != SpecialType.None &&
                GetSpecialSprite(newSpecial) != null;

            if (specialTransition != null)
            {
                StopCoroutine(specialTransition);
                specialTransition = null;
            }

            ApplyNormalPieceArt(cell.Piece.Color);

            if (shouldAnimateSpecialCreation)
                specialTransition = StartCoroutine(AnimateSpecialCreation(newSpecial));
            else
                ApplySpecialPieceArt(newSpecial);

            displayedSpecial = newSpecial;
            hasVisualState = true;
        }

        transform.localScale =
            Vector3.one * pieceArtScale;
    }

    private Sprite GetSpecialSprite(SpecialType specialType)
    {
        switch (specialType)
        {
            case SpecialType.LineHorizontal: return horizontalLineSprite;
            case SpecialType.LineVertical: return verticalLineSprite;
            case SpecialType.Bomb: return bombSprite;
            case SpecialType.Target: return targetSprite;
            case SpecialType.ColorClear: return colorClearSprite;
            default: return null;
        }
    }

    private IEnumerator AnimateSpecialCreation(SpecialType specialType)
    {
        float halfDuration = Mathf.Max(0.025f, specialTransitionDuration * 0.5f);
        Color baseColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / halfDuration);
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        ApplySpecialPieceArt(specialType);
        baseColor = spriteRenderer.color;
        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / halfDuration);
            spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            yield return null;
        }

        spriteRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        specialTransition = null;
    }

    private void ApplySpecialPieceArt(SpecialType specialType)
    {
        Sprite specialSprite = GetSpecialSprite(specialType);

        if (specialSprite != null)
        {
            spriteRenderer.sprite = specialSprite;
            spriteRenderer.color = Color.white;
            specialMarker.SetActive(false);
        }
        else
        {
            UpdateSpecialMarker(specialType);
        }
    }

    private void UpdateSpecialMarker(
    SpecialType specialType)
    {
        if (specialType == SpecialType.None)
        {
            specialMarker.SetActive(false);
            return;
        }

        specialMarker.SetActive(true);

        switch (specialType)
        {
            case SpecialType.LineHorizontal:
                specialMarker.transform.localScale =
                    new Vector3(0.65f, 0.12f, 1f);

                specialMarker.transform.localRotation =
                    Quaternion.identity;
                break;

            case SpecialType.LineVertical:
                specialMarker.transform.localScale =
                    new Vector3(0.12f, 0.65f, 1f);

                specialMarker.transform.localRotation =
                    Quaternion.identity;
                break;

            case SpecialType.Target:
                specialMarker.transform.localScale =
                    new Vector3(0.28f, 0.28f, 1f);

                specialMarker.transform.localRotation =
                    Quaternion.identity;
                break;

            case SpecialType.ColorClear:
                specialMarker.transform.localScale =
                    new Vector3(0.48f, 0.48f, 1f);

                specialMarker.transform.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        45f
                    );
                break;

            case SpecialType.Bomb:
                specialMarker.transform.localScale =
                    new Vector3(0.42f, 0.42f, 1f);

                specialMarker.transform.localRotation =
                    Quaternion.identity;
                break;

            default:
                specialMarker.SetActive(false);
                break;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (isSelected)
        {
            transform.localScale = Vector3.one * pieceArtScale * 0.85f;
        }
        else
        {
            transform.localScale = Vector3.one * pieceArtScale;
        }
    }

    private void OnMouseDown()
    {
        InputController.Instance.HandlePieceClicked(this);
    }

    private void ApplyNormalPieceArt(PieceType pieceType)
    {
        Sprite sprite = GetSprite(pieceType);
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
        }
        else
        {
            spriteRenderer.sprite = null;
            spriteRenderer.color = GetColor(pieceType);
        }
    }

    private Sprite GetSprite(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Red: return redSprite;
            case PieceType.Orange: return orangeSprite;
            case PieceType.Yellow: return yellowSprite;
            case PieceType.Green: return greenSprite;
            case PieceType.Blue: return blueSprite;
            case PieceType.Purple: return purpleSprite;
            default: return null;
        }
    }

    private Color GetColor(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Red:
                return Color.red;

            case PieceType.Blue:
                return Color.blue;

            case PieceType.Green:
                return Color.green;

            case PieceType.Yellow:
                return Color.yellow;

            case PieceType.Purple:
                return new Color(0.6f, 0.2f, 0.8f);

            case PieceType.Orange:
                return new Color(1f, 0.5f, 0f);

            default:
                return Color.white;
        }
    }
}