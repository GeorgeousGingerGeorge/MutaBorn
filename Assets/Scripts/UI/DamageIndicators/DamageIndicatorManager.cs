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
            CreateCanvas();

        }

        private void OnEnable()
        {
            CombatActionExecutor.DamageApplied += OnDamageApplied;

        }

        private void OnDisable()
        {
            CombatActionExecutor.DamageApplied -= OnDamageApplied;

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
