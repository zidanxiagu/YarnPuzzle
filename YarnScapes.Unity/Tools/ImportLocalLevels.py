import argparse
import csv
import json
import os
import shutil


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--csv", required=True)
    parser.add_argument("--source-dir", required=True)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--catalog", required=True)
    args = parser.parse_args()

    os.makedirs(args.output_dir, exist_ok=True)
    catalog = []
    with open(args.csv, "r", encoding="utf-8-sig", newline="") as handle:
        for row in csv.DictReader(handle):
            level_id = row["id"]
            source_name = row.get("i512_local", "")
            finished_name = row.get("finished_local", "")
            source_path = os.path.join(args.source_dir, source_name)
            finished_path = os.path.join(args.source_dir, finished_name) if finished_name else ""
            if not os.path.isfile(source_path):
                continue
            source_target = os.path.join(args.output_dir, "level_" + level_id + "_source" + os.path.splitext(source_name)[1].lower())
            shutil.copy2(source_path, source_target)
            finished_target = ""
            if finished_path and os.path.isfile(finished_path):
                finished_target = os.path.join(args.output_dir, "level_" + level_id + "_finished" + os.path.splitext(finished_name)[1].lower())
                shutil.copy2(finished_path, finished_target)
            catalog.append({
                "id": level_id,
                "paintId": row.get("paint_id", ""),
                "groupCategory": row.get("group_category", ""),
                "categories": row.get("categories", ""),
                "sourceResource": "LevelImages/Local/" + os.path.splitext(os.path.basename(source_target))[0],
                "finishedResource": "LevelImages/Local/" + os.path.splitext(os.path.basename(finished_target))[0] if finished_target else "",
            })

    with open(args.catalog, "w", encoding="utf-8") as handle:
        json.dump(catalog, handle, ensure_ascii=False, indent=2)
        handle.write("\n")
    print(json.dumps({"imported": len(catalog), "output": args.output_dir}, ensure_ascii=False))


if __name__ == "__main__":
    main()
