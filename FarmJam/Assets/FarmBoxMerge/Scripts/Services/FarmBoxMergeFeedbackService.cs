using UnityEngine;

public interface IFarmBoxMergeFeedbackService
{
    Color ColorFor(ColorType colorType);
    void PlayButtonClick();
    void PlayCardPicked(RectTransform card);
    void PlayCardSpawn(RectTransform card, Color color);
    void PlayCardMerge(RectTransform card, Color color);
    void PlayCardDiscard(RectTransform trashTarget);
    void PlayItemSpawn(Transform item, ColorType colorType);
    void PlayItemLanded(Transform item, ColorType colorType);
    void PlayBoxCreated(Transform boxGroup, ColorType colorType);
    void PlayBoxCleared(Vector3 position, ColorType colorType, int boxCount);
    void PlayOutcome(GameObject panel, bool won);
}
