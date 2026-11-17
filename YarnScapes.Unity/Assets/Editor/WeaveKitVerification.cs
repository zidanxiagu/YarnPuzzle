using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WeaveKit.Core;
using YarnScapes.Core;
using YarnScapes.Infrastructure;
using YarnScapes.Presentation;

namespace YarnScapes.Editor
{
    /// <summary>Batch-mode contract checks; no dependency on the Unity Test Framework package.</summary>
    public static class WeaveKitVerification
    {
        private static int checks;
        private static readonly List<string> results = new List<string>();
        private const string Output = "Acceptance/2026-11-10/refactor-reuse-v1";
        [MenuItem("YarnScapes/Verify WeaveKit and Build")]
        public static void VerifyAndBuild()
        {
            Verify();
            ProjectBuilder.BuildWindowsPlayer();
        }
        public static void Verify()
        {
            checks = 0; results.Clear();
            // Do not access Graphic.canvasRenderer here: its lazy getter can hide
            // a missing dependency that prevents rendering in Editor Play Mode.
            var threadObject = new GameObject("ThreadDependencyCheck", typeof(RectTransform), typeof(WeaveKit.Unity.YarnThreadGraphic));
            try { Check("thread_creates_canvas_renderer_before_canvas_rebuild", threadObject.GetComponent<CanvasRenderer>() != null); }
            finally { UnityEngine.Object.DestroyImmediate(threadObject); }
            var input = new[] { 0, -1, 1, -1, 0, 1, 1, 1, 0 };
            var grid = new GridDefinition(3, 3, 2, input);
            input[0] = 1;
            Check("definition_owns_immutable_copy", grid.Cells[0] == 0);
            Check("paintable_count", grid.TotalCount == 7);
            Throws<ArgumentException>("reject_mismatched_dimensions", () => new GridDefinition(2, 3, 1, new int[3]));
            Throws<ArgumentException>("reject_invalid_palette_id", () => new GridDefinition(1, 1, 1, new[] { 1 }));
            Throws<ArgumentException>("reject_ambiguous_empty_id", () => new GridDefinition(1, 1, 1, new[] { 0 }, 0));
            Throws<ArgumentOutOfRangeException>("reject_zero_dimensions", () => new GridDefinition(0, 1, 1, Array.Empty<int>()));
            var state = new BoardState(grid);
            var four = new DepthFirstRegionRoutePlanner(GridConnectivity.Four);
            var eight = new DepthFirstRegionRoutePlanner(GridConnectivity.Eight);
            Check("four_neighbors_separate_diagonals", four.Build(state, 0, 0).SequenceEqual(new[] { 0 }));
            Check("eight_neighbors_trace_and_backtrack", eight.Build(state, 0, 0).SequenceEqual(new[] { 0, 4, 8, 4, 0 }));
            Check("wrong_color_does_not_plan", eight.Build(state, 0, 1).Count == 0);
            Check("negative_seed_does_not_plan", eight.Build(state, -1, 0).Count == 0);
            Check("out_of_range_seed_does_not_plan", eight.Build(state, 9, 0).Count == 0);
            Check("restore_skips_duplicate_empty_invalid", state.Restore(new[] { 0, 0, -1, 9, 1, 2 }).SequenceEqual(new[] { 0, 2 }));
            Check("restore_updates_counters", state.FilledCount == 2 && state.Remaining(0) == 2 && state.Remaining(1) == 3);
            var saved = state.GetFilledIndices(); saved[0] = 8;
            Check("snapshot_is_independent", state.IsFilled(0) && !state.IsFilled(8));
            var session = new FillSession(state);
            Check("begin_region", session.TryStart(4, 0));
            Check("reject_overlapping_playback", !session.TryStart(5, 1));
            Throws<InvalidOperationException>("restore_requires_idle_session", () => session.Restore(new[] { 5 }));
            Check("step_advances_once", session.Advance(out var first) && first.Index == 4 && first.NewlyFilled);
            session.Cancel();
            Check("cancel_keeps_completed_progress", !session.IsPlaying && state.FilledCount == 3 && !session.Advance(out _));
            Check("resume_remaining_region", session.TryStart(8, 0));
            while (session.Advance(out _)) { }
            Check("resume_finishes_without_double_counting", state.FilledCount == 4 && state.Remaining(0) == 0);
            session.Reset();
            Check("reset_clears_state_and_route", state.FilledCount == 0 && state.Remaining(0) == 3 && !session.IsPlaying);
            Check("null_restore_is_safe", session.Restore(null).Length == 0);

            // Random boards compare traversal against an independent BFS region oracle.
            var random = new System.Random(731);
            for (var sample = 0; sample < 100; sample++)
            {
                var data = Enumerable.Range(0, 63).Select(_ => random.Next(-1, 3)).ToArray();
                var test = new BoardState(new GridDefinition(9, 7, 3, data));
                test.Restore(Enumerable.Range(0, 63).Where(_ => random.Next(5) == 0));
                foreach (var connectivity in new[] { GridConnectivity.Four, GridConnectivity.Eight })
                {
                    var planner = new DepthFirstRegionRoutePlanner(connectivity);
                    for (var color = 0; color < 3; color++)
                    {
                        var seed = test.FindUnfilled(color);
                        var route = planner.Build(test, seed, color);
                        var expected = Region(test, seed, color, connectivity == GridConnectivity.Eight);
                        if (!expected.SetEquals(route)) throw new Exception("Random route membership differs from BFS.");
                        if (route.Count != Math.Max(0, 2 * expected.Count - 1)) throw new Exception("Backtracking route length differs.");
                        for (var i = 1; i < route.Count; i++)
                        {
                            var dx = Math.Abs(route[i] % 9 - route[i - 1] % 9);
                            var dy = Math.Abs(route[i] / 9 - route[i - 1] / 9);
                            if (dx > 1 || dy > 1 || dx + dy == 0 || (connectivity == GridConnectivity.Four && dx + dy != 1))
                                throw new Exception("Route jumps between non-adjacent cells.");
                        }
                    }
                }
            }
            Check("600_random_routes_match_independent_bfs_and_adjacency", true);

            var level = new ResourcesLevelRepository().Load("level_326863");
            var live = new FillSession(new BoardState(YarnScapesComposition.Grid(level)));
            var regions = 0; var steps = 0; var uniqueFills = 0;
            for (var color = 0; color < level.palette.Length; color++)
            {
                int seed;
                while ((seed = live.State.FindUnfilled(color)) >= 0)
                {
                    live.TryStart(seed, color); regions++;
                    while (live.Advance(out var step)) { steps++; if (step.NewlyFilled) uniqueFills++; }
                }
            }
            Check("real_level_1334_cells_32_regions", live.State.IsComplete && uniqueFills == 1334 && regions == 32);
            Check("real_level_keeps_backtracking_steps", steps == 2 * 1334 - 32);
            var restored = new BoardState(live.State.Definition); restored.Restore(live.State.GetFilledIndices());
            Check("real_level_snapshot_roundtrip", restored.IsComplete && restored.GetFilledIndices().SequenceEqual(live.State.GetFilledIndices()));

            var temporary = Path.Combine(Path.GetTempPath(), "weavekit-progress-" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                new LocalProgressStore(temporary).SaveLevel(level, restored.FilledCount, true, restored.GetFilledIndices());
                var record = new LocalProgressStore(temporary).Get(level.levelId);
                Check("existing_save_schema_roundtrip", record.completed && record.totalCells == 1334 && record.rewardGems == level.rewardGems && record.filledIndices.SequenceEqual(restored.GetFilledIndices()));
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            Check("core_has_no_unity_reference", !typeof(GridDefinition).Assembly.GetReferencedAssemblies().Any(x => x.Name.StartsWith("Unity")));
            Check("package_does_not_reference_application", !typeof(WeaveKit.Unity.PixelBoardRenderer).Assembly.GetReferencedAssemblies().Any(x => x.Name.StartsWith("Assembly-CSharp") || x.Name.StartsWith("YarnScapes")));
            Directory.CreateDirectory(Output);
            File.WriteAllLines(Path.Combine(Output, "contract-checks.txt"), results);
            Debug.Log("WEAVEKIT_CONTRACTS PASS checks=" + checks + " randomized_routes=600 regions=" + regions + " steps=" + steps);
        }
        private static HashSet<int> Region(BoardState state, int seed, int color, bool diagonal)
        {
            var result = new HashSet<int>();
            if (!state.CanFill(seed, color)) return result;
            var queue = new Queue<int>(); queue.Enqueue(seed); result.Add(seed);
            var grid = state.Definition;
            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                for (var candidate = 0; candidate < grid.Cells.Count; candidate++)
                {
                    var dx = Math.Abs(candidate % grid.Width - index % grid.Width);
                    var dy = Math.Abs(candidate / grid.Width - index / grid.Width);
                    if (dx + dy == 0 || dx > 1 || dy > 1 || (!diagonal && dx + dy != 1)) continue;
                    if (state.CanFill(candidate, color) && result.Add(candidate)) queue.Enqueue(candidate);
                }
            }
            return result;
        }
        private static void Check(string name, bool passed)
        {
            if (!passed) throw new Exception("WeaveKit contract failed: " + name);
            checks++; results.Add("PASS " + name);
        }
        private static void Throws<T>(string name, Action action) where T : Exception
        {
            try { action(); } catch (T) { Check(name, true); return; }
            throw new Exception("Expected " + typeof(T).Name + ": " + name);
        }
    }
}
