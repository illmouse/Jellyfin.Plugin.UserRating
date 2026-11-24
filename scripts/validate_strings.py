#!/usr/bin/env python3
"""Validate Strings/*.json translation files.

Strings/en.json is the source of truth. Checks:
  * every file is a flat JSON object of string values;
  * translation keys exist in en.json (plural variants key_zero/one/two/few/many/other
    are accepted when en.json has the plural base);
  * {0}, {1} placeholders match the English text (plural forms may omit placeholders,
    but not add new ones);
  * HTML tags in values match the English text;
  * every key used by Configuration/ratings.js and Configuration/configPage.html exists in en.json.

Missing or empty translations are NOT errors: they fall back to English at runtime.
Exit code 1 on any error.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STRINGS = ROOT / "Strings"
PLURAL = re.compile(r"_(zero|one|two|few|many|other)$")
PLACEHOLDER = re.compile(r"\{\d+\}")
TAG = re.compile(r"<[^>]+>")
USED_IN_CODE = re.compile(r"""\b(?:t|tf|tp|T|TP)\(\s*['"]([A-Za-z0-9_]+)['"]""")
USED_IN_MARKUP = re.compile(r'data-i18n(?:-html|-placeholder)?="([^"]+)"')

errors = []


def err(msg):
    errors.append(msg)


def load(path):
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except Exception as e:  # noqa: BLE001
        err(f"{path.name}: invalid JSON: {e}")
        return None
    if not isinstance(data, dict):
        err(f"{path.name}: top level must be an object")
        return None
    for k, v in data.items():
        if not isinstance(v, str):
            err(f"{path.name}: '{k}' must be a string")
            return None
    return data


def main():
    en = load(STRINGS / "en.json")
    if en is None:
        return report()
    en_plural_bases = {PLURAL.sub("", k) for k in en if PLURAL.search(k)}

    for path in sorted(STRINGS.glob("*.json")):
        if path.name == "en.json":
            continue
        data = load(path)
        if data is None:
            continue
        translated = 0
        for k, v in data.items():
            m = PLURAL.search(k)
            if k in en:
                ref = en[k]
                strict = True
            elif m and PLURAL.sub("", k) in en_plural_bases:
                ref = en.get(PLURAL.sub("", k) + "_other", "")
                strict = False
            else:
                err(f"{path.name}: unknown key '{k}' (not in en.json)")
                continue
            if not v.strip():
                continue
            translated += 1
            got, want = set(PLACEHOLDER.findall(v)), set(PLACEHOLDER.findall(ref))
            if (strict and got != want) or (not strict and not got <= want):
                err(f"{path.name}: '{k}' placeholders {sorted(got)} != English {sorted(want)}")
            if sorted(TAG.findall(v)) != sorted(TAG.findall(ref)):
                err(f"{path.name}: '{k}' HTML tags differ from English")
        base_keys = {PLURAL.sub("", k) for k in en}
        done = {PLURAL.sub("", k) for k, v in data.items() if v.strip()}
        print(f"{path.name}: {len(done)}/{len(base_keys)} strings translated")

    used = set()
    for name in ("ratings.js", "configPage.html"):
        text = (ROOT / "Configuration" / name).read_text(encoding="utf-8")
        used |= set(USED_IN_CODE.findall(text)) | set(USED_IN_MARKUP.findall(text))
    for k in sorted(used):
        if k not in en and k not in en_plural_bases:
            err(f"code uses key '{k}' that is missing from en.json")

    return report()


def report():
    for e in errors:
        print(f"ERROR: {e}")
    print("OK" if not errors else f"{len(errors)} error(s)")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
