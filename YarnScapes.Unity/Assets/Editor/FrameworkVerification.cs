using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using YarnScapes.Core;
using YarnScapes.Infrastructure;

namespace YarnScapes.Editor
{
    public static class FrameworkVerification
    {
        private static readonly List<string> checks=new List<string>();
        public const string Output="Acceptance/2026-11-16/framework";
        [MenuItem("YarnScapes/Verify Framework and Build")]
        public static void VerifyAndBuild() { Verify(); ProjectBuilder.BuildWindowsPlayer(); }
        public static void Verify()
        {
            checks.Clear(); Directory.CreateDirectory(Output);
            var repo=new FrameworkRepository(); var local=new ResourcesLevelRepository();
            Check("15 original categories",repo.Catalog.categories.Length==15);
            Check("577 unique source entries",repo.Catalog.levels.Length==577);
            Check("103 playable entries",repo.Catalog.levels.Count(x=>x.available)==103);
            Check("10 original dated daily entries",repo.Catalog.daily.Length==10);
            Check("source category records preserved",repo.Catalog.categories.Sum(x=>x.levelIds.Length)==652);
            foreach(var cat in repo.Catalog.categories)
                Check("category references "+cat.name,cat.levelIds.All(id=>repo.Find(id)!=null));
            foreach(var entry in repo.Catalog.levels.Where(x=>x.available))
            {
                var level=local.Load(entry.levelId);
                Check("valid configuration "+entry.levelId,level!=null);
                Check("metadata identity "+entry.levelId,level.paintId==entry.paintId && level.width==entry.width && level.height==entry.height);
                Check("complete resources "+entry.levelId,Resources.Load<Texture2D>(entry.sourceResource)!=null && Resources.Load<Texture2D>(level.finishedImageResource)!=null && Resources.Load<Texture2D>(level.unfilledImageResource)!=null);
            }
            Check("category membership survives deduplication",repo.Category("People",true).Any(x=>x.levelId=="level_374987") && repo.Category("Fantasy",true).Any(x=>x.levelId=="level_374987"));
            Check("missing assets excluded from offline view",repo.Catalog.categories.All(x=>repo.Category(x.name,true).All(e=>e.available)));
            var definition=local.Load("level_326863");
            var temp=Path.Combine(Path.GetTempPath(),"YarnFrameworkTest_"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var progress=new LocalProgressStore(temp);
                var valid=Enumerable.Range(0,definition.cells.Length).Where(i=>definition.cells[i]>=0).ToArray();
                progress.SaveLevel(definition,999,true,new[]{valid[0],valid[0],-1,int.MaxValue});
                Check("normalize progress and reject false completion",progress.Get(definition.levelId).filledCells==1&&!progress.Get(definition.levelId).completed);
                progress.Settings.musicEnabled=false; progress.SaveSettings();
                var restored=new LocalProgressStore(temp);
                Check("progress survives restart",restored.Get(definition.levelId).filledIndices.SequenceEqual(new[]{valid[0]}));
                Check("settings survive restart",!restored.Settings.musicEnabled);
                Check("last work survives restart",restored.LastLevelId==definition.levelId);
                restored.SaveLevel(definition,valid.Length,true,valid);
                Check("completion persisted",new LocalProgressStore(temp).Get(definition.levelId).completed);
                restored.SaveSettings(); File.WriteAllText(temp,"{broken");
                Check("corrupt primary recovers backup",new LocalProgressStore(temp).Get(definition.levelId).completed);
                restored.DeleteLevel(definition.levelId);
                Check("restart clears only chosen work",new LocalProgressStore(temp).Get(definition.levelId)==null);
                var wrong=new LevelDefinition{levelId="bad",width=1,height=1,palette=definition.palette,cells=new[]{999}};
                Check("invalid cells rejected",ResourcesLevelRepository.Validate(wrong)!=null);
            }
            finally { foreach(var suffix in new[]{"",".tmp",".bak"}) if(File.Exists(temp+suffix))File.Delete(temp+suffix); }
            File.WriteAllText(Output+"/configuration_checks.json",JsonUtility.ToJson(new Report{passed=checks.Count,checks=checks.ToArray()},true));
            Debug.Log("FRAMEWORK_CHECKS_PASSED="+checks.Count);
        }
        private static void Check(string name,bool value) { if(!value)throw new Exception("Framework check failed: "+name); checks.Add(name); }
        [Serializable] private sealed class Report { public int passed; public string[] checks; }
    }

    public sealed class FrameworkCatalogWindow : EditorWindow
    {
        private FrameworkRepository repository;
        private Vector2 scroll;
        private string search="";
        private bool availableOnly=true;
        [MenuItem("YarnScapes/Level Configuration Catalog")]
        public static void Open() => GetWindow<FrameworkCatalogWindow>("关卡配置");
        private void OnGUI()
        {
            if(repository==null)repository=new FrameworkRepository();
            EditorGUILayout.LabelField("原包关卡目录",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("双击/打开配置可编辑每关 JSON。分类归属及顺序在 Framework/catalog.json。修改后运行 Verify Framework and Build。",MessageType.Info);
            search=EditorGUILayout.TextField("ID / 分类 / paintId",search);
            availableOnly=EditorGUILayout.Toggle("仅显示可玩关卡",availableOnly);
            scroll=EditorGUILayout.BeginScrollView(scroll);
            foreach(var entry in repository.Catalog.levels.Where(x=>(!availableOnly||x.available)&&
                (x.levelId+" "+x.paintId+" "+x.categories).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(entry.levelId+"   "+(entry.available?entry.width+"×"+entry.height+" / "+entry.paletteCount+" 色":"缺少本地图像"));
                if(entry.available && GUILayout.Button("打开配置",GUILayout.Width(90)))
                {
                    var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Levels/"+entry.levelId+".json");
                    Selection.activeObject=asset; EditorGUIUtility.PingObject(asset); AssetDatabase.OpenAsset(asset);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
