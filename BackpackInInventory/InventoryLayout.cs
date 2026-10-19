using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackpackInInventory
{
    /// <summary>
    /// Makes the inventory slot area scrollable and adds a grid for the backpack's slots under the inventory slots.
    /// <para>
    /// The area ("Inventory slots") holds two stacked grids: the slot placeholders ("Not available slots") and the slots
    /// themselves (<see cref="InventoryPanelUI.inventorySlotPanel"/>). Both move into a scrolled content rect under a
    /// clipping viewport, keeping their size and position; the backpack grid goes under them with the same cell layout.
    /// Everything is put back by <see cref="Remove"/>.
    /// </para>
    /// </summary>
    internal static class InventoryLayout
    {
        private sealed class SavedRect
        {
            public RectTransform Rect;
            public Vector2 AnchorMin, AnchorMax, Pivot, AnchoredPosition, SizeDelta;
        }

        private const float ScrollbarWidth = 4f;

        /// <summary>Name prefix of the objects this mod adds to the game's UI.</summary>
        private const string Prefix = "BackpackInInventory";

        private static RectTransform area;
        private static RectTransform viewport;
        private static RectTransform content;
        private static RectTransform mainGrid;
        private static GridLayoutGroup mainLayout;
        private static RectTransform backpackGrid;
        private static RectTransform scrollbar;
        private static Scroller scroller;
        private static readonly List<SavedRect> moved = new List<SavedRect>();

        internal static Transform BackpackGrid => backpackGrid;

        internal static bool IsBackpackSlot(Slot slot) => backpackGrid != null && slot.transform.parent == backpackGrid;

        /// <summary>
        /// A slot scrolled out of the viewport: hidden by the mask, but raycasts still hit it (the item info panel lies
        /// over the part below the viewport).
        /// </summary>
        internal static bool IsHiddenSlot(Slot slot, Vector2 screenPoint)
        {
            if (viewport == null || content == null || !slot.transform.IsChildOf(content))
                return false;
            Canvas canvas = viewport.GetComponentInParent<Canvas>();
            Camera camera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.rootCanvas.worldCamera;
            return !RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, camera);
        }

        /// <summary>Builds the scrolled layout once; true when it's in place.</summary>
        internal static bool Install()
        {
            if (viewport != null)
                return true;
            if (moved.Count > 0)
                Forget(); // destroyed with the scene

            GameObject panel = Inventory.instance.inventoryPanelUI.inventorySlotPanel;
            mainGrid = panel != null ? panel.transform as RectTransform : null;
            area = mainGrid != null ? mainGrid.parent as RectTransform : null;
            mainLayout = mainGrid != null ? mainGrid.GetComponent<GridLayoutGroup>() : null;
            if (area == null || mainLayout == null)
            {
                Plugin.Log.LogError("The inventory slot grid was not found (game update?); backpack slots are not shown.");
                Forget();
                return false;
            }

            viewport = NewRect(Prefix + " Viewport", area);
            viewport.SetAsFirstSibling();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, area.rect.height);

            // Move the grids (in their order) into the content, pinned to its top at the same offset and height.
            var children = new List<RectTransform>();
            foreach (Transform child in area)
                if (child != viewport && child is RectTransform rect && !child.name.StartsWith(Prefix))
                    children.Add(rect);
            // The slots draw over their placeholders. Enforced rather than copied, so a wrong order left behind by an
            // older build (it restored the two grids swapped) heals itself.
            children.Remove(mainGrid);
            children.Add(mainGrid);
            foreach (RectTransform rect in children)
            {
                moved.Add(new SavedRect
                {
                    Rect = rect, AnchorMin = rect.anchorMin, AnchorMax = rect.anchorMax,
                    Pivot = rect.pivot, AnchoredPosition = rect.anchoredPosition, SizeDelta = rect.sizeDelta,
                });
                float height = rect.rect.height;
                float top = rect.localPosition.y + rect.rect.yMax - area.rect.yMax;
                rect.SetParent(content, false);
                rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
                rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
                rect.pivot = new Vector2(rect.pivot.x, 1f);
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top);
            }

            backpackGrid = NewRect("Backpack Slots", content);
            backpackGrid.anchorMin = new Vector2(mainGrid.anchorMin.x, 1f);
            backpackGrid.anchorMax = new Vector2(mainGrid.anchorMax.x, 1f);
            backpackGrid.pivot = new Vector2(mainGrid.pivot.x, 1f);
            backpackGrid.sizeDelta = new Vector2(mainGrid.sizeDelta.x, 0f);
            backpackGrid.anchoredPosition = new Vector2(mainGrid.anchoredPosition.x, 0f);
            GridLayoutGroup grid = backpackGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = mainLayout.cellSize;
            grid.spacing = mainLayout.spacing;
            grid.startCorner = mainLayout.startCorner;
            grid.startAxis = mainLayout.startAxis;
            grid.constraint = mainLayout.constraint;
            grid.constraintCount = mainLayout.constraintCount;
            grid.padding = new RectOffset(mainLayout.padding.left, mainLayout.padding.right, 0, 0);
            grid.childAlignment = (TextAnchor)((int)mainLayout.childAlignment % 3); // same column, top row

            // A thin bar on the right edge, outside the clipped viewport; shown only when there is something to scroll.
            scrollbar = NewRect(Prefix + " Scrollbar", area);
            scrollbar.anchorMin = new Vector2(1f, 0f);
            scrollbar.anchorMax = Vector2.one;
            scrollbar.pivot = new Vector2(1f, 0.5f);
            scrollbar.sizeDelta = new Vector2(ScrollbarWidth, 0f);
            scrollbar.anchoredPosition = Vector2.zero;
            Image track = scrollbar.gameObject.AddComponent<Image>();
            track.color = new Color(1f, 1f, 1f, 0.12f);
            RectTransform handle = NewRect("Handle", scrollbar);
            handle.anchorMin = new Vector2(0f, 1f);
            handle.anchorMax = Vector2.one;
            handle.pivot = new Vector2(0.5f, 1f);
            Image handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.5f);

            scroller = viewport.gameObject.AddComponent<Scroller>();
            scroller.Init(content, viewport, scrollbar, handle, mainLayout.cellSize.y + mainLayout.spacing.y);
            handle.gameObject.AddComponent<ScrollHandle>().Scroller = scroller;
            return true;
        }

        /// <summary>Places the backpack grid under the last inventory row and sizes the scrolled content.</summary>
        internal static void Relayout()
        {
            if (viewport == null)
                return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainGrid);

            float mainBottom = float.MaxValue;
            foreach (Transform child in mainGrid)
            {
                if (!child.gameObject.activeSelf || !(child is RectTransform cell))
                    continue;
                float bottom = content.InverseTransformPoint(cell.TransformPoint(new Vector3(0f, cell.rect.yMin))).y;
                mainBottom = Mathf.Min(mainBottom, bottom);
            }
            if (mainBottom == float.MaxValue)
                mainBottom = -area.rect.height;

            int count = 0;
            foreach (Transform child in backpackGrid)
                if (child.gameObject.activeSelf)
                    count++;
            int columns = Mathf.Max(1, Columns());
            int rows = (count + columns - 1) / columns;
            float height = rows > 0 ? rows * mainLayout.cellSize.y + (rows - 1) * mainLayout.spacing.y : 0f;
            float top = mainBottom - mainLayout.spacing.y;

            backpackGrid.gameObject.SetActive(count > 0);
            backpackGrid.sizeDelta = new Vector2(backpackGrid.sizeDelta.x, height);
            backpackGrid.anchoredPosition = new Vector2(backpackGrid.anchoredPosition.x, top);
            float needed = count > 0 ? -top + height + mainLayout.padding.bottom : 0f;
            content.sizeDelta = new Vector2(0f, Mathf.Max(area.rect.height, needed));
            scroller.Refresh();
        }

        private static int Columns()
        {
            if (mainLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                return mainLayout.constraintCount;
            float width = mainGrid.rect.width - mainLayout.padding.horizontal + mainLayout.spacing.x;
            return Mathf.FloorToInt(width / (mainLayout.cellSize.x + mainLayout.spacing.x));
        }

        /// <summary>Puts the grids back where they were and removes everything the mod added.</summary>
        internal static void Remove()
        {
            if (viewport == null && moved.Count == 0)
                return;
            // The area held only these grids, so appending them in their saved order restores it.
            foreach (SavedRect saved in moved)
            {
                if (saved.Rect == null || area == null)
                    continue;
                saved.Rect.SetParent(area, false);
                saved.Rect.SetAsLastSibling();
                saved.Rect.anchorMin = saved.AnchorMin;
                saved.Rect.anchorMax = saved.AnchorMax;
                saved.Rect.pivot = saved.Pivot;
                saved.Rect.sizeDelta = saved.SizeDelta;
                saved.Rect.anchoredPosition = saved.AnchoredPosition;
            }
            // Detached, not just destroyed: Destroy is deferred, and a build loaded right after (hot reload) must not find them.
            Discard(viewport);
            Discard(scrollbar);
            Forget();
        }

        private static void Discard(RectTransform rect)
        {
            if (rect == null)
                return;
            rect.gameObject.SetActive(false);
            rect.SetParent(null, false);
            Object.Destroy(rect.gameObject);
        }

        private static void Forget()
        {
            moved.Clear();
            area = viewport = content = mainGrid = backpackGrid = scrollbar = null;
            mainLayout = null;
            scroller = null;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        internal static string Describe()
        {
            if (viewport == null)
                return "layout: not installed";
            return $"layout: area {area.rect.size}, content {content.rect.size}, offset {content.anchoredPosition.y:0}, " +
                   $"main grid {mainGrid.rect.size} cell {mainLayout.cellSize} spacing {mainLayout.spacing} " +
                   $"padding ({mainLayout.padding.left},{mainLayout.padding.right},{mainLayout.padding.top},{mainLayout.padding.bottom}) " +
                   $"align {mainLayout.childAlignment} {mainLayout.constraint} {mainLayout.constraintCount}, " +
                   $"backpack grid at {backpackGrid.anchoredPosition.y:0} size {backpackGrid.rect.size} slots {backpackGrid.childCount}";
        }
    }

    /// <summary>Mouse-wheel scrolling of the inventory slot area (no drag-to-scroll: dragging is for items).</summary>
    internal sealed class Scroller : MonoBehaviour, IScrollHandler
    {
        private RectTransform content, viewport, bar, handle;
        private float step;
        private float offset;

        internal void Init(RectTransform content, RectTransform viewport, RectTransform bar, RectTransform handle, float step)
        {
            this.content = content;
            this.viewport = viewport;
            this.bar = bar;
            this.handle = handle;
            this.step = step;
            Refresh();
        }

        private float Max => content == null ? 0f : Mathf.Max(0f, content.rect.height - viewport.rect.height);

        public void OnScroll(PointerEventData eventData) => ScrollTo(offset - eventData.scrollDelta.y * step);

        /// <summary>Drag of the bar handle, in the bar's local units.</summary>
        internal void DragHandle(float barDelta)
        {
            float barHeight = bar.rect.height;
            if (barHeight > 0f)
                ScrollTo(offset - barDelta * content.rect.height / barHeight);
        }

        internal void Refresh() => ScrollTo(offset);

        private void ScrollTo(float value)
        {
            if (content == null)
                return;
            float max = Max;
            offset = Mathf.Clamp(value, 0f, max);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, offset);

            bool scrollable = max > 0.5f;
            if (bar.gameObject.activeSelf != scrollable)
                bar.gameObject.SetActive(scrollable);
            if (!scrollable)
                return;
            float barHeight = bar.rect.height;
            float total = content.rect.height;
            handle.sizeDelta = new Vector2(0f, barHeight * viewport.rect.height / total);
            handle.anchoredPosition = new Vector2(0f, -barHeight * offset / total);
        }
    }

    internal sealed class ScrollHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        internal Scroller Scroller;

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            float scale = transform.lossyScale.y;
            if (Scroller != null && scale > 0f)
                Scroller.DragHandle(eventData.delta.y / scale);
        }
    }
}
