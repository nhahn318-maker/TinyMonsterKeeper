using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#pragma warning disable 0649

public class FogZoneManager : MonoBehaviour
{
    public event Action<string> FogZoneUnlocked;

    private enum RevealDirection
    {
        Right,
        Left,
        Up,
        Down,
        Custom
    }

    [Serializable]
    private class FogZone
    {
        public string zoneName;
        public int unlockCost = 10;

        [Header("Click Target")]
        public Button uiButton;
        public Collider2D mapButtonCollider;

        [Header("Fog")]
        public FogTilemapRevealController fogReveal;
        public Collider2D revealBounds;
        public FogAreaBlocker[] monsterBlockers;
        public bool revealWholeTilemap = true;

        [Header("Reveal Motion")]
        public RevealDirection revealDirection = RevealDirection.Right;
        public float revealDistance = 0.75f;
        public Vector3 customRevealDriftOffset = new Vector3(0.75f, 0f, 0f);

        [Header("State")]
        public bool hideButtonOnUnlock = true;

        [NonSerialized] public bool isUnlocked;
        [NonSerialized] public bool isUnlocking;
    }

    [Header("Shared References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private FogUnlockConfirmDialogUI confirmDialog;
    [SerializeField] private GameTextDatabase textDatabase;

    [Header("Text")]
    [SerializeField] private GameTextKey confirmMessageKey = GameTextKey.FogUnlockConfirm;
    [SerializeField] private GameTextKey notEnoughCoinMessageKey = GameTextKey.NotEnoughCoin;
    [SerializeField] private string confirmMessageFallback = "Unlock this area for {0} coins?";
    [SerializeField] private string notEnoughCoinFallback = "Not enough coins.";

    [Header("Zones")]
    [SerializeField] private List<FogZone> zones = new List<FogZone>();

    private readonly List<UnityAction> buttonActions = new List<UnityAction>();

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        TryHandleMapButtonClick(Input.mousePosition);
    }

    private void BindButtons()
    {
        UnbindButtons();

        for (int i = 0; i < zones.Count; i++)
        {
            int zoneIndex = i;
            Button button = zones[i].uiButton;
            if (button == null)
                continue;

            UnityAction action = () => HandleZoneClicked(zoneIndex);
            button.onClick.AddListener(action);
            buttonActions.Add(action);
        }
    }

    private void UnbindButtons()
    {
        for (int i = 0; i < zones.Count && i < buttonActions.Count; i++)
        {
            Button button = zones[i].uiButton;
            if (button != null)
                button.onClick.RemoveListener(buttonActions[i]);
        }

        buttonActions.Clear();
    }

    private void TryHandleMapButtonClick(Vector3 screenPosition)
    {
        if (worldCamera == null)
            return;

        Vector2 worldPoint = worldCamera.ScreenToWorldPoint(screenPosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);

        for (int i = 0; i < zones.Count; i++)
        {
            Collider2D buttonCollider = zones[i].mapButtonCollider;
            if (buttonCollider == null)
                continue;

            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                if (hits[hitIndex] == buttonCollider)
                {
                    HandleZoneClicked(i);
                    return;
                }
            }
        }
    }

    private void HandleZoneClicked(int zoneIndex)
    {
        if (zoneIndex < 0 || zoneIndex >= zones.Count)
            return;

        FogZone zone = zones[zoneIndex];
        if (zone.isUnlocking || zone.isUnlocked)
            return;

        FogUnlockConfirmDialogUI dialog = confirmDialog != null ? confirmDialog : FogUnlockConfirmDialogUI.Instance;
        if (dialog == null)
        {
            Debug.LogWarning("Fog unlock confirm dialog is missing. Unlocking directly for testing.");
            TryUnlock(zone);
            return;
        }

        dialog.ShowConfirm(GetText(confirmMessageKey, confirmMessageFallback, zone.unlockCost), () => TryUnlock(zone));
    }

