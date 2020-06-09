using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.EditorIterationProfiler
{
    [Serializable]
    class EditorIterationProfilerTreeViewItem : TreeViewItem
    {
        public string Details
        {
            get; set;
        }
        public double Duration
        {
            get; set;
        }
    }

    public class EditorIterationProfilerTreeView : TreeView
    {
        enum ColumnId
        {
            Event,
            Details,
            Duration,
        }

        static readonly ColumnId[] k_ColumnTypes =
        {
            ColumnId.Event,
            ColumnId.Details,
            ColumnId.Duration,
        };

        bool m_UserCodeOnly;
        public bool UserCodeOnly
        {
            get => m_UserCodeOnly; 
            set
            {
                m_UserCodeOnly = value;
                EditorIterationProfilerIntegration.Instance.Settings.UserCode = value;
            }
        }

        bool m_Flatten;
        public bool Flatten
        {
            get => m_Flatten;
            set
            {
                m_Flatten = value;
                EditorIterationProfilerIntegration.Instance.Settings.Flatten = value;
            }
        }

        public EditorIterationProfilerTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header), "Header is null");
            }

            showAlternatingRowBackgrounds = true;
            showBorder = true;

            header.sortingChanged += OnSortingChanged;

            UserCodeOnly = EditorPrefs.GetBool(EditorIterationProfilerWindow.Styles.k_UserCodePref);
            Flatten = EditorPrefs.GetBool(EditorIterationProfilerWindow.Styles.k_FlattenPref);
            EditorIterationProfilerIntegration.Instance.IterationList.Updated += iterationList => Reload();
            Reload();
        }

        void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            var index = multiColumnHeader.sortedColumnIndex;
            var columnTypes = k_ColumnTypes[index];
            if (hasSearch)
            {
                //Sort(GetRows(), columnTypes, multiColumnHeader.IsSortedAscending(index));
            }
            else
            {
                //SortHierarchical(rootItem, columnTypes, multiColumnHeader.IsSortedAscending(index));
            }
            Reload();
        }

        static void Sort(IList<TreeViewItem> rows, ColumnId columnId, bool ascending)
        {
            if (rows == null)
            {
                return;
            }

            IList<TreeViewItem> sortedRows;
            switch (columnId)
            {
                case ColumnId.Details:
                {
                    sortedRows = rows.Order(x => ((EditorIterationProfilerTreeViewItem)x).Details, ascending).ToList();
                    break;
                }
                case ColumnId.Duration:
                {
                    sortedRows = rows.Order(x => ((EditorIterationProfilerTreeViewItem)x).Duration, ascending).ToList();
                    break;
                }
                default:
                {
                    sortedRows = rows.Order(x => ((EditorIterationProfilerTreeViewItem)x).displayName, ascending).ToList();
                    break;
                }
            }

            rows.Clear();
            foreach (var r in sortedRows)
            {
                rows.Add(r);
            }
        }

        static void SortHierarchical(TreeViewItem root, ColumnId columnId, bool ascending = true)
        {
            if (root == null)
            {
                return;
            }

            SortItems(root.children, columnId, ascending);
        }

        static void SortItems(IList<TreeViewItem> children, ColumnId columnId, bool ascending = true)
        {
            if (children == null)
            {
                return;
            }

            IList<TreeViewItem> sortedRows;
            switch (columnId)
            {
                case ColumnId.Details:
                {
                    sortedRows = children.Order(x => ((EditorIterationProfilerTreeViewItem)x).Details, ascending).ToList();
                    break;
                }
                case ColumnId.Duration:
                {
                    sortedRows = children.Order(x => ((EditorIterationProfilerTreeViewItem)x).Duration, ascending).ToList();
                    break;
                }
                default:
                {
                    sortedRows = children.Order(x => ((EditorIterationProfilerTreeViewItem)x).displayName, ascending).ToList();
                    break;
                }
            }

            children.Clear();
            foreach (var r in sortedRows)
            {
                children.Add(r);
            }

            foreach (var c in children)
            {
                SortItems(c.children, columnId, ascending);
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            if (hasSearch && state.selectedIDs.Count != 0)
            {
                ClearSearch();
                CollapseAll();
                FrameItem(id);
            }
            else
            {
                SetExpanded(id, !IsExpanded(id));
            }
        }

        internal void ClearSearch()
        {
            searchString = string.Empty;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new EditorIterationProfilerTreeViewItem { id = 0, depth = -1, displayName = "Root", Details = null };
            var allItems = new List<TreeViewItem>();

            var iterationList = EditorIterationProfilerIntegration.Instance.IterationList;
            var idCounter = 1;
            var iterationCounter = 1;

            var eventDataFlagsFilter = EventDataFlags.None;

            if (UserCodeOnly)
            {
                eventDataFlagsFilter |= EventDataFlags.UserCode;
            }

            if (Flatten)
            {
                eventDataFlagsFilter |= EventDataFlags.Flatten;
            }

            for (var i = 0; i < iterationList.IterationEventRoots.Count; ++i)
            {
                var eventDataList = iterationList.IterationEventRoots[i];
                var iterationItem = new EditorIterationProfilerTreeViewItem { id = idCounter++, depth = 0, displayName = string.Empty };
                double iterationDuration = 0;

                allItems.Add(iterationItem);

                foreach (var eventData in eventDataList.Events)
                {
                    if (eventData.ParentIndex < 0)
                    {
                        iterationDuration += eventData.Duration;
                        AddEventDataRecursive(eventData, allItems, ref idCounter, 1, eventDataFlagsFilter);
                    }
                }

                iterationItem.displayName = $"Iteration Event {iterationCounter++} ({iterationList.IterationEventKinds[i]})";
                iterationItem.Duration = iterationDuration;
            }

            SetupParentsAndChildrenFromDepths(root, allItems);

            return root;
        }

        static bool ShouldAddTreeViewItem(EventData eventData, EventDataFlags filter)
        {
            var flags = eventData.Flags;

            if (eventData.Kind != IterationEventKind.None)
            {
                return true;
            }

            if (filter == EventDataFlags.None)
            {
                return true;
            }

            if (filter.HasFlag(EventDataFlags.UserCode) && filter.HasFlag(EventDataFlags.Flatten))
            {
                if ((flags & filter).HasFlag(EventDataFlags.UserCode))
                {
                    if ((flags & filter).HasFlag(EventDataFlags.Flatten))
                    {
                        return false;
                    }
                    return true;
                }
                return false;
            }

            if ((flags & filter).HasFlag(EventDataFlags.UserCode))
            {
                return true;
            }

            if (!(flags & filter).HasFlag(EventDataFlags.Flatten))
            {
                if (filter.HasFlag(EventDataFlags.Flatten))
                {
                    return true;
                }
            }

            return false;
        }

        static void AddEventDataRecursive(EventData eventData, List<TreeViewItem> treeViewItems, ref int idCounter, int depth, EventDataFlags eventDataFlagsFilter)
        {
            if (ShouldAddTreeViewItem(eventData, eventDataFlagsFilter))
            {
                treeViewItems.Add(new EditorIterationProfilerTreeViewItem
                {
                    id = idCounter++,
                    depth = depth++,
                    displayName = eventData.DisplayName,
                    Details = eventData.Details,
                    Duration = eventData.Duration
                });
            }

            if (eventData.Children == null)
            {
                return;
            }

            foreach (var child in eventData.Children)
            {
                AddEventDataRecursive(child, treeViewItems, ref idCounter, depth, eventDataFlagsFilter);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (EditorIterationProfilerTreeViewItem)args.item;

            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }
        }

        void CellGUI(Rect cellRect, EditorIterationProfilerTreeViewItem item, int column, ref RowGUIArgs args)
        {
            args.rowRect = cellRect;

            switch (column)
            {
                case 0:
                {
                    base.RowGUI(args);
                    break;
                }
                case 1:
                {
                    if (!string.IsNullOrEmpty(item.Details))
                    {
                        DefaultGUI.Label(cellRect, item.Details, args.selected, args.focused);
                    }

                    break;
                }
                case 2:
                {
                    DefaultGUI.LabelRightAligned(cellRect, $"{item.Duration:0.000}", args.selected, args.focused);
                    break;
                }
            }
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Event"),
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Left,
                    minWidth = 250,
                    autoResize = true,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Details"),
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Center,
                    minWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Duration (ms)"),
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Right,
                    minWidth = 100,
                    autoResize = false,
                    allowToggleVisibility = false
                }
            };

            Assert.AreEqual(columns.Length, k_ColumnTypes.Length, L10n.Tr($"Number of columns should match number of {k_ColumnTypes}"));

            return new MultiColumnHeaderState(columns);
        }
    }

    static class ExtensionMethods
    {
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.OrderBy(selector);
            }

            return source.OrderByDescending(selector);
        }

        public static IOrderedEnumerable<T> ThenBy<T, TKey>(this IOrderedEnumerable<T> source, Func<T, TKey> selector, bool ascending)
        {
            if (ascending)
            {
                return source.ThenBy(selector);
            }
            else
            {
                return source.ThenByDescending(selector);
            }
        }
    }
}
