using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YarnScapes.Core;
using YarnScapes.Infrastructure;
using WeaveKit.Unity;

namespace YarnScapes.Presentation
{
    // Opt-in executable acceptance. Saves are isolated under the output directory.
    public sealed class FrameworkAcceptanceCapture : MonoBehaviour
    {
        public static string OutputDirectory
        {
            get
            {
                var args=Environment.GetCommandLineArgs();
                for(var i=0;i+1<args.Length;i++) if(args[i]=="--framework-acceptance-output")return Path.GetFullPath(args[i+1]);
                return null;
            }
        }
        private readonly Report report=new Report();
        private string userPath,userBefore;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(OutputDirectory!=null)new GameObject("FrameworkAcceptance").AddComponent<FrameworkAcceptanceCapture>();
        }
        private IEnumerator Start()
        {
            Directory.CreateDirectory(OutputDirectory);
            userPath=Path.Combine(Application.persistentDataPath,"yarnscapes_progress.json");
            userBefore=File.Exists(userPath)?File.ReadAllText(userPath):null;
            Application.logMessageReceived+=OnLog;
            var routine=Run();
            while(true)
            {
                object current=null; bool more;
                try { more=routine.MoveNext(); if(more)current=routine.Current; }
                catch(Exception ex) { report.failures.Add(ex.ToString()); break; }
                if(!more)break;
                yield return current;
            }
            Check("user_progress_untouched",userBefore==(File.Exists(userPath)?File.ReadAllText(userPath):null));
            var phase=Array.IndexOf(Environment.GetCommandLineArgs(),"--framework-resume")>=0?"resume":"runtime";
            File.WriteAllText(Path.Combine(OutputDirectory,phase+"_checks.json"),JsonUtility.ToJson(report,true));
            Application.Quit(report.failures.Count==0?0:2);
        }
        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(2);
            var app=FindObjectOfType<YarnScapesApp>(); Require("boot_to_library",app!=null&&app.ScreenState==AppScreen.Home&&app.Tab==HomeTab.Library);
            Canvas.ForceUpdateCanvases();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--framework-resume")>=0)
            {
                Require("settings_survive_process_restart",!app.Progress.Settings.musicEnabled);
                var ongoing=app.Progress.Records.FirstOrDefault(x=>!x.completed&&x.filledCells>0);
                Require("ongoing_survives_process_restart",ongoing!=null);
                app.OpenLevel(ongoing.levelId); yield return new WaitForSecondsRealtime(1);
                Require("cells_restored_after_process_restart",FindObjectOfType<PixelBoardRenderer>().FilledCount==ongoing.filledCells);
                Click("BackHome"); yield return new WaitForEndOfFrame();
                Require("completion_survives_process_restart",app.Progress.Records.Any(x=>x.completed));
                yield break;
            }
            yield return new WaitForEndOfFrame(); Capture("01_library.png");
            Click("NextPage"); yield return new WaitForEndOfFrame(); Require("pagination_changes_items",GameObject.Find("LevelCard_level_326863")==null);
            Click("PreviousPage"); yield return new WaitForEndOfFrame();
            Click("TabDaily"); yield return new WaitForEndOfFrame(); Require("daily_tab",app.Tab==HomeTab.Daily);
            yield return new WaitForEndOfFrame(); Capture("02_daily.png");
            Click("TabMy"); yield return new WaitForEndOfFrame(); Require("my_tab",app.Tab==HomeTab.My);
            yield return new WaitForEndOfFrame(); Capture("03_before_settings.png");
            Click("OpenSettings"); yield return new WaitForEndOfFrame(); Require("settings_page",app.ScreenState==AppScreen.Settings);
            Click("Setting_Music"); yield return new WaitForEndOfFrame(); Require("music_setting_persisted",!new LocalProgressStore(Path.Combine(OutputDirectory,"progress.json")).Settings.musicEnabled);
            yield return new WaitForEndOfFrame(); Capture("03_settings.png");
            Click("SettingsBack"); yield return new WaitForEndOfFrame();
            Click("TabLibrary"); yield return new WaitForEndOfFrame();
            Click("LevelCard_level_326863"); yield return new WaitForSecondsRealtime(1);
            Require("card_opens_correct_level",app.CurrentLevelId=="level_326863"&&app.Game!=null);
            var board=FindObjectOfType<PixelBoardRenderer>(); var viewport=FindObjectOfType<BoardViewportInput>();
            Require("music_setting_applied",!app.Game.GetComponent<AudioSource>().isPlaying);
            viewport.Fit(); Canvas.ForceUpdateCanvases();
            var seed=board.FindUnfilled(board.SelectedColor);
            ClickAt(RectTransformUtility.WorldToScreenPoint(null,board.CellWorldPosition(seed)));
            yield return new WaitForSecondsRealtime(.25f);
            Require("pointer_starts_weaving",board.FilledCount>0);
            yield return new WaitForEndOfFrame(); Capture("04_playing.png");
            var count=board.FilledCount; Click("BackHome"); yield return new WaitForEndOfFrame();
            Require("back_saves_partial_and_stops_game",app.ScreenState==AppScreen.Home&&app.Game==null&&app.Progress.Get("level_326863").filledCells>=count);
            var saved=app.Progress.Get("level_326863").filledCells;
            Click("ContinueLast"); yield return new WaitForSecondsRealtime(1);
            board=FindObjectOfType<PixelBoardRenderer>(); Require("continue_restores_exact_progress",board.FilledCount==saved);
            Click("ResetLevel"); yield return new WaitForEndOfFrame(); Click("CancelRestart"); yield return new WaitForEndOfFrame();
            Require("cancel_restart_preserves_cells",board.FilledCount==saved);
            Click("BackHome"); yield return new WaitForEndOfFrame();
            Click("TabMy"); yield return new WaitForEndOfFrame();
            Require("ongoing_gallery_has_saved_work",GameObject.Find("LevelCard_level_326863")!=null);
            yield return new WaitForEndOfFrame(); Capture("05_my.png");
            // Complete the smallest recovered artwork using the real color and cell handlers.
            // Color buttons are invoked directly when offscreen; region fills are never injected.
            var smallest=app.Repository.Catalog.levels.Where(x=>x.available).OrderBy(x=>x.paintableCells).First();
            app.OpenLevel(smallest.levelId); yield return new WaitForSecondsRealtime(1);
            board=FindObjectOfType<PixelBoardRenderer>();
            var deadline=Time.realtimeSinceStartup+180;
            while(app.ScreenState==AppScreen.Playing && board!=null && Time.realtimeSinceStartup<deadline)
            {
                if(board.IsFilling || board.FilledCount==board.TotalCount) {yield return new WaitForEndOfFrame();continue;}
                var color=Enumerable.Range(0,smallest.paletteCount).First(x=>board.Remaining(x)>0);
                GameObject.Find("Color_"+(color+1)).GetComponent<Button>().onClick.Invoke();
                board.ClickScreenPoint(RectTransformUtility.WorldToScreenPoint(null,board.CellWorldPosition(board.FindUnfilled(color))),null);
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitForSecondsRealtime(1);
            Require("natural_completion_routes_to_result",app.ScreenState==AppScreen.Complete&&app.Progress.Get(smallest.levelId).completed);
            yield return new WaitForEndOfFrame(); Capture("06_complete.png");
            Click("CompleteHome"); yield return new WaitForEndOfFrame();
            app.OpenLevel(smallest.levelId); yield return new WaitForEndOfFrame();
            Require("completed_art_opens_result_directly",app.ScreenState==AppScreen.Complete&&app.Game==null);
            Click("ReplayArtwork"); yield return new WaitForEndOfFrame(); Click("DialogCancel"); yield return new WaitForEndOfFrame();
            Require("cancel_replay_preserves_completed_work",app.Progress.Get(smallest.levelId).completed);
            Click("CompleteHome"); yield return new WaitForEndOfFrame();
            var missing=app.Repository.Catalog.levels.First(x=>!x.available);
            app.OpenLevel(missing.levelId); yield return new WaitForEndOfFrame();
            Require("missing_asset_is_explicit_and_does_not_start_game",GameObject.Find("AppDialog")!=null&&app.Game==null);
            Click("DialogCancel"); yield return new WaitForEndOfFrame();
            Check("no_duplicate_sessions",FindObjectsOfType<PixelBoardRenderer>().Length==0);
        }
        private void Capture(string name)
        {
            var texture=ScreenCapture.CaptureScreenshotAsTexture(); File.WriteAllBytes(Path.Combine(OutputDirectory,name),texture.EncodeToPNG()); Destroy(texture);
        }
        private void Click(string name)
        {
            Canvas.ForceUpdateCanvases(); var go=GameObject.Find(name);
            if(go==null)throw new Exception("Missing control: "+name);
            var rect=go.GetComponent<RectTransform>(); ClickAt(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
        }
        private void ClickAt(Vector2 point)
        {
            var data=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(data,hits);
            if(hits.Count==0)throw new Exception("No raycast at "+point);
            Debug.Log("Framework pointer " + point + " -> " + hits[0].gameObject.name);
            data.pointerCurrentRaycast=hits[0]; data.pressPosition=point;
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,data,ExecuteEvents.pointerClickHandler);
        }
        private void Check(string name,bool pass) { if(pass)report.checks.Add(name);else report.failures.Add(name); }
        private void Require(string name,bool pass) { Check(name,pass); if(!pass)throw new Exception("Failed: "+name); }
        private void OnLog(string message,string trace,LogType type) { if(type==LogType.Error||type==LogType.Exception)report.failures.Add(message); }
        private void OnDestroy() { Application.logMessageReceived-=OnLog; }
        [Serializable] private sealed class Report
        {
            public string method="UGUI raycast navigation, real region playback for one recovered level; isolated progress + separate process resume";
            public List<string> checks=new List<string>(),failures=new List<string>();
        }
    }
}
