using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MutaBorn.Gameplay
{
    // Installs itself on Play; no scene, prefab or inspector wiring is required.
    // Optional sprites/colors are read from the Resources palette.
    public sealed class DamageIndicatorManager : MonoBehaviour
    {
        private sealed class TargetCluster
        {
            public float LastSpawnTime;
            public int NextSlot;

        }

        private readonly Dictionary<Transform, TargetCluster> clusters = new Dictionary<Transform, TargetCluster>();
        private RectTransform canvasRect;
        private DamageIndicatorPalette palette;
        private Sprite guardActivationIcon;
        private Sprite healIcon;
        private Sprite apIcon;
        private Sprite manaIcon;
        private Sprite buffIcon;
        private Sprite debuffIcon;
        private Sprite blockedIcon;
        private Camera worldCamera;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateAutomatically()
        {
            if (FindAnyObjectByType<DamageIndicatorManager>() != null)
            {
                return;

            }

            GameObject host = new GameObject("MutaBorn Damage Indicators", typeof(DamageIndicatorManager));
            DontDestroyOnLoad(host);

        }

        private void Awake()
        {
            palette = Resources.Load<DamageIndicatorPalette>("DamageIndicators/DamageIndicatorPalette");
            // Inspector references take priority. Otherwise a matching Sprite PNG
            // placed in Resources/DamageIndicators is loaded automatically.
            guardActivationIcon = LoadFeedbackSprite(
                palette != null ? palette.GuardActivationIcon : null, "GuardActivationIcon");
            healIcon = LoadFeedbackSprite(palette != null ? palette.HealIcon : null, "HealIcon");
            apIcon = LoadFeedbackSprite(palette != null ? palette.APIcon : null, "APIcon");
            manaIcon = LoadFeedbackSprite(palette != null ? palette.ManaIcon : null, "ManaIcon");
            buffIcon = LoadFeedbackSprite(palette != null ? palette.BuffIcon : null, "BuffIcon");
            debuffIcon = LoadFeedbackSprite(palette != null ? palette.DebuffIcon : null, "DebuffIcon");
            blockedIcon = LoadFeedbackSprite(palette != null ? palette.BlockedIcon : null, "BlockedIcon");
            CreateCanvas();

        }

        private static Sprite LoadFeedbackSprite(Sprite assignedSprite, string resourceName)
        {
            if (assignedSprite != null)
            {
                return assignedSprite;

            }

            return Resources.Load<Sprite>("DamageIndicators/" + resourceName);

        }

        private void OnEnable()
        {
            CombatActionExecutor.DamageApplied += OnDamageApplied;
            CombatFloatingFeedback.ResourceGained += OnResourceGained;
            CombatFloatingFeedback.StatusApplied += OnStatusApplied;
            CombatFloatingFeedback.GuardBlocked += OnGuardBlocked;
            CombatFloatingFeedback.GuardActivated += OnGuardActivated;

        }

        private void OnDisable()
        {
            CombatActionExecutor.DamageApplied -= OnDamageApplied;
            CombatFloatingFeedback.ResourceGained -= OnResourceGained;
            CombatFloatingFeedback.StatusApplied -= OnStatusApplied;
            CombatFloatingFeedback.GuardBlocked -= OnGuardBlocked;
            CombatFloatingFeedback.GuardActivated -= OnGuardActivated;

        }

        private void OnDamageApplied(CombatDamageEvent report)
        {
            if (report.Target == null || report.ActualDamage <= 0 || canvasRect == null)
            {
                return;

            }

            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                worldCamera = Camera.main;

            }

            if (worldCamera == null)
            {
                return;

            }

            float height = palette != null ? palette.SpawnHeight : 1.1f;
            Vector3 worldPoint = report.Target.transform.position + Vector3.up * height;
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f)
            {
                return;

            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                screenPoint, null, out Vector2 canvasPoint))
            {
                return;

            }

            // One combat event remains one gameplay event, but may create
            // multiple visual numbers when the resolved hit had several types.
            List<DamageTypeAmount> display = new List<DamageTypeAmount>();
            if (report.DamageByType != null)
            {
                foreach (DamageTypeAmount contribution in report.DamageByType)
                {
                    if (contribution.Amount > 0)
                    {
                        display.Add(contribution);

                    }

                }

            }

            if (display.Count == 0)
            {
                display.Add(new DamageTypeAmount(report.DamageType, report.ActualDamage));

            }

            int slot = NextSlot(report.Target.transform);
            float spacing = palette != null ? palette.SideSpacing : 46f;
            float batchOffset = SlotOffset(slot) * spacing;
            for (int i = 0; i < display.Count; i++)
            {
                float side = i - (display.Count - 1) * 0.5f;
                SpawnIndicator(display[i], canvasPoint,
                    batchOffset + side * spacing, side + SlotOffset(slot) * 0.5f);

            }

        }

        private void SpawnIndicator(DamageTypeAmount contribution, Vector2 canvasPoint,
            float horizontalOffset, float driftSide)
        {
            DamageType type = contribution.Type == DamageType.Unspecified
                ? DamageType.Slash : contribution.Type;
            DamageIndicatorStyle style = ResolveStyle(type);

            GameObject popupObject = new GameObject("Damage " + type + " (" + contribution.Amount + ")",
                typeof(RectTransform), typeof(CanvasGroup), typeof(DamageIndicatorPopup));
            popupObject.transform.SetParent(canvasRect, false);
            RectTransform popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(82f, 72f);
            popupRect.anchoredPosition = canvasPoint + new Vector2(horizontalOffset, 0f);

            GameObject backgroundObject = new GameObject("Damage Type PNG", typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(popupRect, false);
            RectTransform bgRect = backgroundObject.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image background = backgroundObject.GetComponent<Image>();
            background.sprite = style.backgroundSprite;
            background.color = style.backgroundColor;
            background.preserveAspect = true;
            background.raycastTarget = false;

            GameObject numberObject = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
            numberObject.transform.SetParent(popupRect, false);
            RectTransform textRect = numberObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0.26f);
            textRect.anchorMax = new Vector2(1f, 0.90f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI number = numberObject.GetComponent<TextMeshProUGUI>();
            number.text = contribution.Amount.ToString();
            number.fontSize = 35f;
            number.fontStyle = FontStyles.Bold;
            number.alignment = TextAlignmentOptions.Center;
            number.enableAutoSizing = true;
            number.fontSizeMin = 15f;
            number.fontSizeMax = 35f;
            number.color = style.numberColor;
            number.raycastTarget = false;

            GameObject labelObject = new GameObject("Type Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(popupRect, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.07f);
            labelRect.anchorMax = new Vector2(1f, 0.36f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = string.IsNullOrEmpty(style.label) ? type.ToString() : style.label;
            label.fontSize = 12f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = style.numberColor;
            label.raycastTarget = false;

            Vector2 direction = new Vector2(driftSide * 0.65f, 1f).normalized;
            popupObject.GetComponent<DamageIndicatorPopup>().Initialize(
                direction, palette != null ? palette.Lifetime : 1.05f,
                palette != null ? palette.HoldDuration : 0.16f,
                palette != null ? palette.DriftSpeed : 25f,
                palette != null ? palette.Acceleration : 220f);

        }

        // All positive resource changes are reported at UnitData, not guessed
        // from requested potion/ability amounts (which can over-heal).
        private void OnResourceGained(UnitData data, ResourceIdentifier resource, int amount)
        {
            if (data == null || amount <= 0)
            {
                return;

            }

            UnitController unit = FindUnitWithData(data);
            if (unit == null)
            {
                return;

            }

            switch (resource)
            {
                case ResourceIdentifier.Health:
                {
                    SpawnFeedback(unit, "+" + amount, "HP",
                        new Color(0.28f, 0.88f, 0.52f, 1f), healIcon, DamageType.Holy);

                    break;

                }
                case ResourceIdentifier.ActionPoints:
                {
                    SpawnFeedback(unit, "+" + amount, "AP",
                        new Color(1f, 0.76f, 0.21f, 1f), apIcon, DamageType.Lightning);

                    break;

                }
                case ResourceIdentifier.Mana:
                {
                    SpawnFeedback(unit, "+" + amount, "MANA",
                        new Color(0.56f, 0.72f, 1f, 1f), manaIcon, DamageType.Arcane);

                    break;

                }
            }

        }

        private static UnitController FindUnitWithData(UnitData data)
        {
            // Data is not a Component. Resolve its owning UnitController only
            // when a gain occurs, so ordinary damage rendering stays cheap.
            foreach (UnitController unit in FindObjectsByType<UnitController>())
            {
                if (unit != null && ReferenceEquals(unit.data, data))
                {
                    return unit;

                }

            }

            return null;

        }

        private void OnStatusApplied(UnitController unit, bool isDebuff)
        {
            Sprite icon = isDebuff ? debuffIcon : buffIcon;
            SpawnFeedback(unit,
                isDebuff ? "DEBUFF" : "BUFF",
                "STATUS", isDebuff
                    ? new Color(1f, 0.49f, 0.33f, 1f)
                    : new Color(0.35f, 0.9f, 0.62f, 1f),
                icon,
                isDebuff ? DamageType.Dark : DamageType.Holy);

        }

        private void OnGuardBlocked(UnitController unit, int amount)
        {
            if (amount > 0)
            {
                SpawnFeedback(unit, "BLOCKED " + amount, "GUARD",
                    new Color(0.63f, 0.79f, 1f, 1f), blockedIcon, DamageType.Blunt);

            }

        }

        // Distinct from BLOCKED N: this confirms activation even if no attack follows.
        // All ApplyGuard actions (including shield variants and Brace) use this path.
        private void OnGuardActivated(UnitController unit)
        {
            if (unit == null || !unit.isActiveAndEnabled || canvasRect == null)
            {
                return;

            }

            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                worldCamera = Camera.main;

            }

            if (worldCamera == null)
            {
                return;

            }

            float height = palette != null ? palette.SpawnHeight : 1.1f;
            Vector3 worldPoint = unit.transform.position + Vector3.up * (height + 0.65f);
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                    screenPoint, null, out Vector2 canvasPoint))
            {
                return;

            }

            int slot = NextSlot(unit.transform);
            float spacing = palette != null ? palette.SideSpacing : 46f;
            GameObject popupObject = new GameObject("Guard Activated",
                typeof(RectTransform), typeof(CanvasGroup), typeof(DamageIndicatorPopup));
            popupObject.transform.SetParent(canvasRect, false);
            RectTransform popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(108f, 122f);
            popupRect.anchoredPosition = canvasPoint + new Vector2(SlotOffset(slot) * spacing, 0f);

            if (guardActivationIcon != null)
            {
                GameObject shieldObject = new GameObject("Shield Icon",
                    typeof(RectTransform), typeof(Image));
                shieldObject.transform.SetParent(popupRect, false);
                RectTransform shieldRect = shieldObject.GetComponent<RectTransform>();
                shieldRect.anchorMin = new Vector2(0.10f, 0.23f);
                shieldRect.anchorMax = new Vector2(0.90f, 0.96f);
                shieldRect.offsetMin = Vector2.zero;
                shieldRect.offsetMax = Vector2.zero;
                Image shieldImage = shieldObject.GetComponent<Image>();
                shieldImage.sprite = guardActivationIcon;
                shieldImage.preserveAspect = true;
                shieldImage.raycastTarget = false;

            }

            GameObject labelObject = new GameObject("Guard Text",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(popupRect, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, guardActivationIcon != null ? 0.32f : 0.85f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "GUARD";
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.fontSize = guardActivationIcon != null ? 21f : 29f;
            label.color = new Color(0.74f, 0.88f, 1f, 1f);
            label.raycastTarget = false;

            popupObject.GetComponent<DamageIndicatorPopup>().Initialize(
                new Vector2(SlotOffset(slot) * 0.20f, 1f), 1.25f, 0.35f, 17f, 110f);

        }

        private void SpawnFeedback(UnitController unit, string value, string category,
            Color tint, Sprite icon, DamageType styleType)
        {
            if (unit == null || !unit.isActiveAndEnabled || canvasRect == null)
            {
                return;

            }

            if (worldCamera == null || !worldCamera.isActiveAndEnabled)
            {
                worldCamera = Camera.main;

            }

            if (worldCamera == null)
            {
                return;

            }

            float height = palette != null ? palette.SpawnHeight : 1.1f;
            Vector3 worldPoint = unit.transform.position + Vector3.up * (height + 0.28f);
            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,
                    screenPoint, null, out Vector2 canvasPoint))
            {
                return;

            }

            int slot = NextSlot(unit.transform);
            float spacing = Mathf.Max(80f, palette != null ? palette.SideSpacing : 46f);
            float offset = SlotOffset(slot) * spacing;
            DamageIndicatorStyle style = ResolveStyle(styleType);

            GameObject popupObject = new GameObject("Combat Feedback " + value,
                typeof(RectTransform), typeof(CanvasGroup), typeof(DamageIndicatorPopup));
            popupObject.transform.SetParent(canvasRect, false);
            RectTransform popupRect = popupObject.GetComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(96f, 76f);
            popupRect.anchoredPosition = canvasPoint + new Vector2(offset, 0f);

            GameObject backgroundObject = new GameObject("Feedback Type PNG",
                typeof(RectTransform), typeof(Image));
            backgroundObject.transform.SetParent(popupRect, false);
            RectTransform bgRect = backgroundObject.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image background = backgroundObject.GetComponent<Image>();
            background.sprite = style.backgroundSprite;
            background.color = style.backgroundSprite != null
                ? style.backgroundColor
                : new Color(0.11f, 0.13f, 0.17f, 0.85f);
            background.preserveAspect = true;
            background.raycastTarget = false;

            if (icon != null)
            {
                GameObject iconObject = new GameObject("Feedback Icon",
                    typeof(RectTransform), typeof(Image));
                iconObject.transform.SetParent(popupRect, false);
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.18f, 0.24f);
                iconRect.anchorMax = new Vector2(0.82f, 0.88f);
                iconRect.offsetMin = Vector2.zero;
                iconRect.offsetMax = Vector2.zero;
                Image iconImage = iconObject.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.color = new Color(1f, 1f, 1f, 0.18f);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;

            }

            GameObject valueObject = new GameObject("Value",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            valueObject.transform.SetParent(popupRect, false);
            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 0.26f);
            valueRect.anchorMax = new Vector2(1f, 0.90f);
            valueRect.offsetMin = Vector2.zero;
            valueRect.offsetMax = Vector2.zero;
            TextMeshProUGUI valueText = valueObject.GetComponent<TextMeshProUGUI>();
            valueText.text = value;
            valueText.fontSize = 31f;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 12f;
            valueText.fontSizeMax = 31f;
            valueText.color = tint;
            valueText.raycastTarget = false;

            GameObject labelObject = new GameObject("Feedback Label",
                typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(popupRect, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.07f);
            labelRect.anchorMax = new Vector2(1f, 0.36f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
            labelText.text = category;
            labelText.fontSize = 12f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = tint;
            labelText.raycastTarget = false;

            Vector2 direction = new Vector2(SlotOffset(slot) * 0.4f, 1f).normalized;
            popupObject.GetComponent<DamageIndicatorPopup>().Initialize(
                direction, palette != null ? palette.Lifetime : 1.05f,
                palette != null ? palette.HoldDuration : 0.16f,
                palette != null ? palette.DriftSpeed : 25f,
                palette != null ? palette.Acceleration : 220f);

        }

        private int NextSlot(Transform target)
        {
            if (!clusters.TryGetValue(target, out TargetCluster cluster))
            {
                cluster = new TargetCluster();
                clusters.Add(target, cluster);

            }

            float now = Time.unscaledTime;
            float window = palette != null ? palette.ClusterWindow : 0.25f;
            if (now - cluster.LastSpawnTime > window)
            {
                cluster.NextSlot = 0;

            }

            int slot = cluster.NextSlot++ % 5;
            cluster.LastSpawnTime = now;

            // Keep the cache bounded even during extended combat sessions.
            if (clusters.Count > 128)
            {
                clusters.Clear();

            }

            return slot;

        }

        private static int SlotOffset(int slot)
        {
            switch (slot)
            {
                case 1:
                {
                    return -1;

                }
                case 2:
                {
                    return 1;

                }
                case 3:
                {
                    return -2;

                }
                case 4:
                {
                    return 2;

                }
                default:
                {
                    return 0;

                }

            }

        }

        private DamageIndicatorStyle ResolveStyle(DamageType type)
        {
            if (palette != null && palette.TryGetStyle(type, out DamageIndicatorStyle style))
            {
                return style;

            }

            // Usable without a palette asset; custom presentation stays data-driven.
            return new DamageIndicatorStyle
            {
                damageType = type,
                backgroundColor = new Color(0.11f, 0.13f, 0.17f, 0.85f),
                numberColor = Color.white,
                label = type.ToString()

            };

        }

        private void CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Damage Indicator Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            canvasRect = canvasObject.GetComponent<RectTransform>();
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 500;

        }

    }

}
