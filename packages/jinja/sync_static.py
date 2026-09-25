"""Copies the library's dist/ into the package before it is built, so the wheel carries
the stylesheet, the script, the fonts and the plugins:

    python sync_static.py && pip wheel . --no-deps -w wheelhouse

The copy (src/insiyab/static/) is not committed; the repository's dist/ is the one
source, and a checkout uses it directly.
"""
import os
import shutil
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
DIST = os.path.normpath(os.path.join(HERE, "..", "..", "dist"))
OUT = os.path.join(HERE, "src", "insiyab", "static")

if not os.path.isfile(os.path.join(DIST, "insiyab.js")):
    sys.exit("dist/ is missing: run node build.mjs at the repository root first")
shutil.rmtree(OUT, ignore_errors=True)
shutil.copytree(DIST, OUT)
print("copied dist/ to src/insiyab/static/")
