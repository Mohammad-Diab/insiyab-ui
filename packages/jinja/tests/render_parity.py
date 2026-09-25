"""Renders every parity case with the macros and prints them as JSON, {name: html},
for test/jinja.test.mjs to compare in a browser with the docs' examples."""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "src"))

from jinja2 import DictLoader, Environment  # noqa: E402

import insiyab  # noqa: E402
from parity_cases import CASES  # noqa: E402

env = insiyab.register(Environment(loader=DictLoader({}), autoescape=True))
out = {}
for name, source in CASES.items():
    out[name] = env.from_string('{% import "insiyab/ui.html" as ins %}' + source).render()
sys.stdout.reconfigure(encoding="utf-8")
print(json.dumps(out, ensure_ascii=False))
