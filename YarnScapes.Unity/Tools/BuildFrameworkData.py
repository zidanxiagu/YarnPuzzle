"""Recover the application catalog directly from the APK, without network access.

Run with the shared office Python (-I -X utf8). Does not alter pixel maps.
"""
import hashlib
import json
import shutil
import zipfile
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[1]
ASSETS = PROJECT / 'Assets'
ROOT = PROJECT.parent


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    newline = '\r\n' if path.exists() and b'\r\n' in path.read_bytes() else '\n'
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline=newline)


def main():
    apk = ROOT / 'base.apk'
    local = json.loads((ASSETS / 'Resources/Levels/level_catalog.json').read_text(encoding='utf-8-sig'))['levels']
    local = {v['levelId']: v for v in local}
    labels = dict(zip(['New','For You','Animal','Objects','Festival','People','Nature','Cute','Mandala','Emoji','Food','Fantasy','House','Fashion','Faith'],
                      ['最新','为你推荐','动物','物品','节日','人物','自然','可爱','曼陀罗','表情','美食','幻想','房屋','时尚','信仰']))
    entries, categories, sources, daily = {}, [], [], []
    with zipfile.ZipFile(apk) as archive:
        def read(name):
            path = 'assets/init_data/' + name
            raw = archive.read(path)
            sources.append({'apkPath': path, 'sha256': hashlib.sha256(raw).hexdigest()})
            # Preserve exact source JSON, including unknown nested fields.
            dest = ASSETS / 'Original/FrameworkSnapshot' / (name + '.json')
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(raw)
            return json.loads(raw)
        initial = read('init_data')
        for cat in initial['category'] + [{'category_uid':'daily_data','category_name':'Daily'}]:
            rows = read(cat['category_uid'])
            ids = []
            for order, row in enumerate(rows):
                key = 'level_' + str(row['id'])
                ids.append(key)
                if key not in entries:
                    item = dict(local.get(key, {}))
                    item.update(levelId=key, paintId=row['paint_id'], categories=row.get('category_names',''),
                                sourceUrl=row.get('i512_url',''), pixelMapUrl=row.get('finished_url',''),
                                gifUrl=row.get('gif_url',''), unlockType=row.get('unlock_type',0),
                                cornerMarker=row.get('corner_marker',0), distributeFlag=row.get('distribute_flag',0),
                                onlineDate=row.get('online_date',0), timeZone=row.get('time_zone','UTC'),
                                available=key in local, evidenceStatus='recovered from base.apk assets/init_data',
                                width=0, height=0, paletteCount=0, paintableCells=0)
                    if key in local:
                        file = ASSETS / 'Resources/Levels' / (key+'.json')
                        definition = json.loads(file.read_text(encoding='utf-8-sig'))
                        for field in ['paintId','categories','sourceUrl','pixelMapUrl','gifUrl','unlockType','cornerMarker','distributeFlag','onlineDate','timeZone']:
                            definition[field] = item[field]
                        definition['dataEvidenceStatus'] = 'pixel map recovered; original runtime cache comparison pending'
                        definition['animationEvidenceStatus'] = 'reimplemented; timing unverified'
                        definition['rewardGems'] = 0
                        definition['starThreshold'] = 0
                        definition.setdefault('routeStepSeconds', 0.045)
                        write(file, definition)
                        item.update(width=definition['width'], height=definition['height'], paletteCount=len(definition['palette']),
                                    paintableCells=sum(v>=0 for v in definition['cells']))
                    entries[key] = item
                if cat['category_uid']=='daily_data':
                    daily.append({'levelId':key,'onlineDate':row.get('online_date',0),'timeZone':row.get('time_zone','UTC'),'sort':row.get('sort',order)})
            if cat['category_uid']!='daily_data':
                categories.append({'id':str(cat['id']),'uid':cat['category_uid'],'name':cat['category_name'],
                                   'label':labels.get(cat['category_name'],cat['category_name']),
                                   'newTag':cat.get('new_tag',False),'forYouTag':cat.get('for_your_tag',False),'levelIds':ids})
        assert set(local).issubset(entries), 'Local levels missing from source catalog'
        catalog={'schemaVersion':2,'sourceApkSha256':hashlib.sha256(apk.read_bytes()).hexdigest(),
                 'categories':categories,'levels':list(entries.values()),'daily':daily}
        write(ASSETS/'Resources/Framework/catalog.json',catalog)
        write(ASSETS/'Resources/Framework/app_config.json',{
            'schemaVersion':1,'defaultCategory':'For You','pageSize':12,
            'tabs':['Library','Daily','My'],'hideUnavailableByDefault':True,
            'settings':{'musicEnabled':True,'soundEnabled':True,'autoSwitchColor':True,'showCompleted':True},
            'evidenceStatus':'navigation/categories recovered; Unity layout and pagination reimplemented',
            'remotePolicy':'offline snapshot; no synthetic daily selection, purchases, rewards or remote experiments'})
        report={'sourceApkSha256':catalog['sourceApkSha256'],'categoryCount':len(categories),
                'categoryRecords':sum(len(c['levelIds']) for c in categories),'dailyRecords':len(daily),
                'uniqueMetadataLevels':len(entries),'playableLevels':sum(e['available'] for e in entries.values()),
                'missingLocalLevels':[e['levelId'] for e in entries.values() if not e['available']],
                'sources':sources,'scope':'All APK metadata; only recovered local image pairs are playable. Not a claim of complete server content.'}
        write(ASSETS/'Original/framework_recovery_manifest.json',report)
    ui=ASSETS/'Resources/Framework/UI'
    ui.mkdir(parents=True,exist_ok=True)
    names=['img_library_header','img_daily_header','img_my_head','icon_setting','splash_app_icon',
           'my_page_empty_in_progress_icon','my_page_empty_completed_icon','bg_daily_today',
           'icon_back_circle'] + ['icon_tab_'+tab+'_'+state for tab in ['library','daily','my'] for state in ['normal','selected']]
    copied=[]
    for name in names:
        for folder in ['-xxhdpi','-xhdpi','-hdpi','']:
            source=ASSETS/'Original/Textures'/folder/(name+'.png')
            if source.exists():
                shutil.copyfile(source,ui/(name+'.png'))
                copied.append(name)
                break
    print(json.dumps({k:v for k,v in report.items() if k not in ('sources','missingLocalLevels')},ensure_ascii=False))
    print('UI assets:', ', '.join(copied))


if __name__=='__main__':
    main()