    private void TryUnlock(FogZone zone)
    {
        if (zone == null || zone.isUnlocking || zone.isUnlocked)
            return;

        if (CurrencyManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager is missing. Cannot unlock fog zone.");
            return;
        }

        if (zone.unlockCost > 0 && !CurrencyManager.Instance.SpendCoin(zone.unlockCost))
        {
            FogUnlockConfirmDialogUI dialog = confirmDialog != null ? confirmDialog : FogUnlockConfirmDialogUI.Instance;
            if (dialog != null)
                dialog.ShowMessage(GetText(notEnoughCoinMessageKey, notEnoughCoinFallback));
            return;
        }

        UnlockZone(zone);
    }

    private void UnlockZone(FogZone zone)
    {
        UnlockZone(zone, true, true);
    }

    private void UnlockZone(FogZone zone, bool animateReveal, bool notify)
    {
        zone.isUnlocking = animateReveal;
        zone.isUnlocked = true;

        if (zone.hideButtonOnUnlock)
        {
            if (zone.uiButton != null)
                zone.uiButton.gameObject.SetActive(false);

            if (zone.mapButtonCollider != null)
                zone.mapButtonCollider.gameObject.SetActive(false);
        }

        SetMonsterBlockers(zone, false);

        if (zone.fogReveal == null)
        {
            Debug.LogWarning($"Fog reveal controller is missing for zone: {zone.zoneName}");
            if (notify)
                FogZoneUnlocked?.Invoke(GetZoneId(zone));
            return;
        }

        if (animateReveal)
        {
            Vector3 driftOffset = GetRevealDriftOffset(zone);
            if (zone.revealWholeTilemap || zone.revealBounds == null)
                zone.fogReveal.RevealAll(driftOffset);
            else
                zone.fogReveal.RevealColliderBounds(zone.revealBounds, driftOffset);
        }
        else
        {
            if (zone.revealWholeTilemap || zone.revealBounds == null)
                zone.fogReveal.ClearAll();
            else
                zone.fogReveal.ClearColliderBounds(zone.revealBounds);
        }

        zone.isUnlocking = false;

        if (notify)
            FogZoneUnlocked?.Invoke(GetZoneId(zone));
    }

    public void ApplyUnlockedZones(List<string> unlockedZoneIds)
    {
        if (unlockedZoneIds == null)
            return;

        HashSet<string> unlockedIdSet = new HashSet<string>(unlockedZoneIds);
        for (int i = 0; i < zones.Count; i++)
        {
            FogZone zone = zones[i];
            if (zone == null)
                continue;

            string zoneId = GetZoneId(zone, i);
            if (!unlockedIdSet.Contains(zoneId))
                continue;

            UnlockZone(zone, false, false);
        }
    }

    public List<string> ExportUnlockedZoneIds()
    {
        List<string> unlockedZoneIds = new List<string>();
        for (int i = 0; i < zones.Count; i++)
        {
            FogZone zone = zones[i];
            if (zone != null && zone.isUnlocked)
                unlockedZoneIds.Add(GetZoneId(zone, i));
        }

        return unlockedZoneIds;
    }

    private string GetZoneId(FogZone zone)
    {
        int index = zones.IndexOf(zone);
        return GetZoneId(zone, index);
    }

    private string GetZoneId(FogZone zone, int index)
    {
        if (zone == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(zone.zoneName))
            return zone.zoneName.Trim();

        return "FogZone_" + index;
    }

    private void SetMonsterBlockers(FogZone zone, bool blocked)
    {
        if (zone.monsterBlockers == null)
            return;

        for (int i = 0; i < zone.monsterBlockers.Length; i++)
        {
            if (zone.monsterBlockers[i] != null)
                zone.monsterBlockers[i].SetBlocked(blocked);
        }
    }

    private Vector3 GetRevealDriftOffset(FogZone zone)
    {
        float distance = Mathf.Max(0f, zone.revealDistance);

        switch (zone.revealDirection)
        {
            case RevealDirection.Left:
                return Vector3.left * distance;
            case RevealDirection.Up:
                return Vector3.up * distance;
            case RevealDirection.Down:
                return Vector3.down * distance;
            case RevealDirection.Custom:
                return zone.customRevealDriftOffset;
            default:
                return Vector3.right * distance;
        }
    }

    private string GetText(GameTextKey key, string fallback, params object[] args)
    {
        if (textDatabase != null)
            return textDatabase.Get(key, fallback, args);

        return args == null || args.Length == 0 ? fallback : string.Format(fallback, args);
    }
}

#pragma warning restore 0649
