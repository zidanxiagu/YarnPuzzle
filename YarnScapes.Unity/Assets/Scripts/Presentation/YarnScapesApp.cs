using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YarnScapes.Core;
using YarnScapes.Infrastructure;
using U = YarnScapes.Presentation.FrameworkUI;

namespace YarnScapes.Presentation
{
    // Home state survives gameplay; only one gameplay session can exist at a time.
    public sealed class YarnScapesApp : MonoBehaviour
    {
        public FrameworkRepository Repository { get; private set; }
        public LocalProgressStore Progress { get; private set; }
        public AppScreen ScreenState { get; private set; }
        public HomeTab Tab { get; private set; }
        public string CategoryName { get; private set; }
        public string CurrentLevelId { get; private set; }
        public YarnScapesRuntime Game { get; private set; }
        private RectTransform frame;
        private GameObject canvas, page, modal;
        private readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
        private int pageIndex;
        private bool onlyAvailable, myCompleted;
        private DateTime month;
        private float libraryScroll=1;
        private string message;

        private System.Collections.IEnumerator Start()
        {
            Repository=new FrameworkRepository();
            var testPath=FrameworkAcceptanceCapture.OutputDirectory;
            Progress=new LocalProgressStore(testPath==null?null:System.IO.Path.Combine(testPath,"progress.json"),Repository.Config.settings);
            CategoryName=Repository.Config.defaultCategory;
            onlyAvailable=Repository.Config.hideUnavailableByDefault;
            month=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);
            var latest=Repository.Catalog.daily.Where(x=>x.LocalDate<=DateTime.Today).OrderByDescending(x=>x.onlineDate).FirstOrDefault();
            if(latest!=null) month=new DateTime(latest.LocalDate.Year,latest.LocalDate.Month,1);
            BuildCanvas(); ClearPage();
            Picture(page.transform,"Framework/UI/splash_app_icon",.32f,.46f,.36f,.2f);
            U.Label(page.transform,"YarnScapes",48,.15f,.36f,.7f,.07f,U.Ink,TextAnchor.MiddleCenter);
            yield return new WaitForSecondsRealtime(.5f);
            ShowHome(HomeTab.Library);
        }
        private void BuildCanvas()
        {
            canvas=new GameObject("AppCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform,false); canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            if(FindObjectOfType<EventSystem>()==null) new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            U.Panel(canvas.transform,"AppBackdrop",U.Paper);
            var go=new GameObject("AppPortraitFrame",typeof(RectTransform)); go.transform.SetParent(canvas.transform,false);
            frame=go.GetComponent<RectTransform>(); frame.anchorMin=frame.anchorMax=new Vector2(.5f,.5f); frame.sizeDelta=new Vector2(1080,1920);
        }
        private void ClearPage()
        {
            if(modal!=null) { modal.SetActive(false); Destroy(modal); modal=null; }
            if(page!=null) { page.SetActive(false); Destroy(page); }
            foreach(var sprite in sprites.Values) if(sprite!=null) Destroy(sprite);
            sprites.Clear();
            page=U.Panel(frame,"AppPage",U.Paper).gameObject;
        }
        private void Picture(Transform parent,string resource,float x,float y,float w,float h)
        {
            if(string.IsNullOrEmpty(resource)) return;
            if(!sprites.TryGetValue(resource,out var sprite))
            {
                var texture=Resources.Load<Texture2D>(resource); if(texture==null) return;
                sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
                sprites.Add(resource,sprite);
            }
            var image=U.Panel(parent,"Art",Color.white,x,y,w,h); image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false;
        }
        public void ShowHome(HomeTab tab)
        {
            if(Game!=null) { Game.gameObject.SetActive(false); Destroy(Game.gameObject); Game=null; }
            canvas.SetActive(true); ScreenState=AppScreen.Home; Tab=tab; ClearPage();
            Picture(page.transform,"Framework/UI/"+(tab==HomeTab.Library?"img_library_header":tab==HomeTab.Daily?"img_daily_header":"img_my_head"),.055f,.923f,.44f,.06f);
            var settings=U.Button(page.transform,"OpenSettings","",.87f,.927f,.09f,.05f,ShowSettings);
            Picture(settings.transform,"Framework/UI/icon_setting",.15f,.15f,.7f,.7f);
            BuildTabs();
            if(tab==HomeTab.Library) BuildLibrary();
            else if(tab==HomeTab.My) BuildMy();
            else BuildDaily();
        }
        private void BuildTabs()
        {
            var bar=U.Panel(page.transform,"HomeTabs",Color.white,0,0,1,.08f);
            var names=new[]{"library","daily","my"}; var captions=new[]{"图库","每日","我的"};
            var tabs=Repository.Config.tabs.Select(x=>Enum.TryParse<HomeTab>(x,out var t)?t:HomeTab.Library).Distinct().ToArray();
            if(tabs.Length==0) tabs=new[]{HomeTab.Library,HomeTab.Daily,HomeTab.My};
            for(var i=0;i<tabs.Length;i++)
            {
                var tab=tabs[i]; var index=(int)tab;
                var button=U.Button(bar.transform,"Tab"+tab,"",i/(float)tabs.Length,0,1f/tabs.Length,1,()=> { pageIndex=0; ShowHome(tab); });
                Picture(button.transform,"Framework/UI/icon_tab_"+names[index]+(Tab==tab?"_selected":"_normal"),.36f,.39f,.28f,.52f);
                U.Label(button.transform,captions[index],26,0,.03f,1,.34f,Tab==tab?U.Accent:U.Muted,TextAnchor.MiddleCenter);
            }
        }
        private void BuildLibrary()
        {
            var content=U.Scroll(page.transform,"CategoryStrip",.04f,.856f,.92f,.052f,true);
            content.sizeDelta=new Vector2(Repository.Catalog.categories.Length*183,0);
            var layout=content.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing=13; layout.childControlWidth=false; layout.childControlHeight=false; layout.childForceExpandWidth=false;
            foreach(var cat in Repository.Catalog.categories)
            {
                var category=cat;
                var button=U.Button(content,"Category_"+cat.name,cat.label,0,0,1,1,()=> {CategoryName=category.name;pageIndex=0;libraryScroll=1;ShowHome(HomeTab.Library);},cat.name==CategoryName);
                button.GetComponent<RectTransform>().sizeDelta=new Vector2(170,87);
            }
            // Keep the selected category visible after navigation.
            var selectedIndex=Array.FindIndex(Repository.Catalog.categories,x=>x.name==CategoryName);
            content.GetComponentInParent<ScrollRect>().horizontalNormalizedPosition=Mathf.Clamp01(selectedIndex/(float)Math.Max(1,Repository.Catalog.categories.Length-5));
            var items=Repository.Category(CategoryName,onlyAvailable).Where(x=>Progress.Settings.showCompleted || Progress.Get(x.levelId)?.completed!=true).ToArray();
            U.Label(page.transform,(onlyAvailable?"可离线编织":"全部目录")+" · "+items.Length+" 张",25,.055f,.818f,.58f,.03f,U.Muted);
            U.Button(page.transform,"ToggleAvailability",onlyAvailable?"查看完整目录":"只看可玩",.72f,.816f,.23f,.035f,()=>{onlyAvailable=!onlyAvailable;pageIndex=0;ShowHome(Tab);});
            BuildGrid(items,.16f,.64f,true);
            var last=Repository.Find(Progress.LastLevelId); var saved=last==null?null:Progress.Get(last.levelId);
            if(last!=null && saved!=null && !saved.completed && saved.filledCells>0)
                U.Button(page.transform,"ContinueLast","继续上次作品  ·  "+Percent(saved)+"%",.055f,.094f,.89f,.045f,()=>OpenLevel(last.levelId),true);
        }
        private void BuildGrid(CatalogLevel[] items,float bottom,float height,bool rememberScroll=false)
        {
            var count=Math.Max(1,(items.Length+Repository.Config.pageSize-1)/Repository.Config.pageSize);
            pageIndex=Mathf.Clamp(pageIndex,0,count-1);
            var content=U.Scroll(page.transform,"LevelGrid",.045f,bottom,.91f,height);
            var grid=content.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize=new Vector2(477,445); grid.spacing=new Vector2(25,25);
            grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=2; grid.childAlignment=TextAnchor.UpperCenter;
            var slice=items.Skip(pageIndex*Repository.Config.pageSize).Take(Repository.Config.pageSize).ToArray();
            content.sizeDelta=new Vector2(0,Mathf.CeilToInt(slice.Length/2f)*470);
            foreach(var entry in slice) BuildCard(content,entry);
            if(items.Length==0)
            {
                Picture(page.transform,"Framework/UI/my_page_empty_"+(myCompleted?"completed":"in_progress")+"_icon",.34f,.42f,.32f,.20f);
                U.Label(page.transform,"这里还没有作品\n去图库挑一张开始编织吧",32,.15f,.31f,.7f,.1f,U.Muted,TextAnchor.MiddleCenter);
            }
            U.Button(page.transform,"PreviousPage","上一页",.055f,bottom-.038f,.22f,.033f,()=>{pageIndex--;libraryScroll=1;ShowHome(Tab);}).interactable=pageIndex>0;
            U.Label(page.transform,(pageIndex+1)+" / "+count,25,.36f,bottom-.038f,.28f,.033f,U.Muted,TextAnchor.MiddleCenter);
            U.Button(page.transform,"NextPage","下一页",.725f,bottom-.038f,.22f,.033f,()=>{pageIndex++;libraryScroll=1;ShowHome(Tab);}).interactable=pageIndex<count-1;
            if(rememberScroll)
            {
                Canvas.ForceUpdateCanvases(); var scroll=content.GetComponentInParent<ScrollRect>(); scroll.verticalNormalizedPosition=libraryScroll;
                scroll.onValueChanged.AddListener(v=>libraryScroll=v.y);
            }
        }
        private void BuildCard(Transform parent,CatalogLevel entry)
        {
            var button=U.Button(parent,"LevelCard_"+entry.levelId,"",0,0,1,1,()=>OpenLevel(entry.levelId));
            var saved=Progress.Get(entry.levelId);
            if(entry.available) Picture(button.transform,entry.sourceResource,.05f,.18f,.9f,.78f);
            else U.Label(button.transform,"图片未缓存",33,.06f,.3f,.88f,.35f,U.Muted,TextAnchor.MiddleCenter);
            U.Label(button.transform,entry.levelId.Replace("level_","#"),25,.05f,.025f,.41f,.12f,U.Muted);
            U.Label(button.transform,!entry.available?"暂不可玩":saved?.completed==true?"已完成":saved?.filledCells>0?Percent(saved)+"%":"开始编织",26,.43f,.025f,.51f,.12f,saved?.completed==true?new Color(.22f,.62f,.46f):U.Accent,TextAnchor.MiddleRight);
        }
        private static int Percent(LevelProgressRecord record) => record==null || record.totalCells==0?0:Mathf.FloorToInt(100f*record.filledCells/record.totalCells);
        private void BuildMy()
        {
            var done=Progress.Records.Count(x=>x.completed); var ongoing=Progress.Records.Count(x=>x.filledCells>0&&!x.completed);
            U.Label(page.transform,"已完成 "+done+" 张  ·  进行中 "+ongoing+" 张",28,.055f,.867f,.8f,.035f,U.Muted);
            U.Button(page.transform,"MyInProgress","进行中",.055f,.807f,.43f,.046f,()=>{myCompleted=false;pageIndex=0;ShowHome(Tab);},!myCompleted);
            U.Button(page.transform,"MyCompleted","已完成",.515f,.807f,.43f,.046f,()=>{myCompleted=true;pageIndex=0;ShowHome(Tab);},myCompleted);
            var items=Progress.Records.Reverse().Where(x=>x.completed==myCompleted&&x.filledCells>0).Select(x=>Repository.Find(x.levelId)).Where(x=>x!=null&&x.available).ToArray();
            BuildGrid(items,.15f,.63f);
        }
        private void BuildDaily()
        {
            U.Label(page.transform,"离线日历 · 按原始发布日期展示",27,.055f,.864f,.9f,.04f,U.Muted);
            U.Button(page.transform,"PreviousMonth","‹",.06f,.796f,.14f,.045f,()=>{month=month.AddMonths(-1);ShowHome(Tab);});
            U.Label(page.transform,month.ToString("yyyy 年 MM 月"),36,.22f,.793f,.56f,.05f,U.Ink,TextAnchor.MiddleCenter);
            U.Button(page.transform,"NextMonth","›",.8f,.796f,.14f,.045f,()=>{month=month.AddMonths(1);ShowHome(Tab);});
            var cal=U.Panel(page.transform,"Calendar",Color.white,.045f,.39f,.91f,.39f);
            var weekdays=new[]{"日","一","二","三","四","五","六"};
            for(var i=0;i<7;i++) U.Label(cal.transform,weekdays[i],26,i/7f,.88f,1/7f,.1f,U.Muted,TextAnchor.MiddleCenter);
            var offset=(int)month.DayOfWeek;
            for(var day=1;day<=DateTime.DaysInMonth(month.Year,month.Month);day++)
            {
                var date=month.AddDays(day-1); var daily=Repository.Catalog.daily.FirstOrDefault(x=>x.LocalDate==date);
                var item=daily==null?null:Repository.Find(daily.levelId); var slot=offset+day-1;
                var caption=day.ToString()+(daily==null?"":item?.available==true?"\n可编织":"\n未缓存");
                var button=U.Button(cal.transform,"DailyDay_"+day,caption,(slot%7)/7f,.74f-(slot/7)*.145f,1/7f,.14f,()=>{if(item!=null)OpenLevel(item.levelId);},date==DateTime.Today);
                button.interactable=item!=null && date<=DateTime.Today;
                button.GetComponentInChildren<Text>().fontSize=24;
            }
            var today=Repository.Catalog.daily.FirstOrDefault(x=>x.LocalDate==DateTime.Today);
            U.Label(page.transform,today==null?"今天还没有离线每日作品":"今天的每日作品",34,.06f,.30f,.88f,.055f,U.Ink,TextAnchor.MiddleCenter);
            U.Label(page.transform,"已保留 "+Repository.Catalog.daily.Length+" 条原版每日记录。\n未缓存图片的日期，暂时无法开始编织。",27,.09f,.205f,.82f,.085f,U.Muted,TextAnchor.MiddleCenter);
            if(today!=null) U.Button(page.transform,"OpenToday","查看今日作品",.22f,.13f,.56f,.055f,()=>OpenLevel(today.levelId),true);
        }
        public void OpenLevel(string id)
        {
            if(ScreenState==AppScreen.Playing) return;
            var entry=Repository.Find(id);
            if(entry==null || !entry.available) { Notice("这张作品的图片没有包含在本地资源中，暂时无法编织。"); return; }
            if(Progress.Get(id)?.completed==true) { ShowComplete(id); return; }
            StartGame(id);
        }
        private void StartGame(string id)
        {
            if(new ResourcesLevelRepository().Load(id)==null) { Notice("关卡配置无法读取，请检查关卡文件。"); return; }
            CurrentLevelId=id; ScreenState=AppScreen.Playing; canvas.SetActive(false);
            var obj=new GameObject("ActiveGameSession"); obj.transform.SetParent(transform,false);
            Game=obj.AddComponent<YarnScapesRuntime>(); Game.LaunchLevelId=id; Game.SharedProgress=Progress;
            Game.HomeRequested=()=>ShowHome(Tab); Game.LevelCompleted=ShowComplete;
        }
        public void ShowComplete(string id)
        {
            if(Game!=null) { Game.gameObject.SetActive(false); Destroy(Game.gameObject); Game=null; }
            canvas.SetActive(true); CurrentLevelId=id; ScreenState=AppScreen.Complete; ClearPage();
            var entry=Repository.Find(id);
            U.Label(page.transform,"编织完成！",55,.06f,.865f,.88f,.075f,U.Ink,TextAnchor.MiddleCenter);
            U.Label(page.transform,"作品已保存到「我的」",28,.1f,.81f,.8f,.04f,U.Muted,TextAnchor.MiddleCenter);
            Picture(page.transform,entry.sourceResource,.09f,.33f,.82f,.45f);
            U.Button(page.transform,"CompleteHome","返回图库",.08f,.205f,.84f,.065f,()=>{pageIndex=0;ShowHome(HomeTab.Library);},true);
            var next=Repository.NextAvailable(id,CategoryName);
            U.Button(page.transform,"NextArtwork",next==null?"本分类已到末尾":"下一张作品",.08f,.115f,.39f,.06f,()=>{if(next!=null)OpenLevel(next.levelId);}).interactable=next!=null;
            U.Button(page.transform,"ReplayArtwork","重新编织",.53f,.115f,.39f,.06f,()=>Confirm("重新编织会清除这张作品的进度。",()=>{Progress.DeleteLevel(id);StartGame(id);}));
        }
        private void ShowSettings()
        {
            ScreenState=AppScreen.Settings; ClearPage();
            U.Label(page.transform,"设置",55,.06f,.89f,.8f,.07f);
            var settings=Progress.Settings;
            SettingRow("Music","背景音乐",settings.musicEnabled,.73f,()=>settings.musicEnabled=!settings.musicEnabled);
            SettingRow("Sound","编织音效",settings.soundEnabled,.63f,()=>settings.soundEnabled=!settings.soundEnabled);
            SettingRow("AutoColor","完成颜色后自动切换",settings.autoSwitchColor,.53f,()=>settings.autoSwitchColor=!settings.autoSwitchColor);
            SettingRow("ShowCompleted","图库显示已完成作品",settings.showCompleted,.43f,()=>settings.showCompleted=!settings.showCompleted);
            U.Button(page.transform,"SettingsBack","返回",.07f,.12f,.86f,.065f,()=>ShowHome(Tab),true);
            if(!string.IsNullOrEmpty(message)) U.Label(page.transform,message,26,.08f,.25f,.84f,.1f,U.Muted);
        }
        private void SettingRow(string name,string caption,bool state,float y,Action action)
        {
            U.Label(page.transform,caption,31,.07f,y,.65f,.06f);
            U.Button(page.transform,"Setting_"+name,state?"开启":"关闭",.75f,y,.18f,.06f,()=>{action();Progress.SaveSettings();message=Progress.LastError;ShowSettings();},state);
        }
        private void Notice(string text) { Confirm(text,null); }
        private void Confirm(string text,Action confirm)
        {
            if(modal!=null) return;
            modal=U.Panel(frame,"AppDialog",new Color(0,0,0,.38f)).gameObject;
            var box=U.Panel(modal.transform,"DialogBody",U.Paper,.07f,.34f,.86f,.32f);
            U.Label(box.transform,text,33,.08f,.38f,.84f,.52f,U.Ink,TextAnchor.MiddleCenter);
            U.Button(box.transform,"DialogCancel",confirm==null?"知道了":"取消",.08f,.09f,confirm==null?.84f:.39f,.19f,CloseModal);
            if(confirm!=null) U.Button(box.transform,"DialogConfirm","确定",.53f,.09f,.39f,.19f,()=>{CloseModal();confirm();},true);
        }
        private void CloseModal() { if(modal!=null) {modal.SetActive(false);Destroy(modal);modal=null;} }
        private void Update()
        {
            if(ScreenState==AppScreen.Playing || !Input.GetKeyDown(KeyCode.Escape))return;
            if(modal!=null) CloseModal();
            else if(ScreenState!=AppScreen.Home || Tab!=HomeTab.Library) ShowHome(HomeTab.Library);
            else Confirm("退出游戏？当前作品的进度已经自动保存。",Application.Quit);
        }
        private void OnDestroy() { foreach(var sprite in sprites.Values) if(sprite!=null) Destroy(sprite); }
    }
}
