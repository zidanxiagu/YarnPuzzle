"""Verify the generated catalog against APK bytes and every playable pixel map."""
import hashlib
import json
from pathlib import Path
import sys
import zipfile

sys.path.insert(0, str(Path(__file__).resolve().parent))
from AuditLevel import analyze


def main():
    project=Path(__file__).resolve().parents[1]
    assets=project/'Assets'
    catalog=json.loads((assets/'Resources/Framework/catalog.json').read_text(encoding='utf-8-sig'))
    checks=[]
    with zipfile.ZipFile(project.parent/'base.apk') as apk:
        for path in (assets/'Original/FrameworkSnapshot').glob('*.json'):
            assert path.read_bytes()==apk.read('assets/init_data/'+path.stem), path
            checks.append('source bytes '+path.stem)
        for category in catalog['categories']:
            source=json.loads(apk.read('assets/init_data/'+category['uid']))
            assert category['levelIds']==['level_'+str(x['id']) for x in source]
            checks.append('ordered category '+category['name'])
        for entry in catalog['levels']:
            if not entry['available']:
                continue
            level=json.loads((assets/'Resources/Levels'/(entry['levelId']+'.json')).read_text(encoding='utf-8-sig'))
            image=assets/'Resources'/ (entry['finishedResource']+'.png')
            source_name=hashlib.md5(entry['pixelMapUrl'].encode()).hexdigest()+'.png'
            assert image.read_bytes()==apk.read('assets/init_data/images/'+source_name), entry['levelId']
            size,palette,cells=analyze(image)
            assert [level['width'],level['height']]==list(size), entry['levelId']
            assert level['cells']==cells, entry['levelId']
            assert len(level['palette'])==len(palette), entry['levelId']
            for got,want in zip(level['palette'],palette):
                assert all(abs(got[k]-v/255)<1e-6 for k,v in zip(('r','g','b','a'),want)),entry['levelId']
            checks.append('pixel map and palette '+entry['levelId'])
    output=project/'Acceptance/2026-11-16/framework/source_checks.json'
    output.write_text(json.dumps({'passed':len(checks),'checks':checks,'failures':[]},ensure_ascii=False,indent=2)+'\n',encoding='utf-8',newline='\n')
    print('SOURCE_CHECKS_PASSED',len(checks))


if __name__=='__main__':
    main()
